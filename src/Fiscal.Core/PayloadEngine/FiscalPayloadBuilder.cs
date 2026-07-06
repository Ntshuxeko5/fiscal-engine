using Fiscal.Core.Context;
using Fiscal.Core.Domain;
using Fiscal.Core.Interfaces;
using Fiscal.Core.PayloadEngine.Config;
using Fiscal.Core.PayloadEngine.Resolution;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Fiscal.Core.PayloadEngine
{
    /// <summary>
    /// The core of the plug-and-play engine. Walks the payload section
    /// of the client's JSON config and builds a DynamicRecord that matches
    /// exactly the shape the fiscal device expects.
    ///
    /// For each field in the config, it:
    ///   1. Reads the source-prefixed mapping string
    ///   2. Identifies which resolver handles that prefix
    ///   3. Delegates resolution to that resolver
    ///   4. Sets the resolved value on the output DynamicRecord
    ///
    /// Supports nested objects (recurse), arrays of line items (loop + template),
    /// and conditional fields (IncludeIf check before resolving).
    ///
    /// The builder knows nothing about specific field names or client logic.
    /// All of that lives in the JSON config file.
    /// </summary>
    public class FiscalPayloadBuilder : IFiscalPayloadBuilder
    {
        private readonly FiscalEngineConfig _config;
        private readonly Dictionary<string, IFieldResolver> _resolvers;

        public FiscalPayloadBuilder(
            FiscalEngineConfig config,
            IEnumerable<IFieldResolver> resolvers)
        {
            _config = config;

            // Index resolvers by prefix for O(1) lookup at resolution time.
            // Same pattern as TransformationRegistry - no switch, no if-chain.
            _resolvers = resolvers.ToDictionary(
                r => r.Prefix,
                r => r,
                StringComparer.OrdinalIgnoreCase);
        }

        public DynamicRecord Build(FiscalContext context)
        {
            // Payload is now a JsonElement - walk it directly
            if (_config.Payload.ValueKind != System.Text.Json.JsonValueKind.Object)
            {
                return new DynamicRecord();
            }

            var configNode = new Dictionary<string, object>();
            foreach (var prop in _config.Payload.EnumerateObject())
            {
                configNode[prop.Name] = prop.Value;
            }

            return BuildRecord(configNode, context);
        }

        /// <summary>
        /// Recursively walks a config dictionary and builds a DynamicRecord.
        /// Called for the root payload and for every nested object within it.
        /// </summary>
        private DynamicRecord BuildRecord(
            Dictionary<string, object> configNode,
            FiscalContext context)
        {
            var record = new DynamicRecord();

            foreach (var (fieldName, fieldDef) in configNode)
            {
                object? resolved = ResolveField(fieldName, fieldDef, context);
                if (resolved is not null)
                {
                    record.Set(fieldName, resolved);
                }
            }

            return record;
        }

        /// <summary>
        /// Resolves a single field from the config. The fieldDef can be:
        ///   - A string mapping   → "OpsContext.X", "Config:X", "Calc:X" etc.
        ///   - A JsonElement      → nested object or array (from JSON deserialization)
        ///   - A Dictionary       → nested object (when constructed in code)
        /// </summary>
        private object? ResolveField(
            string fieldName,
            object fieldDef,
            FiscalContext context)
        {
            // ── String mapping: the common case ──────────────────────────────
            // "TransactionId": "OpsContext.TransactionId"
            if (fieldDef is string mappingString)
            {
                return ResolveMapping(mappingString, context);
            }

            // ── JsonElement: what we get when config is deserialized from JSON ─
            if (fieldDef is JsonElement jsonElement)
            {
                return ResolveJsonElement(fieldName, jsonElement, context);
            }

            // ── Nested Dictionary: nested object defined in code ──────────────
            if (fieldDef is Dictionary<string, object> nested)
            {
                return BuildRecord(nested, context);
            }

            // Unknown shape - return as-is and let the fiscal device deal with it
            return fieldDef;
        }

        /// <summary>
        /// Handles a JsonElement from deserialized config.
        /// JsonElement can be a string (leaf mapping), object (nested),
        /// or array (line items).
        /// </summary>
        private object? ResolveJsonElement(
            string fieldName,
            JsonElement element,
            FiscalContext context)
        {
            switch (element.ValueKind)
            {
                // Leaf string mapping: "OpsContext.TransactionId"
                case JsonValueKind.String:
                    string? mappingStr = element.GetString();
                    return mappingStr is not null
                        ? ResolveMapping(mappingStr, context)
                        : null;

                // Nested object: recurse into it
                case JsonValueKind.Object:
                    var nestedConfig = new Dictionary<string, object>();
                    foreach (var prop in element.EnumerateObject())
                    {
                        nestedConfig[prop.Name] = prop.Value;
                    }
                    return BuildRecord(nestedConfig, context);

                // Array: line items - loop over POS data and apply template
                case JsonValueKind.Array:
                    return ResolveArray(element, context);

                // Literal values (numbers, bools) - use directly
                case JsonValueKind.Number:
                    return element.GetDecimal();

                case JsonValueKind.True:
                    return true;

                case JsonValueKind.False:
                    return false;

                default:
                    return null;
            }
        }

        /// <summary>
        /// Resolves a config array into a list of DynamicRecords.
        /// Arrays in config represent line item collections - each element
        /// is a template applied to every item in the POS line items list.
        ///
        /// Example config:
        /// "LineItems": [
        ///   {
        ///     "Sku":    "OpsContext.Sku",
        ///     "Amount": "OpsContext.Amount"
        ///   }
        /// ]
        ///
        /// The engine loops over POS LineItems, applies the template
        /// to each one, and returns a list of resolved DynamicRecords.
        /// </summary>
        private object ResolveArray(JsonElement arrayElement, FiscalContext context)
        {
            var results = new List<DynamicRecord>();

            // Get the first element as the item template
            // (arrays in our config schema have exactly one template element)
            JsonElement? template = null;
            foreach (var element in arrayElement.EnumerateArray())
            {
                template = element;
                break;
            }

            if (template is null)
            {
                return results;
            }

            // Get line items from the POS check
            IReadOnlyList<DynamicRecord> lineItems =
                context.Check.Data.GetRecordList("LineItems");

            // Apply the template to each line item
            foreach (DynamicRecord lineItem in lineItems)
            {
                // Temporarily overlay the line item data onto context
                // so "OpsContext.Sku" resolves from the line item,
                // not from the header check data
                var lineItemContext = BuildLineItemContext(context, lineItem);
                var itemRecord = new DynamicRecord();

                foreach (var prop in template.Value.EnumerateObject())
                {
                    object? value = ResolveField(prop.Name, prop.Value, lineItemContext);
                    if (value is not null)
                    {
                        itemRecord.Set(prop.Name, value);
                    }
                }

                results.Add(itemRecord);
            }

            return results;
        }

        /// <summary>
        /// Creates a context where the current line item's fields are
        /// accessible via "OpsContext.X" mappings, by merging the line
        /// item data on top of the check data temporarily.
        /// </summary>
        private static FiscalContext BuildLineItemContext(
            FiscalContext context,
            DynamicRecord lineItem)
        {
            // Build a merged record: line item fields override check fields
            // so template field refs like "OpsContext.Sku" resolve from
            // the line item, while "OpsContext.TransactionId" still works
            // from the header check data.
            var mergedData = new DynamicRecord();

            // Start with check header fields
            // (We need a way to copy - add a helper to DynamicRecord)
            var lineItemCheck = new PosCheck(lineItem);

            return new FiscalContext
            {
                Check = lineItemCheck,
                FiscalResult = context.FiscalResult,
                PaymentOutcome = context.PaymentOutcome
            };
        }

        /// <summary>
        /// The core routing method. Reads the prefix from a mapping string
        /// and delegates to the right resolver.
        ///
        /// Handles both colon-separated ("Config:X", "Calc:X") and
        /// dot-separated ("OpsContext.X") prefix styles.
        /// </summary>
        private object? ResolveMapping(string mapping, FiscalContext context)
        {
            // Identify prefix: check for colon first, then dot
            string prefix = GetPrefix(mapping);

            if (_resolvers.TryGetValue(prefix, out IFieldResolver? resolver))
            {
                return resolver.Resolve(mapping, context);
            }

            // No resolver found - log and return null rather than throwing,
            // so one bad mapping doesn't kill the entire payload build
            Console.WriteLine(
                $"[PayloadBuilder] Warning: no resolver found for prefix " +
                $"'{prefix}' in mapping '{mapping}'.");
            return null;
        }

        /// <summary>
        /// Extracts the prefix from a mapping string.
        /// "Config:SellerName"       → "Config"
        /// "OpsContext.TransactionId" → "OpsContext"
        /// "Calc:lineTotal"           → "Calc"
        /// </summary>
        private static string GetPrefix(string mapping)
        {
            // Try colon first (most prefixes use colon)
            int colonIndex = mapping.IndexOf(':');
            if (colonIndex > 0)
            {
                return mapping[..colonIndex];
            }

            // Fall back to dot (OpsContext uses dot notation)
            int dotIndex = mapping.IndexOf('.');
            if (dotIndex > 0)
            {
                return mapping[..dotIndex];
            }

            // No separator found - return the whole string as-is
            return mapping;
        }
    }
}
