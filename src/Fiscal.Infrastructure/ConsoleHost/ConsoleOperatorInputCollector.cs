using Fiscal.Core.Context;
using Fiscal.Core.Domain;
using Fiscal.Core.Interfaces;
using Fiscal.Core.PayloadEngine;
using Fiscal.Core.PayloadEngine.Config;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Infrastructure.ConsoleHost
{
    /// <summary>
    /// Console-based operator input collector. Prompts stdin for required
    /// values based on transaction mode and check type.
    ///
    /// Used by Fiscal.Console for interactive demos.
    /// In WPF, replace with a dialog-based implementation.
    /// </summary>
    public class ConsoleOperatorInputCollector : IOperatorInputCollector
    {
        private readonly BuyerInfoFormConfig? _formConfig;

        public ConsoleOperatorInputCollector(
            BuyerInfoFormConfig? formConfig = null)
        {
            _formConfig = formConfig;
        }

        public Task CollectAsync(FiscalContext context)
        {
            if (context.Mode == TransactionMode.Credit)
            {
                CollectCreditInput(context);
            }

            if (context.Check.Data.Get<bool>("IsB2B"))
            {
                CollectB2BInput(context);
            }

            return Task.CompletedTask;
        }

        private static void CollectCreditInput(FiscalContext context)
        {
            System.Console.WriteLine();
            System.Console.WriteLine(
                "Credit transaction. Enter the original fiscal number:");
            System.Console.Write("Original Fiscal No: ");

            string? input = System.Console.ReadLine()?.Trim();
            if (!string.IsNullOrWhiteSpace(input))
            {
                context.OperatorInput.Set("fiscalNo", input);
            }
        }

        private void CollectB2BInput(FiscalContext context)
        {
            // Use form config fields if available,
            // otherwise fall back to hardcoded fields
            if (_formConfig is not null)
            {
                CollectFromFormConfig(context);
            }
            else
            {
                CollectB2BFallback(context);
            }
        }

        private void CollectFromFormConfig(FiscalContext context)
        {
            var validator = new BuyerInfoFormValidator(_formConfig!);

            while (true)
            {
                System.Console.WriteLine();
                System.Console.WriteLine(
                    $"=== {_formConfig!.TriggerLabel} ===");

                var values = new Dictionary<string, string?>();

                foreach (BuyerInfoField field in _formConfig.Fields)
                {
                    System.Console.Write($"{field.Label}: ");
                    string? input = System.Console.ReadLine()?.Trim();
                    values[field.Key] = input;
                }

                // Validate before accepting
                var errors = validator.Validate(
                    values.ToDictionary(k => k.Key, v => v.Value));

                if (errors.Count == 0)
                {
                    // All valid - write to operator input
                    foreach (var (key, value) in values)
                    {
                        if (!string.IsNullOrWhiteSpace(value))
                        {
                            context.OperatorInput.Set(key, value!);
                        }
                    }
                    break;
                }

                // Show errors and re-prompt
                System.Console.WriteLine();
                System.Console.WriteLine("Please fix the following:");
                foreach (var (_, error) in errors)
                {
                    System.Console.WriteLine($"  • {error}");
                }
            }
        }

        private static void CollectB2BFallback(FiscalContext context)
        {
            System.Console.WriteLine();
            System.Console.WriteLine("B2B transaction. Enter buyer information:");

            System.Console.Write("Buyer Tax Number : ");
            string? taxNumber = System.Console.ReadLine()?.Trim();
            if (!string.IsNullOrWhiteSpace(taxNumber))
            {
                context.OperatorInput.Set("BuyerTaxNumber", taxNumber);
            }

            System.Console.Write("Buyer Name       : ");
            string? name = System.Console.ReadLine()?.Trim();
            if (!string.IsNullOrWhiteSpace(name))
            {
                context.OperatorInput.Set("BuyerName", name);
            }
        }
    }
}
