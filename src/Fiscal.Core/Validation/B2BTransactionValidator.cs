using Fiscal.Core.Context;
using Fiscal.Core.Domain;
using Fiscal.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Core.Validation
{
    /// <summary>
    /// Enforces the one runtime business rule: B2B transactions must
    /// have a tax number supplied via operator input before fiscalization.
    /// All other data validation is handled by the POS.
    /// </summary>
    public class B2BTransactionValidator : ITransactionValidator
    {
        public FiscalValidationResult Validate(FiscalContext context)
        {
            bool isB2B = context.Check.Data.Get<bool>("IsB2B");
            if (!isB2B)
            {
                return FiscalValidationResult.Success();
            }

            string? taxNumber = context.OperatorInput.Get<string>("BuyerTaxNumber");
            if (string.IsNullOrWhiteSpace(taxNumber))
            {
                return FiscalValidationResult.Failure(
                    "B2B transaction requires a buyer tax number. " +
                    "Please collect it via the operator input form.");
            }

            return FiscalValidationResult.Success();
        }
    }
}
