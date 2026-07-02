using Fiscal.Core.Context;
using Fiscal.Core.Domain;
using Fiscal.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Infrastructure.Fakes
{
    /// <summary>
    /// Fake fiscal device client. Simulates a successful fiscal response
    /// by default. Set ShouldFail = true to simulate a fiscal failure
    /// and verify the gate holds in tests.
    /// </summary>
    public class FakeFiscalClient : IFiscalClient
    {
        public bool ShouldFail { get; set; } = false;

        public Task<DynamicRecord> FiscalizeAsync(
            DynamicRecord payload,
            FiscalContext context)
        {
            if (ShouldFail)
            {
                var failure = new DynamicRecord(new Dictionary<string, object?>
                {
                    ["Success"] = false,
                    ["ErrorMessage"] = "Fiscal device rejected the transaction."
                });
                return Task.FromResult(failure);
            }

            var success = new DynamicRecord(new Dictionary<string, object?>
            {
                ["Success"] = true,
                ["FiscalReceiptNumber"] = "FISC-20260701-0001",
                ["FiscalTimestamp"] = DateTime.UtcNow.ToString("o")
            });
            return Task.FromResult(success);
        }
    }
}
