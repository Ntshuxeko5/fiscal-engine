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
        private readonly Dictionary<string, string> _buyerValues;

        public FakeOperatorInputCollector(
            string? fiscalNo = null,
            Dictionary<string, string>? buyerValues = null)
        {
            _fiscalNo = fiscalNo;
            _buyerValues = buyerValues ?? new Dictionary<string, string>();
        }

        public Task CollectAsync(FiscalContext context)
        {
            if (context.Mode == TransactionMode.Credit &&
                _fiscalNo is not null)
            {
                context.OperatorInput.Set("fiscalNo", _fiscalNo);
            }

            if (context.Check.Data.Get<bool>("IsB2B"))
            {
                foreach (var (key, value) in _buyerValues)
                {
                    context.OperatorInput.Set(key, value);
                }
            }

            return Task.CompletedTask;
        }
    }
}
