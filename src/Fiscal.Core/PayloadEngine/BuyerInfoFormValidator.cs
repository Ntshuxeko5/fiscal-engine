using Fiscal.Core.PayloadEngine.Config;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Core.PayloadEngine
{
    /// <summary>
    /// Validates operator-entered buyer info against the field rules
    /// defined in BuyerInfoFormConfig. Called before the form submits
    /// so invalid input never reaches the pipeline.
    ///
    /// Three constraints per field:
    ///   Required → value must not be empty
    ///   Min      → length must be >= Min (or value >= Min for numbers)
    ///   Max      → length must be <= Max
    ///
    /// "Equal to" is just Min == Max, so no extra concept needed.
    /// </summary>
    public class BuyerInfoFormValidator
    {
        private readonly BuyerInfoFormConfig _config;

        public BuyerInfoFormValidator(BuyerInfoFormConfig config)
        {
            _config = config;
        }

        /// <summary>
        /// Validates a set of field values against config rules.
        /// Returns a dictionary of field key → error message for any failures.
        /// Empty dictionary means all fields are valid.
        /// </summary>
        public Dictionary<string, string> Validate(
            Dictionary<string, string?> values)
        {
            var errors = new Dictionary<string, string>();

            foreach (BuyerInfoField field in _config.Fields)
            {
                values.TryGetValue(field.Key, out string? value);
                string trimmed = value?.Trim() ?? string.Empty;

                // Required check
                if (field.Required && string.IsNullOrWhiteSpace(trimmed))
                {
                    errors[field.Key] = $"{field.Label} is required.";
                    continue; // no point checking length on empty value
                }

                // Skip length checks if field is empty and not required
                if (string.IsNullOrWhiteSpace(trimmed))
                {
                    continue;
                }

                // Min length check
                if (field.Min.HasValue && trimmed.Length < field.Min.Value)
                {
                    errors[field.Key] = field.Min == field.Max
                        ? $"{field.Label} must be exactly {field.Min} characters."
                        : $"{field.Label} must be at least {field.Min} characters.";
                    continue;
                }

                // Max length check
                if (field.Max.HasValue && trimmed.Length > field.Max.Value)
                {
                    errors[field.Key] = field.Min == field.Max
                        ? $"{field.Label} must be exactly {field.Max} characters."
                        : $"{field.Label} must be at most {field.Max} characters.";
                }
            }

            return errors;
        }
    }
}
