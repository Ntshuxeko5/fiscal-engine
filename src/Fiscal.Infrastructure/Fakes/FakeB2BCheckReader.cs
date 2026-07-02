using Fiscal.Core.Context;
using Fiscal.Core.Domain;
using Fiscal.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Infrastructure.Fakes
{
    /// <summary>
    /// Fake check reader that simulates a B2B transaction
    /// with no buyer tax number supplied - used to test that
    /// B2B validation fires before fiscalization.
    /// </summary>
    public class FakeB2BCheckReader : ICheckReader
    {
        public Task<FiscalContext> ReadAsync(object posCheckInput)
        {
            var check = new PosCheck(new DynamicRecord(
                new Dictionary<string, object?>
                {
                    ["TransactionId"] = "TXN-B2B-001",
                    ["TotalDue"] = 500.00m,
                    ["IsB2B"] = true
                    // BuyerTaxNumber deliberately omitted
                }));

            var context = new FiscalContext { Check = check };
            return Task.FromResult(context);
        }
    }
}
