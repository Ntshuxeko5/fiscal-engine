using Fiscal.Core.Interfaces;
using Fiscal.Core.PayloadEngine;
using Fiscal.Core.PayloadEngine.Config;
using Fiscal.Core.Pipeline;
using Fiscal.Core.Validation;
using Fiscal.Infrastructure.Fakes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Tests.Validation
{
    /// <summary>
    /// Tests B2B validation using the full form config,
    /// proving the pipeline stops at the right stage with the right reason.
    /// </summary>
    public class B2BFormValidationTests
    {
        private static BuyerInfoFormConfig BuildFormConfig() => new()
        {
            TriggerLabel = "B2B Sale",
            Fields =
            [
                new BuyerInfoField
            {
                Key      = "BuyerTaxNumber",
                Label    = "Tax Number",
                Required = true,
                Min      = 9,
                Max      = 9
            },
            new BuyerInfoField
            {
                Key      = "BuyerName",
                Label    = "Company Name",
                Required = true
            }
            ]
        };

        private static FiscalTransactionProcessor BuildProcessor(
            IOperatorInputCollector collector,
            BuyerInfoFormConfig formConfig)
        {
            return new FiscalTransactionProcessor(
                new FakeB2BCheckReader(),
                collector,
                new B2BTransactionValidator(formConfig),
                new FakePayloadBuilder(),
                new FakeFiscalClient(),
                new FakePaymentClient(),
                new FakeSlipPrinter());
        }

        [Fact]
        public async Task B2B_WithValidFormInput_Succeeds()
        {
            var collector = new FakeOperatorInputCollector(
                buyerValues: new Dictionary<string, string>
                {
                    ["BuyerTaxNumber"] = "123456789",
                    ["BuyerName"] = "Acme Corp"
                });

            var processor = BuildProcessor(collector, BuildFormConfig());
            var result = await processor.ProcessAsync(new object());

            Assert.True(result.IsSuccess);
        }

        [Fact]
        public async Task B2B_WithTaxNumberTooShort_FailsAtValidation()
        {
            var collector = new FakeOperatorInputCollector(
                buyerValues: new Dictionary<string, string>
                {
                    ["BuyerTaxNumber"] = "12345",   // too short
                    ["BuyerName"] = "Acme Corp"
                });

            var processor = BuildProcessor(collector, BuildFormConfig());
            var result = await processor.ProcessAsync(new object());

            Assert.False(result.IsSuccess);
            Assert.Equal(PipelineFailureStage.Validation, result.FailedAtStage);
            Assert.Contains("exactly 9", result.FailureReason,
                StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task B2B_WithMissingCompanyName_FailsAtValidation()
        {
            var collector = new FakeOperatorInputCollector(
                buyerValues: new Dictionary<string, string>
                {
                    ["BuyerTaxNumber"] = "123456789",
                    ["BuyerName"] = ""          // required but empty
                });

            var processor = BuildProcessor(collector, BuildFormConfig());
            var result = await processor.ProcessAsync(new object());

            Assert.False(result.IsSuccess);
            Assert.Equal(PipelineFailureStage.Validation, result.FailedAtStage);
        }

        [Fact]
        public async Task B2B_ValidationFailed_PaymentNeverCalled()
        {
            var paymentClient = new FakePaymentClient();
            var collector = new FakeOperatorInputCollector(
                buyerValues: new Dictionary<string, string>
                {
                    ["BuyerTaxNumber"] = "123",     // invalid
                    ["BuyerName"] = "Acme Corp"
                });

            var processor = new FiscalTransactionProcessor(
                new FakeB2BCheckReader(),
                collector,
                new B2BTransactionValidator(BuildFormConfig()),
                new FakePayloadBuilder(),
                new FakeFiscalClient(),
                paymentClient,
                new FakeSlipPrinter());

            await processor.ProcessAsync(new object());

            Assert.False(paymentClient.WasCalled,
                "Payment must never run if B2B validation failed.");
        }
    }
}
