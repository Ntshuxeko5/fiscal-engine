using Fiscal.Core.Context;
using Fiscal.Core.Domain;
using Fiscal.Core.Interfaces;
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
                "Credit transaction detected. Please enter the original fiscal number:");
            System.Console.Write("Original Fiscal No: ");

            string? input = System.Console.ReadLine()?.Trim();
            if (!string.IsNullOrWhiteSpace(input))
            {
                context.OperatorInput.Set("fiscalNo", input);
            }
        }

        private static void CollectB2BInput(FiscalContext context)
        {
            System.Console.WriteLine();
            System.Console.WriteLine(
                "B2B transaction. Please enter buyer information:");

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

            System.Console.Write("Buyer Address    : ");
            string? address = System.Console.ReadLine()?.Trim();
            if (!string.IsNullOrWhiteSpace(address))
            {
                context.OperatorInput.Set("BuyerAddress", address);
            }
        }
    }
}
