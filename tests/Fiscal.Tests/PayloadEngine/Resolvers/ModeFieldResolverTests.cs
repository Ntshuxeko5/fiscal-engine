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
    public class ModeFieldResolverTests
    {
        private static FiscalEngineConfig BuildConfig(
            Dictionary<string, string> modeValues)
        {
            var json = JsonSerializer.Serialize(new
            {
                TenderMediaNumber = 4,
                StaticValues = new Dictionary<string, string>(),
                ModeValues = modeValues,
                Payload = new { }
            });

            return JsonSerializer.Deserialize<FiscalEngineConfig>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }

        private static FiscalContext BuildContext(TransactionMode mode) =>
            new FiscalContext
            {
                Check = new PosCheck(new DynamicRecord()),
                Mode = mode
            };

        [Fact]
        public void ResolvesInvoiceModeValue()
        {
            var config = BuildConfig(new Dictionary<string, string>
            {
                ["Invoice"] = "INVOICE",
                ["Credit"] = "CREDIT"
            });
            var resolver = new ModeFieldResolver(config);

            var result = resolver.Resolve(
                "Mode:documentType",
                BuildContext(TransactionMode.Invoice));

            Assert.Equal("INVOICE", result);
        }

        [Fact]
        public void ResolvesCreditModeValue()
        {
            var config = BuildConfig(new Dictionary<string, string>
            {
                ["Invoice"] = "INVOICE",
                ["Credit"] = "CREDIT"
            });
            var resolver = new ModeFieldResolver(config);

            var result = resolver.Resolve(
                "Mode:documentType",
                BuildContext(TransactionMode.Credit));

            Assert.Equal("CREDIT", result);
        }

        [Fact]
        public void SupportsClientSpecificCreditAbbreviation()
        {
            // Some fiscal devices expect "C" not "CREDIT"
            var config = BuildConfig(new Dictionary<string, string>
            {
                ["Invoice"] = "I",
                ["Credit"] = "C"
            });
            var resolver = new ModeFieldResolver(config);

            var result = resolver.Resolve(
                "Mode:documentType",
                BuildContext(TransactionMode.Credit));

            Assert.Equal("C", result);
        }

        [Fact]
        public void ReturnsNullWhenModeNotSet()
        {
            var config = BuildConfig(new Dictionary<string, string>());
            var resolver = new ModeFieldResolver(config);
            var context = new FiscalContext
            {
                Check = new PosCheck(new DynamicRecord()),
                Mode = null
            };

            var result = resolver.Resolve("Mode:documentType", context);

            Assert.Null(result);
        }

        [Fact]
        public void PrefixIsMode()
        {
            var config = BuildConfig(new Dictionary<string, string>());
            var resolver = new ModeFieldResolver(config);
            Assert.Equal("Mode", resolver.Prefix);
        }
    }
}
