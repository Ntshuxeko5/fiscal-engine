using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Infrastructure.FiscalEdge
{
    /// <summary>
    /// Configuration for connecting to Fiscal Edge, the middleware that
    /// sits between our POS integration and MRA EIS. Fiscal Edge handles
    /// invoice number generation, tax breakdown validation, and MRA's
    /// exact schema translation internally - we send it a simpler,
    /// documented shape and it does the rest.
    ///
    /// Auth is two custom headers, not Bearer or Basic:
    ///   X-API-Key     - the API key issued for this integration
    ///   X-TerminalId  - identifies which terminal is submitting
    /// </summary>
    public class FiscalEdgeConfig
    {
        public required string BaseUrl { get; init; }

        public required string ApiKey { get; init; }

        public required string TerminalId { get; init; }

        public int TimeoutSeconds { get; init; } = 30;
    }
}
