using Fiscal.Core.Context;
using Fiscal.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Infrastructure.Fakes
{
    /// <summary>
    /// Fake slip printer. Records whether it was called so tests can
    /// assert that printing never runs after a fiscal failure.
    /// Writes to console so you can see the pipeline completing
    /// when running Fiscal.Console.
    /// </summary>
    public class FakeSlipPrinter : ISlipPrinter
    {
        public bool WasCalled { get; private set; } = false;

        public Task PrintAsync(FiscalContext context)
        {
            WasCalled = true;

            string fiscalNumber = context.FiscalResult?
                .Get<string>("FiscalReceiptNumber") ?? "N/A";

            Console.WriteLine("────────────────────────────");
            Console.WriteLine("       FISCAL RECEIPT       ");
            Console.WriteLine("────────────────────────────");
            Console.WriteLine($"Transaction : {context.Check.Data.Get<string>("TransactionId")}");
            Console.WriteLine($"Fiscal No.  : {fiscalNumber}");
            Console.WriteLine($"Total       : {context.Check.Data.Get<decimal>("TotalDue"):C}");
            Console.WriteLine("────────────────────────────");

            return Task.CompletedTask;
        }
    }
}
