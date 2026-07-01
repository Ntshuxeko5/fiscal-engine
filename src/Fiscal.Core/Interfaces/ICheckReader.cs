using Fiscal.Core.Context;

namespace Fiscal.Core.Interfaces
{
    /// <summary>
    /// Step 1 of the pipeline. Reads raw transaction details from the POS
    /// and returns a populated FiscalContext ready for the rest of the pipeline.
    /// The actual field names and structure of the check data are determined
    /// by config at runtime - not by this interface.
    /// </summary>
    public interface ICheckReader
    {
        Task<FiscalContext> ReadAsync(object posCheckInput);
    }
}
