using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Core.PayloadEngine.Config
{
    /// <summary>
    /// Represents a single field mapping in the JSON config.
    /// The Value string tells the engine where to get the data:
    ///
    ///   "OpsContext.TransactionId"  → from the live POS check
    ///   "Config:SellerName"         → static value in config
    ///   "Calc:calculateTotal"       → named transformation
    ///   "Generated:timestamp"       → engine creates it
    ///   "Input:BuyerTaxNumber"      → operator-entered form data
    ///
    /// This is the core unit of the plug-and-play config model.
    /// </summary>
    public class PayloadFieldConfig
    {
        /// <summary>
        /// The source-prefixed mapping string. The engine parses
        /// the prefix at runtime to know which resolver to call.
        /// </summary>
        public required string Value { get; init; }

        /// <summary>
        /// Optional condition. If set, this field is only included
        /// in the payload if the condition evaluates to true.
        /// e.g. "OpsContext.ServiceCharge > 0"
        /// Null means always include.
        /// </summary>
        public string? IncludeIf { get; init; }
    }
}
