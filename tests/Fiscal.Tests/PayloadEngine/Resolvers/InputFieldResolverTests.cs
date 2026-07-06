using Fiscal.Core.Context;
using Fiscal.Core.Domain;
using Fiscal.Core.PayloadEngine.Resolution;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Tests.PayloadEngine.Resolvers
{
    public class InputFieldResolverTests
    {
        [Fact]
        public void ResolvesValueFromOperatorInput()
        {
            var resolver = new InputFieldResolver();
            var context = new FiscalContext
            {
                Check = new PosCheck(new DynamicRecord())
            };
            context.OperatorInput.Set("BuyerTaxNumber", "TAX-12345");

            var result = resolver.Resolve("Input:BuyerTaxNumber", context);

            Assert.Equal("TAX-12345", result);
        }

        [Fact]
        public void ReturnsNullWhenInputNotPresent()
        {
            var resolver = new InputFieldResolver();
            var context = new FiscalContext
            {
                Check = new PosCheck(new DynamicRecord())
            };

            // Nothing set in OperatorInput
            var result = resolver.Resolve("Input:BuyerTaxNumber", context);

            Assert.Null(result);
        }

        [Fact]
        public void ResolvesFiscalNoForCreditTransaction()
        {
            var resolver = new InputFieldResolver();
            var context = new FiscalContext
            {
                Check = new PosCheck(new DynamicRecord())
            };
            context.OperatorInput.Set("fiscalNo", "FISC-20260701-0001");

            var result = resolver.Resolve("Input:fiscalNo", context);

            Assert.Equal("FISC-20260701-0001", result);
        }

        [Fact]
        public void PrefixIsInput()
        {
            var resolver = new InputFieldResolver();
            Assert.Equal("Input", resolver.Prefix);
        }
    }
}
