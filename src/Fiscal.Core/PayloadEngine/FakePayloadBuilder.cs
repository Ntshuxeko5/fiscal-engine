using Fiscal.Core.Context;
using Fiscal.Core.Domain;
using Fiscal.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Core.PayloadEngine
{
    /// <summary>
    /// Stub payload builder - returns a minimal DynamicRecord so the
    /// pipeline can run end to end without a real config-driven engine.
    /// The real IFiscalPayloadBuilder implementation will replace this
    /// once the config schema is finalized.
    /// </summary>
    public class FakePayloadBuilder : IFiscalPayloadBuilder
    {
        public DynamicRecord Build(FiscalContext context)
        {
            return new DynamicRecord(new Dictionary<string, object?>
            {
                ["TransactionId"] = context.Check.Data.Get<string>("TransactionId"),
                ["TotalDue"] = context.Check.Data.Get<decimal>("TotalDue"),
                ["CashierId"] = context.Check.Data.Get<string>("CashierId"),
                ["Timestamp"] = DateTime.UtcNow.ToString("o")
            });
        }
    }
}
