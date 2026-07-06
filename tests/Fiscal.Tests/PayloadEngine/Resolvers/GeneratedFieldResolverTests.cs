using Fiscal.Core.Context;
using Fiscal.Core.Domain;
using Fiscal.Core.PayloadEngine.Resolution;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Tests.PayloadEngine.Resolvers
{
    public class GeneratedFieldResolverTests
    {
        private static FiscalContext EmptyContext() => new FiscalContext
        {
            Check = new PosCheck(new DynamicRecord())
        };

        [Fact]
        public void GeneratesValidGuid()
        {
            var resolver = new GeneratedFieldResolver();

            var result = resolver.Resolve("Generated:guid", EmptyContext());

            Assert.NotNull(result);
            Assert.True(Guid.TryParse(result.ToString(), out _),
                "Generated value should be a valid GUID.");
        }

        [Fact]
        public void GeneratesTwoDistinctGuids()
        {
            var resolver = new GeneratedFieldResolver();
            var context = EmptyContext();

            var first = resolver.Resolve("Generated:guid", context);
            var second = resolver.Resolve("Generated:guid", context);

            Assert.NotEqual(first, second);
        }

        [Fact]
        public void GeneratesValidIso8601Timestamp()
        {
            var resolver = new GeneratedFieldResolver();

            var result = resolver.Resolve("Generated:timestamp", EmptyContext());

            Assert.NotNull(result);
            Assert.True(
                DateTime.TryParse(result.ToString(),
                    null,
                    System.Globalization.DateTimeStyles.RoundtripKind,
                    out _),
                "Generated value should be a valid ISO 8601 timestamp.");
        }

        [Fact]
        public void ThrowsForUnknownGenerator()
        {
            var resolver = new GeneratedFieldResolver();

            Assert.Throws<InvalidOperationException>(() =>
                resolver.Resolve("Generated:unknown", EmptyContext()));
        }

        [Fact]
        public void PrefixIsGenerated()
        {
            var resolver = new GeneratedFieldResolver();
            Assert.Equal("Generated", resolver.Prefix);
        }
    }
}
