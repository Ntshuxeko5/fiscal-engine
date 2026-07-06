using Fiscal.Core.Context;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Core.PayloadEngine.Resolution
{
    /// <summary>
    /// Resolves "Generated:X" mappings by creating values on the spot.
    /// No external data source needed - the engine produces these itself.
    ///
    /// Currently supported generators:
    ///   "Generated:timestamp"  → current UTC time in ISO 8601 format
    ///   "Generated:guid"       → a new random GUID
    ///
    /// New generators are added as new cases in the switch below.
    /// </summary>
    public class GeneratedFieldResolver : IFieldResolver
    {
        public string Prefix => "Generated";

        public object? Resolve(string mapping, FiscalContext context)
        {
            // "Generated:timestamp" → "timestamp"
            int colonIndex = mapping.IndexOf(':');
            if (colonIndex < 0 || colonIndex == mapping.Length - 1)
            {
                return null;
            }

            string generatorName = mapping[(colonIndex + 1)..].ToLowerInvariant();

            return generatorName switch
            {
                "timestamp" => DateTime.UtcNow.ToString("o"),
                "guid" => Guid.NewGuid().ToString(),
                _ => throw new InvalidOperationException(
                    $"Unknown generator '{generatorName}'. " +
                    $"Supported: timestamp, guid.")
            };
        }
    }
}
