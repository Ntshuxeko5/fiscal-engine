using Fiscal.Core.Context;
using Fiscal.Core.Domain;
using Fiscal.Core.Interfaces;

namespace Fiscal.Infrastructure.Fakes
{
    /// <summary>
    /// Fake implementation of ICheckReader for development and testing.
    /// Returns a hardcoded check with header fields and two line items,
    /// using real Fiscal Edge sandbox product codes so a --real run
    /// produces a valid, acceptable invoice.
    /// </summary>
    public class FakeCheckReader : ICheckReader
    {
        public Task<FiscalContext> ReadAsync(object posCheckInput)
        {
            var check = new PosCheck(new DynamicRecord(
                new Dictionary<string, object?>
                {
                    ["TransactionId"] = "TXN-0005",
                    ["CashierId"] = "CSH-42",
                    ["TotalDue"] = 12000.00m,
                    ["IsB2B"] = false,
                    ["ServiceCharge"] = 0m,
                    ["TipAmount"] = 0m,
                    ["LineItems"] = new List<DynamicRecord>
                    {
                        new DynamicRecord(new Dictionary<string, object?>
                        {
                            ["LineNumber"] = 1,
                            ["Sku"]        = "22834",   // (Massage)
                            ["Name"]       = "Massage",
                            ["Quantity"]   = 1,
                            ["UnitPrice"]  = 4000.00m,
                            ["Amount"]     = 4000.00m
                        }),
                        new DynamicRecord(new Dictionary<string, object?>
                        {
                            ["LineNumber"] = 2,
                            ["Sku"]        = "22834",
                            ["Name"]       = "Massage",
                            ["Quantity"]   = 2,
                            ["UnitPrice"]  = 4000.00m,
                            ["Amount"]     = 8000.00m
                        })
                    }
                }));

            var context = new FiscalContext { Check = check };
            return Task.FromResult(context);
        }
    }
}
