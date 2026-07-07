using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Core.PayloadEngine.Config
{
    /// <summary>
    /// Describes the layout and content of the printed fiscal receipt.
    /// Each section is a line, text block, field, or line-items list.
    /// Rendered top to bottom by the slip renderer.
    /// </summary>
    public class SlipConfig
    {
        /// <summary>
        /// Character width of the receipt. Used for alignment and separators.
        /// </summary>
        public int Width { get; init; } = 40;

        public List<SlipSection> Sections { get; init; } = new();
    }

    /// <summary>
    /// A single section on the slip. Type determines rendering:
    ///   "line"   → a horizontal separator character repeated to Width
    ///   "text"   → a static label, optionally centered
    ///   "field"  → a label:value pair, value resolved from context
    ///   "items"  → repeating line items from the check
    /// </summary>
    public class SlipSection
    {
        /// <summary>One of: line, text, field, items</summary>
        public required string Type { get; init; }

        /// <summary>Static text for "line" and "text" types.</summary>
        public string? Value { get; init; }

        /// <summary>Display label for "field" and "items" types.</summary>
        public string? Label { get; init; }

        /// <summary>
        /// Source-prefixed mapping for "field" type.
        /// Supports: OpsContext.X, FiscalResult.X, PaymentOutcome.X,
        /// Config:X, Input:X
        /// </summary>
        public string? FieldValue { get; init; }

        /// <summary>Text alignment: left, center, right. Default: left.</summary>
        public string Align { get; init; } = "left";

        /// <summary>Optional condition - same IncludeIf syntax as payload engine.</summary>
        public string? IncludeIf { get; init; }

        /// <summary>For "items" type: field to use as the line label.</summary>
        public string? LabelField { get; init; }

        /// <summary>For "items" type: field to use as the line value.</summary>
        public string? ValueField { get; init; }
    }
}
