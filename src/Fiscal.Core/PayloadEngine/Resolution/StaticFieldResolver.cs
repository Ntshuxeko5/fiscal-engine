using Fiscal.Core.Context;
using Fiscal.Core.PayloadEngine.Config;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Core.PayloadEngine.Resolution
{
    /// <summary>
    /// Resolves "Config:X" mappings by reading static value X
    /// from the client's config file (the StaticValues dictionary).
    ///
    /// Example config entry:
    ///   "SellerName": "Config:SellerName"
    ///
    /// And in the same config file's StaticValues section:
    ///   "SellerName": "Acme Restaurant Group"
    ///
    /// These values never come from the POS - they're fixed per client:
    /// business name, tax number, currency code, device codes, etc.
    /// </summary>
    public class StaticFieldResolver : IFieldResolver
    {
        private readonly FiscalEngineConfig _config;

        public StaticFieldResolver(FiscalEngineConfig config)
        {
            _config = config;
        }

        public string Prefix => "Config";

        public object? Resolve(string mapping, FiscalContext context)
        {
            int colonIndex = mapping.IndexOf(':');
            if (colonIndex < 0 || colonIndex == mapping.Length - 1)
            {
                return null;
            }

            string key = mapping[(colonIndex + 1)..];

            if (!_config.StaticValues.TryGetValue(key, out string? value))
            {
                return null;
            }

            return value;
        }
    }
}
