using Fiscal.Core.Context;
using Fiscal.Core.PayloadEngine.Config;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Core.PayloadEngine.Resolution
{
    /// <summary>
    /// Resolves "Mode:X" mappings by looking up the current transaction
    /// mode (Invoice or Credit) in the per-client ModeValues config.
    ///
    /// Example config:
    ///   "ModeValues": {
    ///     "Invoice": "INVOICE",
    ///     "Credit":  "CREDIT"
    ///   }
    ///   
    ///   "FolioType": "Mode:documentType"
    ///
    /// The engine detects whether it's a credit or invoice transaction.
    /// The config decides what string gets written into the payload.
    /// This keeps the universal business rule (negative = credit) in code
    /// and the client-specific output value in config.
    /// </summary>
    public class ModeFieldResolver : IFieldResolver
    {
        private readonly FiscalEngineConfig _config;

        public ModeFieldResolver(FiscalEngineConfig config)
        {
            _config = config;
        }

        public string Prefix => "Mode";

        public object? Resolve(string mapping, FiscalContext context)
        {
            if (context.Mode is null)
            {
                return null;
            }

            // The mode key to look up in ModeValues config
            // e.g. TransactionMode.Credit → "Credit"
            string modeKey = context.Mode.Value.ToString();

            return _config.ModeValues.TryGetValue(modeKey, out string? value)
                ? value
                : null;
        }
    }
}
