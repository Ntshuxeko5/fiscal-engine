using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Fiscal.Core.PayloadEngine.Config
{
    /// <summary>
    /// Root configuration object deserialized from the client's
    /// JSON config file at startup. One config file per client/device.
    /// </summary>
    public class FiscalEngineConfig
    {
        /// <summary>
        /// Static values referenced in payload mappings via "Config:X".
        /// Strongly typed as string→string since static config values
        /// are always strings.
        /// </summary>
        public Dictionary<string, string> StaticValues { get; init; } = new();

        /// <summary>
        /// The payload structure. Kept as JsonElement so the payload
        /// builder controls exactly how it walks and resolves the tree,
        /// rather than letting the deserializer decide the shape.
        /// </summary>
        public JsonElement Payload { get; init; }

        public int TenderMediaNumber { get; init; }

        /// <summary>
        /// Per-client output values for each transaction mode.
        /// The engine detects the mode; config decides how to write it.
        /// e.g. Invoice → "INVOICE", Credit → "CREDIT" or "C"
        /// </summary>
        public Dictionary<string, string> ModeValues { get; init; } = new();

        /// <summary>
        /// Config-driven receipt layout. Describes what appears on the
        /// printed slip and in what order, using the same source-prefix
        /// pattern as the fiscal payload engine.
        /// </summary>
        public SlipConfig? SlipConfig { get; init; }
    }
}
