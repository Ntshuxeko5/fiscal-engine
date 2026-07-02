using Fiscal.Core.Context;
using Fiscal.Core.Domain;
using Fiscal.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Infrastructure.Fakes
{
    /// <summary>
    /// Fake implementation of ICheckReader for development and testing.
    /// Returns a hardcoded check with two line items so the pipeline
    /// has real-looking data to work with without needing a live POS.
    /// </summary>
    public class FakeCheckReader : ICheckReader
    {
        public Task<FiscalContext> ReadAsync(object posCheckInput)
        {
            var check = new PosCheck(new DynamicRecord(
                new Dictionary<string, object?>
                {
                    ["TransactionId"] = "TXN-0001",
                    ["CashierId"] = "CSH-42",
                    ["TotalDue"] = 150.00m,
                    ["IsB2B"] = false,
                    ["LineItems"] = new List<DynamicRecord>
                    {
                    new DynamicRecord(new Dictionary<string, object?>
                    {
                        ["Sku"]      = "BURGER-001",
                        ["Name"]     = "Cheeseburger",
                        ["Quantity"] = 2,
                        ["Amount"]   = 80.00m
                    }),
                    new DynamicRecord(new Dictionary<string, object?>
                    {
                        ["Sku"]      = "DRINK-002",
                        ["Name"]     = "Cola",
                        ["Quantity"] = 2,
                        ["Amount"]   = 70.00m
                    })
                    }
                }));

            var context = new FiscalContext { Check = check };
            return Task.FromResult(context);
        }
    }
}
