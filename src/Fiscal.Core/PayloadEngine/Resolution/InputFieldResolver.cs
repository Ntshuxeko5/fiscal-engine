using Fiscal.Core.Context;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Core.PayloadEngine.Resolution
{
    /// <summary>
    /// Resolves "Input:X" mappings by reading operator-entered values
    /// from FiscalContext.OperatorInput - the data collected from
    /// the B2B buyer-info form (or any other operator input mechanism).
    ///
    /// Example config entry:
    ///   "BuyerTaxNumber": "Input:BuyerTaxNumber"
    ///
    /// The key after "Input:" must exactly match the key the form
    /// used when writing to context.OperatorInput.
    /// </summary>
    public class InputFieldResolver : IFieldResolver
    {
        public string Prefix => "Input";

        public object? Resolve(string mapping, FiscalContext context)
        {
            // "Input:BuyerTaxNumber" → "BuyerTaxNumber"
            int colonIndex = mapping.IndexOf(':');
            if (colonIndex < 0 || colonIndex == mapping.Length - 1)
            {
                return null;
            }

            string key = mapping[(colonIndex + 1)..];
            return context.OperatorInput.Get(key);
        }
    }
}
