using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Core.PayloadEngine.Config
{
    /// <summary>
    /// Represents a single field mapping in the JSON config.
    /// Can appear in two forms:
    ///
    /// Simple string (no condition):
    ///   "TransactionId": "OpsContext.TransactionId"
    ///
    /// Object with optional condition:
    ///   "ServiceCharge": {
    ///     "Value": "OpsContext.ServiceCharge",
    ///     "IncludeIf": "OpsContext.ServiceCharge > 0"
    ///   }
    ///
    /// The engine resolves Value the same way regardless of form.
    /// IncludeIf is evaluated first - if false, the field is skipped.
    /// </summary>
    public class PayloadFieldConfig
    {
        /// <summary>
        /// The source-prefixed mapping string resolved at runtime.
        /// </summary>
        public required string Value { get; init; }

        /// <summary>
        /// Optional condition. Supported expressions:
        ///   "OpsContext.FieldName > 0"
        ///   "OpsContext.FieldName != null"
        ///   "OpsContext.FieldName == true"
        /// Null means always include.
        /// </summary>
        public string? IncludeIf { get; init; }
    }
}
