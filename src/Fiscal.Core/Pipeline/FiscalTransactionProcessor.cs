using Fiscal.Core.Context;
using Fiscal.Core.Domain;
using Fiscal.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Core.Pipeline
{
    public class FiscalTransactionProcessor
    {
        private readonly ICheckReader _checkReader;
        private readonly IOperatorInputCollector _inputCollector;
        private readonly ITransactionValidator _validator;
        private readonly IFiscalPayloadBuilder _payloadBuilder;
        private readonly IFiscalClient _fiscalClient;
        private readonly IPaymentClient _paymentClient;
        private readonly ISlipPrinter _slipPrinter;

        public FiscalTransactionProcessor(
            ICheckReader checkReader,
            IOperatorInputCollector inputCollector,
            ITransactionValidator validator,
            IFiscalPayloadBuilder payloadBuilder,
            IFiscalClient fiscalClient,
            IPaymentClient paymentClient,
            ISlipPrinter slipPrinter)
        {
            _checkReader = checkReader;
            _inputCollector = inputCollector;
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

            // ── Step 2: Detect transaction mode ──────────────────────────────
            // Universal rule: negative total = credit against previous invoice.
            // Lives in the engine, not config. Output value is configurable.
            decimal totalDue = context.Check.Data.Get<decimal>("TotalDue");
            context.Mode = totalDue < 0
                ? TransactionMode.Credit
                : TransactionMode.Invoice;

            // ── Step 3: Collect operator input ────────────────────────────────
            // Prompts operator for credit fiscalNo, B2B buyer info, etc.
            // Writes collected values to context.OperatorInput.
            // No-op if nothing needs to be collected for this transaction.
            await _inputCollector.CollectAsync(context);

            // ── Step 4: Validate ──────────────────────────────────────────────
            FiscalValidationResult validation = _validator.Validate(context);
            if (!validation.IsValid)
            {
                return FiscalPipelineResult.ValidationFailed(
                    validation.FailureReason ?? "Validation failed.");
            }

            // ── Step 5: Build payload + Fiscalize (the gate) ──────────────────
            DynamicRecord payload = _payloadBuilder.Build(context);
            DynamicRecord fiscalResult =
                await _fiscalClient.FiscalizeAsync(payload, context);
            context.FiscalResult = fiscalResult;

            bool fiscalSucceeded = fiscalResult.Get<bool>("Success");
            if (!fiscalSucceeded)
            {
                string reason = fiscalResult.Get<string>("ErrorMessage")
                    ?? "Fiscal device returned a failure response.";
                return FiscalPipelineResult.FiscalFailed(reason);
            }

            // ── Step 6: Run payment ───────────────────────────────────────────
            DynamicRecord paymentOutcome =
                await _paymentClient.RunPaymentAsync(context);
            context.PaymentOutcome = paymentOutcome;

            // ── Step 7: Print slip ────────────────────────────────────────────
            await _slipPrinter.PrintAsync(context);
            context.SlipPrinted = true;

            return FiscalPipelineResult.Succeeded(context);
        }
    }
}
