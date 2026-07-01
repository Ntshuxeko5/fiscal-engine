using Fiscal.Core.Context;
using Fiscal.Core.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Core.Interfaces
{
    /// <summary>
    /// Step 3 of the pipeline. Executes the payment command on the POS terminal.
    /// Only ever called after a successful fiscal response - the orchestrator
    /// enforces this, not this interface.
    /// The tender media number comes from config, not from this interface's
    /// caller - the implementation resolves it internally.
    /// </summary>
    public interface IPaymentClient
    {
        Task<DynamicRecord> RunPaymentAsync(FiscalContext context);
    }
}
