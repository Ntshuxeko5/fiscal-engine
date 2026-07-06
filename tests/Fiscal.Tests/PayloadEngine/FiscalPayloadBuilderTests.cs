using Fiscal.Core.Context;
using Fiscal.Core.Domain;
using Fiscal.Core.PayloadEngine;
using Fiscal.Core.PayloadEngine.Config;
using Fiscal.Core.PayloadEngine.Resolution;
using Fiscal.Core.Transformations;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Fiscal.Tests.PayloadEngine
{
    public class FiscalPayloadBuilderTests
    {
        private static FiscalEngineConfig BuildConfig()
        {
            var json = JsonSerializer.Serialize(new
            {
                TenderMediaNumber = 4,
                ModeValues = new Dictionary<string, string>
                {
                    ["Invoice"] = "INVOICE",
                    ["Credit"] = "CREDIT"
                },
                StaticValues = new Dictionary<string, string>
                {
                    ["SellerName"] = "Acme Restaurant Group",
                    ["DeviceCode"] = "FD-100"
                },
                Payload = new
                {
                    SellerName = "Config:SellerName",
                    TransactionId = "OpsContext.TransactionId",
                    FiscalId = "Generated:guid",
                    LineTotal = "Calc:lineTotal",
                    FolioType = "Mode:documentType",
                    BuyerTax = "Input:BuyerTaxNumber"
                }
            });

            return JsonSerializer.Deserialize<FiscalEngineConfig>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
        }

        private static FiscalPayloadBuilder BuildEngine(FiscalEngineConfig config)
        {
            var registry = new TransformationRegistry(
                new[] { new LineTotalTransformation() });

            var resolvers = new IFieldResolver[]
            {
            new PosFieldResolver(),
            new StaticFieldResolver(config),
            new GeneratedFieldResolver(),
            new InputFieldResolver(),
            new CalculatedFieldResolver(registry),
            new ModeFieldResolver(config)
            };

            return new FiscalPayloadBuilder(config, resolvers);
        }

        private static FiscalContext BuildContext()
        {
            var check = new PosCheck(new DynamicRecord(
                new Dictionary<string, object?>
                {
                    ["TransactionId"] = "TXN-001",
                    ["TotalDue"] = 150.00m,
                    ["LineItems"] = new List<DynamicRecord>
                    {
                    new DynamicRecord(new Dictionary<string, object?>
                    {
                        ["Sku"]    = "ITEM-001",
                        ["Amount"] = 150.00m
                    })
                    }
                }));

            return new FiscalContext
            {
                Check = check,
                Mode = TransactionMode.Invoice
            };
        }

        [Fact]
        public void Build_ResolvesStaticValue()
        {
            var config = BuildConfig();
            var builder = BuildEngine(config);
            var context = BuildContext();

            var payload = builder.Build(context);

            Assert.Equal("Acme Restaurant Group", payload.Get<string>("SellerName"));
        }

        [Fact]
        public void Build_ResolvesPosField()
        {
            var config = BuildConfig();
            var builder = BuildEngine(config);
            var context = BuildContext();

            var payload = builder.Build(context);

            Assert.Equal("TXN-001", payload.Get<string>("TransactionId"));
        }

        [Fact]
        public void Build_ResolvesGeneratedGuid()
        {
            var config = BuildConfig();
            var builder = BuildEngine(config);
            var context = BuildContext();

            var payload = builder.Build(context);

            string? fiscalId = payload.Get<string>("FiscalId");
            Assert.NotNull(fiscalId);
            Assert.True(Guid.TryParse(fiscalId, out _));
        }

        [Fact]
        public void Build_ResolvesCalculatedLineTotal()
        {
            var config = BuildConfig();
            var builder = BuildEngine(config);
            var context = BuildContext();

            var payload = builder.Build(context);

            Assert.Equal(150.00m, payload.Get<decimal>("LineTotal"));
        }

        [Fact]
        public void Build_ResolvesModeValue()
        {
            var config = BuildConfig();
            var builder = BuildEngine(config);
            var context = BuildContext();

            var payload = builder.Build(context);

            Assert.Equal("INVOICE", payload.Get<string>("FolioType"));
        }

        [Fact]
        public void Build_OmitsNullFields()
        {
            var config = BuildConfig();
            var builder = BuildEngine(config);
            var context = BuildContext();

            // BuyerTax maps to Input:BuyerTaxNumber - nothing in OperatorInput
            var payload = builder.Build(context);

            Assert.False(payload.Has("BuyerTax"),
                "Fields that resolve to null should be omitted from the payload.");
        }

        [Fact]
        public void Build_IncludesInputFieldWhenPresent()
        {
            var config = BuildConfig();
            var builder = BuildEngine(config);
            var context = BuildContext();

            context.OperatorInput.Set("BuyerTaxNumber", "TAX-9999");

            var payload = builder.Build(context);

            Assert.Equal("TAX-9999", payload.Get<string>("BuyerTax"));
        }

        [Fact]
        public void Build_CreditMode_ResolvesCreditModeValue()
        {
            var config = BuildConfig();
            var builder = BuildEngine(config);
            var context = BuildContext();
            context.Mode = TransactionMode.Credit;

            var payload = builder.Build(context);

            Assert.Equal("CREDIT", payload.Get<string>("FolioType"));
        }
    }
}
