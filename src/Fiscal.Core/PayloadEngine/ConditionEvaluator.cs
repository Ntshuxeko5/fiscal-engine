using Fiscal.Core.Context;
using Fiscal.Core.PayloadEngine.Resolution;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Core.PayloadEngine
{
    /// <summary>
    /// Evaluates simple IncludeIf condition strings against the current
    /// FiscalContext. Conditions are intentionally limited to three operators
    /// (>, !=, ==) applied to a single field reference - no arbitrary
    /// expressions, no scripting engine. This keeps the config readable
    /// and the evaluator auditable.
    ///
    /// Supported syntax:
    ///   "OpsContext.FieldName > 0"
    ///   "OpsContext.FieldName != null"
    ///   "OpsContext.FieldName == true"
    ///   "OpsContext.FieldName == someValue"
    ///
    /// Left side must be a field reference (OpsContext.X or Input.X).
    /// Right side is a literal: number, null, true, false, or string.
    /// </summary>
    public class ConditionEvaluator
    {
        private readonly PosFieldResolver _posResolver;
        private readonly InputFieldResolver _inputResolver;

        public ConditionEvaluator(
            PosFieldResolver posResolver,
            InputFieldResolver inputResolver)
        {
            _posResolver = posResolver;
            _inputResolver = inputResolver;
        }

        /// <summary>
        /// Returns true if the condition passes (field should be included),
        /// false if it fails (field should be skipped).
        /// Returns true if condition is null (no condition = always include).
        /// </summary>
        public bool Evaluate(string? condition, FiscalContext context)
        {
            if (string.IsNullOrWhiteSpace(condition))
            {
                return true;
            }

            // Parse: "OpsContext.ServiceCharge > 0"
            //         ↑ left                  ↑ op ↑ right
            (string left, string op, string right) = ParseCondition(condition);

            object? leftValue = ResolveLeft(left, context);
            return EvaluateComparison(leftValue, op, right);
        }

        private static (string left, string op, string right) ParseCondition(
            string condition)
        {
            string[] operators = ["!=", "==", ">=", "<=", ">", "<"];

            foreach (string op in operators)
            {
                int index = condition.IndexOf(op, StringComparison.Ordinal);
                if (index < 0)
                {
                    continue;
                }

                string left = condition[..index].Trim();
                string right = condition[(index + op.Length)..].Trim();
                return (left, op, right);
            }

            throw new InvalidOperationException(
                $"IncludeIf condition '{condition}' could not be parsed. " +
                $"Supported operators: !=, ==, >=, <=, >, <");
        }

        private object? ResolveLeft(string left, FiscalContext context)
        {
            if (left.StartsWith("OpsContext.", StringComparison.OrdinalIgnoreCase))
            {
                return _posResolver.Resolve(left, context);
            }

            if (left.StartsWith("Input:", StringComparison.OrdinalIgnoreCase))
            {
                return _inputResolver.Resolve(left, context);
            }

            return null;
        }

        private static bool EvaluateComparison(
            object? leftValue,
            string op,
            string right)
        {
            // null checks
            if (right == "null")
            {
                return op == "!=" ? leftValue is not null : leftValue is null;
            }

            // bool checks
            if (right == "true" || right == "false")
            {
                bool rightBool = right == "true";
                bool leftBool = leftValue is bool b && b;
                return op == "==" ? leftBool == rightBool : leftBool != rightBool;
            }

            // numeric comparison
            if (decimal.TryParse(right,
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out decimal rightDecimal))
            {
                decimal leftDecimal = leftValue switch
                {
                    decimal d => d,
                    int i => i,
                    double d => (decimal)d,
                    string s when decimal.TryParse(s,
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out decimal parsed) => parsed,
                    _ => 0m
                };

                return op switch
                {
                    ">" => leftDecimal > rightDecimal,
                    "<" => leftDecimal < rightDecimal,
                    ">=" => leftDecimal >= rightDecimal,
                    "<=" => leftDecimal <= rightDecimal,
                    "==" => leftDecimal == rightDecimal,
                    "!=" => leftDecimal != rightDecimal,
                    _ => false
                };
            }

            // string comparison
            string leftString = leftValue?.ToString() ?? string.Empty;
            return op switch
            {
                "==" => leftString == right,
                "!=" => leftString != right,
                _ => false
            };
        }
    }
}
