using Fiscal.Core.Context;
using Fiscal.Core.Domain;
using Fiscal.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Infrastructure.Fakes
{
    /// <summary>
    /// Fake operator input collector for tests and the console runner.
    /// Returns preconfigured values based on transaction mode rather
    /// than prompting a real operator.
    ///
    /// In tests: construct with specific values to simulate any scenario.
    /// In WPF: replace with a real implementation that shows dialogs.
    /// </summary>
    public class FakeOperatorInputCollector : IOperatorInputCollector
    {
        private readonly string? _fiscalNo;
        private readonly string? _buyerTaxNumber;
        private readonly string? _buyerName;

        public FakeOperatorInputCollector(
            string? fiscalNo = null,
            string? buyerTaxNumber = null,
            string? buyerName = null)
        {
            _fiscalNo = fiscalNo;
            _buyerTaxNumber = buyerTaxNumber;
            _buyerName = buyerName;
        }

        public Task CollectAsync(FiscalContext context)
        {
            // Credit transaction - supply the original fiscal number
            if (context.Mode == TransactionMode.Credit &&
                _fiscalNo is not null)
            {
                context.OperatorInput.Set("fiscalNo", _fiscalNo);
            }

            // B2B transaction - supply buyer information
            if (context.Check.Data.Get<bool>("IsB2B"))
            {
                if (_buyerTaxNumber is not null)
                {
                    context.OperatorInput.Set("BuyerTaxNumber", _buyerTaxNumber);
                }

                if (_buyerName is not null)
                {
                    context.OperatorInput.Set("BuyerName", _buyerName);
                }
            }

            return Task.CompletedTask;
        }
    }
}
