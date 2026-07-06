using Fiscal.Core.Context;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Core.PayloadEngine.Resolution
{
    /// <summary>
    /// Resolves "OpsContext.X" mappings by reading field X directly
    /// off the live POS check data in the context.
    ///
    /// Example config entry:
    ///   "TransactionId": "OpsContext.TransactionId"
    ///
    /// At runtime, this resolver strips "OpsContext." from the front
    /// and calls DynamicRecord.Get(fieldName) on the check data.
    /// The field name is whatever came from the POS - no hardcoding.
    /// </summary>
    public class PosFieldResolver : IFieldResolver
    {
        public string Prefix => "OpsContext";

        public object? Resolve(string mapping, FiscalContext context)
        {
            // "OpsContext.TransactionId" → "TransactionId"
            // We split on the first dot only, in case the field name
            // itself contains dots (unlikely but defensive).
            int dotIndex = mapping.IndexOf('.');
            if (dotIndex < 0 || dotIndex == mapping.Length - 1)
            {
                return null;
            }

            string fieldName = mapping[(dotIndex + 1)..];
            return context.Check.Data.Get(fieldName);
        }
    }
}
