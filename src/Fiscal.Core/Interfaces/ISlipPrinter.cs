using Fiscal.Core.Context;

namespace Fiscal.Core.Interfaces
{
    /// <summary>
    /// Step 4 of the pipeline. Composes and prints the client receipt using
    /// data from both the POS check and the fiscal response, both of which
    /// are available on the context by the time this step runs.
    /// </summary>
    public interface ISlipPrinter
    {
        Task PrintAsync(FiscalContext context);
    }
}
