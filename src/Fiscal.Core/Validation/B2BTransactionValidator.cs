using Fiscal.Core.Context;
using Fiscal.Core.Domain;
using Fiscal.Core.Interfaces;
using Fiscal.Core.PayloadEngine;
using Fiscal.Core.PayloadEngine.Config;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Core.Validation
{
    /// <summary>
    /// Enforces B2B transaction validation using BuyerInfoFormConfig rules.
    /// If no form config is provided, falls back to checking BuyerTaxNumber
    /// directly for backwards compatibility.
    /// </summary>
    public class B2BTransactionValidator : ITransactionValidator
    {
        private readonly BuyerInfoFormConfig? _formConfig;

        public B2BTransactionValidator(BuyerInfoFormConfig? formConfig = null)
        {
            _formConfig = formConfig;
        }

        public FiscalValidationResult Validate(FiscalContext context)
        {
            bool isB2B = context.Check.Data.Get<bool>("IsB2B");
            if (!isB2B)
            {
                return FiscalValidationResult.Success();
            }

            if (_formConfig is not null)
            {
                return ValidateWithFormConfig(context);
            }

            // Fallback: just check BuyerTaxNumber exists
            string? taxNumber = context.OperatorInput.Get<string>("BuyerTaxNumber");
            if (string.IsNullOrWhiteSpace(taxNumber))
            {
                return FiscalValidationResult.Failure(
                    "B2B transaction requires a buyer tax number.");
            }

            return FiscalValidationResult.Success();
        }

        private FiscalValidationResult ValidateWithFormConfig(FiscalContext context)
        {
            var validator = new BuyerInfoFormValidator(_formConfig!);

            // Collect current operator input values into a plain dictionary
            var values = new Dictionary<string, string?>();
            foreach (BuyerInfoField field in _formConfig!.Fields)
            {
                values[field.Key] = context.OperatorInput
                    .Get<string>(field.Key);
            }

            Dictionary<string, string> errors = validator.Validate(values);

            if (errors.Count == 0)
            {
                return FiscalValidationResult.Success();
            }

            // Return the first error as the failure reason
            // (the UI layer shows all errors; the pipeline just needs one)
            string firstError = errors.Values.First();
            return FiscalValidationResult.Failure(firstError);
        }
    }
}
