using Fiscal.Core.Context;
using Fiscal.Core.Domain;
using Fiscal.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Infrastructure.Fakes
{
    /// <summary>
    /// Fake POS payment client. Records whether it was called so tests
    /// can assert that payment never runs after a fiscal failure.
    /// </summary>
    public class FakePaymentClient : IPaymentClient
    {
        public bool WasCalled { get; private set; } = false;
        public void Reset() => WasCalled = false;

        public Task<DynamicRecord> RunPaymentAsync(FiscalContext context)
        {
            WasCalled = true;

            var result = new DynamicRecord(new Dictionary<string, object?>
            {
                ["Success"] = true,
                ["TenderMediaNumber"] = 4,
                ["AmountCharged"] = context.Check.Data.Get<decimal>("TotalDue")
            });
            return Task.FromResult(result);
        }
    }
}
