using Fiscal.Core.Context;
using Fiscal.Core.Domain;

namespace Fiscal.Core.Interfaces
{
    /// <summary>
    /// Step 2 of the pipeline. Sends the assembled fiscal payload to the
    /// fiscal device and returns the raw response as a DynamicRecord.
    /// This is the gate step - the orchestrator checks the result before
    /// allowing the pipeline to continue to payment.
    /// </summary>
    public interface IFiscalClient
    {
        Task<DynamicRecord> FiscalizeAsync(DynamicRecord payload, FiscalContext context);
    }
}
