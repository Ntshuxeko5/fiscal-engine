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
    public class FiscalPayloadBuilder : IFiscalPayloadBuilder
    {
        private readonly FiscalEngineConfig _config;
        private readonly Dictionary<string, IFieldResolver> _resolvers;
        private readonly ConditionEvaluator _conditionEvaluator;

        public FiscalPayloadBuilder(
            FiscalEngineConfig config,
            IEnumerable<IFieldResolver> resolvers)
        {
            _config = config;
            _resolvers = resolvers.ToDictionary(
                r => r.Prefix,
                r => r,
                StringComparer.OrdinalIgnoreCase);

            // Extract the two resolvers the condition evaluator needs
            _conditionEvaluator = new ConditionEvaluator(
                (_resolvers.TryGetValue("OpsContext", out var pos)
                    ? pos : null) as PosFieldResolver
                    ?? new PosFieldResolver(),
                (_resolvers.TryGetValue("Input", out var input)
                    ? input : null) as InputFieldResolver
                    ?? new InputFieldResolver());
        }

        public DynamicRecord Build(FiscalContext context)
        {
            if (_config.Payload.ValueKind != JsonValueKind.Object)
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

        private DynamicRecord BuildRecord(
            Dictionary<string, object> configNode,
            FiscalContext context)
        {
            var record = new DynamicRecord();

            foreach (var (fieldName, fieldDef) in configNode)
            {
                // Try to extract a PayloadFieldConfig (for IncludeIf support)
                PayloadFieldConfig? fieldConfig = TryExtractFieldConfig(fieldDef);

                // Evaluate condition before doing any resolution work
                if (fieldConfig is not null &&
                    !_conditionEvaluator.Evaluate(fieldConfig.IncludeIf, context))
                {
                    continue; // condition failed - skip this field entirely
                }

                // Use the Value from fieldConfig if we have one,
                // otherwise use the raw fieldDef for nested objects/arrays
                object effectiveDef = fieldConfig is not null
                    ? fieldConfig.Value
                    : fieldDef;

                object? resolved = ResolveField(fieldName, effectiveDef, context);
                if (resolved is not null)
                {
                    record.Set(fieldName, resolved);
                }
            }

            return record;
        }

        /// <summary>
        /// Tries to interpret a field definition as a PayloadFieldConfig.
        /// Returns null if the field is a nested object or array (not a leaf mapping).
        ///
        /// Handles two JSON shapes:
        ///   "TransactionId": "OpsContext.TransactionId"
        ///     → fieldDef is JsonElement(String) → no IncludeIf
        ///
        ///   "ServiceCharge": { "Value": "OpsContext.ServiceCharge", "IncludeIf": "..." }
        ///     → fieldDef is JsonElement(Object with Value+IncludeIf properties)
        /// </summary>
        private static PayloadFieldConfig? TryExtractFieldConfig(object fieldDef)
        {
            if (fieldDef is not JsonElement element)
            {
                return null;
            }

            // Plain string mapping - wrap in a fieldConfig with no condition
            if (element.ValueKind == JsonValueKind.String)
            {
                string? value = element.GetString();
                return value is not null
                    ? new PayloadFieldConfig { Value = value }
                    : null;
            }

            // Object - check if it has a "Value" property (PayloadFieldConfig shape)
            // vs being a nested payload object (no "Value" property)
            if (element.ValueKind == JsonValueKind.Object &&
                element.TryGetProperty("Value", out var valueProp) &&
                valueProp.ValueKind == JsonValueKind.String)
            {
                string? value = valueProp.GetString();
                string? includeIf = null;

                if (element.TryGetProperty("IncludeIf", out var includeIfProp))
                {
                    includeIf = includeIfProp.GetString();
                }

                return value is not null
                    ? new PayloadFieldConfig { Value = value, IncludeIf = includeIf }
                    : null;
            }

            // Nested object or array - not a leaf field config
            return null;
        }

        private object? ResolveField(
            string fieldName,
            object fieldDef,
            FiscalContext context)
        {
            if (fieldDef is string mappingString)
            {
                return ResolveMapping(mappingString, context);
            }

            if (fieldDef is JsonElement jsonElement)
            {
                return ResolveJsonElement(fieldName, jsonElement, context);
            }

            if (fieldDef is Dictionary<string, object> nested)
            {
                return BuildRecord(nested, context);
            }

            return fieldDef;
        }

        private object? ResolveJsonElement(
            string fieldName,
            JsonElement element,
            FiscalContext context)
        {
            switch (element.ValueKind)
            {
                case JsonValueKind.String:
                    string? mappingStr = element.GetString();
                    return mappingStr is not null
                        ? ResolveMapping(mappingStr, context)
                        : null;

                case JsonValueKind.Object:
                    var nestedConfig = new Dictionary<string, object>();
                    foreach (var prop in element.EnumerateObject())
                    {
                        nestedConfig[prop.Name] = prop.Value;
                    }
                    return BuildRecord(nestedConfig, context);

                case JsonValueKind.Array:
                    return ResolveArray(element, context);

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

        private object ResolveArray(JsonElement arrayElement, FiscalContext context)
        {
            var results = new List<DynamicRecord>();

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

            IReadOnlyList<DynamicRecord> lineItems =
                context.Check.Data.GetRecordList("LineItems");

            foreach (DynamicRecord lineItem in lineItems)
            {
                var lineItemContext = BuildLineItemContext(context, lineItem);
                var itemRecord = new DynamicRecord();

                foreach (var prop in template.Value.EnumerateObject())
                {
                    object? value = ResolveField(
                        prop.Name, prop.Value, lineItemContext);
                    if (value is not null)
                    {
                        itemRecord.Set(prop.Name, value);
                    }
                }

                results.Add(itemRecord);
            }

            return results;
        }

        private static FiscalContext BuildLineItemContext(
            FiscalContext context,
            DynamicRecord lineItem)
        {
            return new FiscalContext
            {
                Check = new PosCheck(lineItem),
                FiscalResult = context.FiscalResult,
                PaymentOutcome = context.PaymentOutcome,
                Mode = context.Mode
            };
        }

        private object? ResolveMapping(string mapping, FiscalContext context)
        {
            string prefix = GetPrefix(mapping);

            if (_resolvers.TryGetValue(prefix, out IFieldResolver? resolver))
            {
                return resolver.Resolve(mapping, context);
            }

            Console.WriteLine(
                $"[PayloadBuilder] Warning: no resolver for prefix " +
                $"'{prefix}' in mapping '{mapping}'.");
            return null;
        }

        private static string GetPrefix(string mapping)
        {
            int colonIndex = mapping.IndexOf(':');
            if (colonIndex > 0)
            {
                return mapping[..colonIndex];
            }

            int dotIndex = mapping.IndexOf('.');
            if (dotIndex > 0)
            {
                return mapping[..dotIndex];
            }

            return mapping;
        }
    }
}
