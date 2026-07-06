using Fiscal.Core.Context;
using Fiscal.Core.Domain;
using Fiscal.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Core.Transformations
{
    /// <summary>
    /// Named "lineTotal" - sums the Amount field across all line items
    /// in the check. Referenced in config as "Calc:lineTotal".
    ///
    /// This is a simple example of a calculated field that aggregates
    /// data from the POS line items - the kind of transformation that
    /// would otherwise require client-specific code.
    /// </summary>
    public class LineTotalTransformation : ITransformation
    {
        public string Name => "lineTotal";

        public object? Apply(IReadOnlyList<string> args, FiscalContext context)
        {
            IReadOnlyList<DynamicRecord> lineItems =
                context.Check.Data.GetRecordList("LineItems");

            decimal total = 0m;
            foreach (DynamicRecord item in lineItems)
            {
                total += item.Get<decimal>("Amount");
            }

            return total;
        }
    }
}
