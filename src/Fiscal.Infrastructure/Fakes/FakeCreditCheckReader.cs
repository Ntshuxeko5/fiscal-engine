using Fiscal.Core.Context;
using Fiscal.Core.Domain;
using Fiscal.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Infrastructure.Fakes
{
    /// <summary>
    /// Simulates a credit/refund transaction - negative TotalDue
    /// triggers credit mode detection in the orchestrator.
    /// OriginalFiscalNo supplied via OperatorInput to simulate
    /// the operator entering the fiscal number being credited.
    /// </summary>
    public class FakeCreditCheckReader : ICheckReader
    {
        public Task<FiscalContext> ReadAsync(object posCheckInput)
        {
            var check = new PosCheck(new DynamicRecord(
                new Dictionary<string, object?>
                {
                    ["TransactionId"] = "TXN-CREDIT-001",
                    ["CashierId"] = "CSH-42",
                    ["TotalDue"] = -150.00m,   // negative = credit
                    ["IsB2B"] = false,
                    ["LineItems"] = new List<DynamicRecord>()
                }));

            var context = new FiscalContext { Check = check };

            // Operator enters the original fiscal number being credited
            context.OperatorInput.Set("fiscalNo", "FISC-20260701-0001");

            return Task.FromResult(context);
        }
    }
}
