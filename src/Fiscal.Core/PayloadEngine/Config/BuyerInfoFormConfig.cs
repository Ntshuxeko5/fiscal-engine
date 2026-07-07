using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Core.PayloadEngine.Config
{
    /// <summary>
    /// Config for the B2B buyer information form.
    /// Defines which fields to collect from the operator when a B2B
    /// transaction is selected. The form renders generically from this
    /// config - no per-client UI code required.
    /// </summary>
    public class BuyerInfoFormConfig
    {
        /// <summary>
        /// Label shown on the trigger button/option in the POS UI.
        /// e.g. "B2B Sale"
        /// </summary>
        public string TriggerLabel { get; init; } = "B2B Sale";

        /// <summary>
        /// Fields to collect, in the order they appear on the form.
        /// </summary>
        public List<BuyerInfoField> Fields { get; init; } = new();
    }

    /// <summary>
    /// A single input field on the B2B buyer info form.
    /// Key matches the Input: reference used in payload mappings.
    /// </summary>
    public class BuyerInfoField
    {
        /// <summary>
        /// The key this field is stored under in context.OperatorInput.
        /// Must exactly match the "Input:X" reference in the payload config.
        /// e.g. "BuyerTaxNumber" → payload uses "Input:BuyerTaxNumber"
        /// </summary>
        public required string Key { get; init; }

        /// <summary>Display label shown to the operator.</summary>
        public required string Label { get; init; }

        /// <summary>Whether the operator must fill this field.</summary>
        public bool Required { get; init; } = false;

        /// <summary>
        /// Minimum length (for strings) or minimum value (for numbers).
        /// Null means no minimum constraint.
        /// Note: Min == Max == 9 means exactly 9 characters required.
        /// </summary>
        public int? Min { get; init; }

        /// <summary>Maximum length or value. Null means no maximum.</summary>
        public int? Max { get; init; }
    }
}
