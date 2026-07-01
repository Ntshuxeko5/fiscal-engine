using Fiscal.Core.Context;
using Fiscal.Core.Domain;
using Fiscal.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Core.Pipeline
{
    /// <summary>
    /// The single entry point for a fiscal transaction.
    /// Runs all four steps in a fixed, unbypassable order:
    ///   1. Read check
    ///   2. Validate (B2B rule)
    ///   3. Fiscalize — this is the gate
    ///   4. Run payment (only if fiscal succeeded)
    ///   5. Print slip
    ///
    /// No step can be reordered, skipped, or called independently
    /// from outside this class. The order is the business rule.
    /// </summary>
    public class FiscalTransactionProcessor
    {
        private readonly ICheckReader _checkReader;
        private readonly ITransactionValidator _validator;
        private readonly IFiscalPayloadBuilder _payloadBuilder;
        private readonly IFiscalClient _fiscalClient;
        private readonly IPaymentClient _paymentClient;
        private readonly ISlipPrinter _slipPrinter;

        public FiscalTransactionProcessor(
            ICheckReader checkReader,
            ITransactionValidator validator,
            IFiscalPayloadBuilder payloadBuilder,
            IFiscalClient fiscalClient,
            IPaymentClient paymentClient,
            ISlipPrinter slipPrinter)
        {
            _checkReader = checkReader;
            _validator = validator;
            _payloadBuilder = payloadBuilder;
            _fiscalClient = fiscalClient;
            _paymentClient = paymentClient;
            _slipPrinter = slipPrinter;
        }

        public async Task<FiscalPipelineResult> ProcessAsync(object posCheckInput)
        {
            // ── Step 1: Read check ────────────────────────────────────────────
            FiscalContext context = await _checkReader.ReadAsync(posCheckInput);

            // ── Step 2: Validate ──────────────────────────────────────────────
            // B2B tax number check happens here, before we ever touch the
            // fiscal device. Cheap to check, expensive to get wrong.
            FiscalValidationResult validation = _validator.Validate(context);
            if (!validation.IsValid)
            {
                return FiscalPipelineResult.ValidationFailed(
                    validation.FailureReason ?? "Validation failed.");
            }

            // ── Step 3: Build payload + Fiscalize (the gate) ──────────────────
            // The payload builder resolves config + check data into the exact
            // JSON shape this client's fiscal device expects.
            // If fiscalization fails, everything stops here.
            DynamicRecord payload = _payloadBuilder.Build(context);
            DynamicRecord fiscalResult = await _fiscalClient.FiscalizeAsync(payload, context);
            context.FiscalResult = fiscalResult;

            bool fiscalSucceeded = fiscalResult.Get<bool>("Success");
            if (!fiscalSucceeded)
            {
                string reason = fiscalResult.Get<string>("ErrorMessage")
                    ?? "Fiscal device returned a failure response.";

                return FiscalPipelineResult.FiscalFailed(reason);
            }

            // ── Step 4: Run payment ───────────────────────────────────────────
            // Only reachable if fiscal succeeded. This is not a coincidence -
            // there is no code path from a failed fiscal to this line.
            DynamicRecord paymentOutcome = await _paymentClient.RunPaymentAsync(context);
            context.PaymentOutcome = paymentOutcome;

            // ── Step 5: Print slip ────────────────────────────────────────────
            // Context now carries check data, fiscal result, and payment outcome -
            // everything the receipt needs.
            await _slipPrinter.PrintAsync(context);
            context.SlipPrinted = true;

            return FiscalPipelineResult.Succeeded(context);
        }
    }
}
