using Fiscal.Core.Context;
using Fiscal.Core.Domain;
using Fiscal.Core.PayloadEngine.Config;
using Fiscal.Core.PayloadEngine.Resolution;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Fiscal.Tests.PayloadEngine.Resolvers
{
    public class StaticFieldResolverTests
    {
        private static FiscalEngineConfig BuildConfig(
            Dictionary<string, string> staticValues)
        {
            // Build config via JSON round-trip so Payload is a valid JsonElement
            var json = JsonSerializer.Serialize(new
            {
                TenderMediaNumber = 4,
                StaticValues = staticValues,
                ModeValues = new Dictionary<string, string>(),
                Payload = new { }
            });

            return JsonSerializer.Deserialize<FiscalEngineConfig>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }

        private static FiscalContext EmptyContext() => new FiscalContext
        {
            Check = new PosCheck(new DynamicRecord())
        };

        [Fact]
        public void ResolvesKnownStaticValue()
        {
            var config = BuildConfig(new Dictionary<string, string>
            {
                ["SellerName"] = "Acme Restaurant Group"
            });
            var resolver = new StaticFieldResolver(config);

            var result = resolver.Resolve("Config:SellerName", EmptyContext());

            Assert.Equal("Acme Restaurant Group", result);
        }

        [Fact]
        public void ReturnsNullForUnknownKey()
        {
            var config = BuildConfig(new Dictionary<string, string>());
            var resolver = new StaticFieldResolver(config);

            var result = resolver.Resolve("Config:NonExistent", EmptyContext());

            Assert.Null(result);
        }

        [Fact]
        public void ReturnsNullForMalformedMapping()
        {
            var config = BuildConfig(new Dictionary<string, string>());
            var resolver = new StaticFieldResolver(config);

            var result = resolver.Resolve("Config", EmptyContext());

            Assert.Null(result);
        }

        [Fact]
        public void PrefixIsConfig()
        {
            var config = BuildConfig(new Dictionary<string, string>());
            var resolver = new StaticFieldResolver(config);

            Assert.Equal("Config", resolver.Prefix);
        }
    }
}
