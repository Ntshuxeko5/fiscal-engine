using Fiscal.Core.Context;
using Fiscal.Core.Domain;
using Fiscal.Core.PayloadEngine.Resolution;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Tests.PayloadEngine.Resolvers
{
    public class PosFieldResolverTests
    {
        private static FiscalContext BuildContext(Dictionary<string, object?> fields)
        {
            return new FiscalContext
            {
                Check = new PosCheck(new DynamicRecord(fields))
            };
        }

        [Fact]
        public void ResolvesStringFieldFromPosCheck()
        {
            var resolver = new PosFieldResolver();
            var context = BuildContext(new Dictionary<string, object?>
            {
                ["TransactionId"] = "TXN-001"
            });

            var result = resolver.Resolve("OpsContext.TransactionId", context);

            Assert.Equal("TXN-001", result);
        }

        [Fact]
        public void ResolvesDecimalFieldFromPosCheck()
        {
            var resolver = new PosFieldResolver();
            var context = BuildContext(new Dictionary<string, object?>
            {
                ["TotalDue"] = 150.00m
            });

            var result = resolver.Resolve("OpsContext.TotalDue", context);

            Assert.Equal(150.00m, result);
        }

        [Fact]
        public void ReturnsNullForMissingField()
        {
            var resolver = new PosFieldResolver();
            var context = BuildContext(new Dictionary<string, object?>());

            var result = resolver.Resolve("OpsContext.NonExistentField", context);

            Assert.Null(result);
        }

        [Fact]
        public void ReturnsNullForMalformedMapping()
        {
            var resolver = new PosFieldResolver();
            var context = BuildContext(new Dictionary<string, object?>());

            // No dot separator - cannot extract field name
            var result = resolver.Resolve("OpsContext", context);

            Assert.Null(result);
        }

        [Fact]
        public void PrefixIsOpsContext()
        {
            var resolver = new PosFieldResolver();
            Assert.Equal("OpsContext", resolver.Prefix);
        }
    }
}
