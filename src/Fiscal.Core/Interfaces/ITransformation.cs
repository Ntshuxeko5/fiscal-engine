using Fiscal.Core.Context;
using Fiscal.Core.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Core.Interfaces
{
    /// <summary>
    /// A single named, reusable calculation referenced in config via "Calc:".
    /// Examples: combining side item prices into a combo total,
    /// aggregating tax amounts across line items.
    /// Each implementation is independently testable and registered by name in DI.
    /// </summary>
    public interface ITransformation
    {
        /// <summary>
        /// The name this transformation is registered and referenced by in config.
        /// Must exactly match the string after "Calc:" in the config file.
        /// e.g. "combineTotals", "serviceChargeAmount", "taxAggregation"
        /// </summary>
        string Name { get; }

        object? Apply(IReadOnlyList<string> args, FiscalContext context);
    }
}
