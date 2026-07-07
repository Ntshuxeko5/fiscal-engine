using Fiscal.Core.Context;
using Fiscal.Core.Domain;
using Fiscal.Core.PayloadEngine.Config;
using Fiscal.Core.PayloadEngine.Resolution;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Core.PayloadEngine
{
    /// <summary>
    /// Renders a fiscal receipt from SlipConfig + FiscalContext.
    /// Uses the same source-prefix resolution as the payload engine,
    /// with two additional prefixes for fiscal and payment data:
    ///   FiscalResult.X    → context.FiscalResult
    ///   PaymentOutcome.X  → context.PaymentOutcome
    ///
    /// Output is a list of strings (one per line) so the caller
    /// decides how to render them (console, printer, WPF TextBlock, etc).
    /// </summary>
    public class SlipRenderer
    {
        private readonly SlipConfig _config;
        private readonly ConditionEvaluator _conditionEvaluator;

        public SlipRenderer(SlipConfig config)
        {
            _config = config;
            _conditionEvaluator = new ConditionEvaluator(
                new PosFieldResolver(),
                new InputFieldResolver());
        }

        public IReadOnlyList<string> Render(FiscalContext context)
        {
            var lines = new List<string>();

            foreach (SlipSection section in _config.Sections)
            {
                if (!_conditionEvaluator.Evaluate(section.IncludeIf, context))
                {
                    continue;
                }

                switch (section.Type.ToLowerInvariant())
                {
                    case "line":
                        lines.Add(section.Value
                            ?? new string('─', _config.Width));
                        break;

                    case "text":
                        lines.Add(Align(section.Value ?? string.Empty,
                            section.Align));
                        break;

                    case "field":
                        if (section.FieldValue is not null)
                        {
                            object? value = ResolveValue(
                                section.FieldValue, context);
                            string formatted = FormatField(
                                section.Label ?? string.Empty,
                                value?.ToString() ?? string.Empty);
                            lines.Add(formatted);
                        }
                        break;

                    case "items":
                        lines.AddRange(
                            RenderItems(section, context));
                        break;
                }
            }

            return lines;
        }

        /// <summary>
        /// Resolves a source-prefixed value string from the slip config.
        /// Supports OpsContext, FiscalResult, PaymentOutcome, Config, Input.
        /// </summary>
        private static object? ResolveValue(
            string mapping,
            FiscalContext context)
        {
            // FiscalResult.X → read from fiscal device response
            if (mapping.StartsWith("FiscalResult.",
                StringComparison.OrdinalIgnoreCase))
            {
                string field = mapping["FiscalResult.".Length..];
                return context.FiscalResult?.Get(field);
            }

            // PaymentOutcome.X → read from payment response
            if (mapping.StartsWith("PaymentOutcome.",
                StringComparison.OrdinalIgnoreCase))
            {
                string field = mapping["PaymentOutcome.".Length..];
                return context.PaymentOutcome?.Get(field);
            }

            // OpsContext.X → read from POS check data
            if (mapping.StartsWith("OpsContext.",
                StringComparison.OrdinalIgnoreCase))
            {
                string field = mapping["OpsContext.".Length..];
                return context.Check.Data.Get(field);
            }

            // Input:X → read from operator input
            if (mapping.StartsWith("Input:",
                StringComparison.OrdinalIgnoreCase))
            {
                string field = mapping["Input:".Length..];
                return context.OperatorInput.Get(field);
            }

            return null;
        }

        private IEnumerable<string> RenderItems(
            SlipSection section,
            FiscalContext context)
        {
            IReadOnlyList<DynamicRecord> lineItems =
                context.Check.Data.GetRecordList("LineItems");

            foreach (DynamicRecord item in lineItems)
            {
                // Build a temporary context scoped to this line item
                var itemContext = new FiscalContext
                {
                    Check = new PosCheck(item),
                    FiscalResult = context.FiscalResult,
                    PaymentOutcome = context.PaymentOutcome,
                    Mode = context.Mode
                };

                string label = section.LabelField is not null
                    ? ResolveValue(section.LabelField, itemContext)
                        ?.ToString() ?? string.Empty
                    : string.Empty;

                string value = section.ValueField is not null
                    ? ResolveValue(section.ValueField, itemContext)
                        ?.ToString() ?? string.Empty
                    : string.Empty;

                yield return FormatField(label, value);
            }
        }

        private string Align(string text, string align) =>
            align.ToLowerInvariant() switch
            {
                "center" => text.PadLeft(
                    (_config.Width + text.Length) / 2).PadRight(_config.Width),
                "right" => text.PadLeft(_config.Width),
                _ => text
            };

        private string FormatField(string label, string value)
        {
            if (string.IsNullOrEmpty(label))
            {
                return value;
            }

            // "Transaction : TXN-0001"
            string formatted = $"{label} : {value}";
            return formatted;
        }
    }
}
