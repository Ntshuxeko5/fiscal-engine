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
    public class ConditionalPostingTests
    {
        private static FiscalPayloadBuilder BuildEngine(object payloadShape)
        {
            var json = JsonSerializer.Serialize(new
            {
                TenderMediaNumber = 4,
                StaticValues = new Dictionary<string, string>(),
                ModeValues = new Dictionary<string, string>(),
                Payload = payloadShape
            });

            var config = JsonSerializer.Deserialize<FiscalEngineConfig>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;

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

        private static FiscalContext BuildContext(decimal serviceCharge) =>
            new FiscalContext
            {
                Check = new PosCheck(new DynamicRecord(
                    new Dictionary<string, object?>
                    {
                        ["TransactionId"] = "TXN-001",
                        ["TotalDue"] = 150.00m,
                        ["ServiceCharge"] = serviceCharge,
                        ["LineItems"] = new List<DynamicRecord>()
                    })),
                Mode = TransactionMode.Invoice
            };

        [Fact]
        public void FieldWithPassingCondition_IsIncluded()
        {
            var engine = BuildEngine(new
            {
                ServiceCharge = new
                {
                    Value = "OpsContext.ServiceCharge",
                    IncludeIf = "OpsContext.ServiceCharge > 0"
                }
            });

            var payload = engine.Build(BuildContext(serviceCharge: 15.00m));

            Assert.True(payload.Has("ServiceCharge"),
                "ServiceCharge should be included when > 0.");
            Assert.Equal(15.00m, payload.Get<decimal>("ServiceCharge"));
        }

        [Fact]
        public void FieldWithFailingCondition_IsOmitted()
        {
            var engine = BuildEngine(new
            {
                ServiceCharge = new
                {
                    Value = "OpsContext.ServiceCharge",
                    IncludeIf = "OpsContext.ServiceCharge > 0"
                }
            });

            var payload = engine.Build(BuildContext(serviceCharge: 0m));

            Assert.False(payload.Has("ServiceCharge"),
                "ServiceCharge should be omitted when == 0.");
        }

        [Fact]
        public void FieldWithNoCondition_IsAlwaysIncluded()
        {
            var engine = BuildEngine(new
            {
                TransactionId = "OpsContext.TransactionId"
            });

            var payload = engine.Build(BuildContext(serviceCharge: 0m));

            Assert.True(payload.Has("TransactionId"));
        }

        [Fact]
        public void MultipleConditionalFields_EachEvaluatedIndependently()
        {
            var engine = BuildEngine(new
            {
                ServiceCharge = new
                {
                    Value = "OpsContext.ServiceCharge",
                    IncludeIf = "OpsContext.ServiceCharge > 0"
                },
                TransactionId = "OpsContext.TransactionId"
            });

            // ServiceCharge = 0, so it should be omitted
            // TransactionId has no condition, so it should be included
            var payload = engine.Build(BuildContext(serviceCharge: 0m));

            Assert.False(payload.Has("ServiceCharge"));
            Assert.True(payload.Has("TransactionId"));
        }
    }
}
