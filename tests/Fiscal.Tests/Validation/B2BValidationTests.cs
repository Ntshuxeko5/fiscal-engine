using Fiscal.Core.PayloadEngine;
using Fiscal.Core.Pipeline;
using Fiscal.Core.Validation;
using Fiscal.Infrastructure.Fakes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Tests.Validation
{
    /// <summary>
    /// Tests for the B2B tax number validation rule.
    /// This rule is the one runtime business rule enforced
    /// before fiscalization - all other validation is on the POS.
    /// </summary>
    public class B2BValidationTests
    {
        [Fact]
        public async Task B2BTransaction_WithoutTaxNumber_FailsBeforeFiscalization()
        {
            // Arrange - B2B check reader that sets IsB2B = true
            var fiscalClient = new FakeFiscalClient();
            var paymentClient = new FakePaymentClient();

            var processor = new FiscalTransactionProcessor(
                new FakeB2BCheckReader(),
                new B2BTransactionValidator(),
                new FakePayloadBuilder(),
                fiscalClient,
                paymentClient,
                new FakeSlipPrinter());

            // Act
            FiscalPipelineResult result = await processor.ProcessAsync(new object());

            // Assert - stopped at validation, fiscal device never called
            Assert.False(result.IsSuccess);
            Assert.Equal(PipelineFailureStage.Validation, result.FailedAtStage);
            Assert.False(paymentClient.WasCalled,
                "Payment must not run if validation failed.");
        }

        [Fact]
        public async Task NonB2BTransaction_SkipsB2BValidation_Succeeds()
        {
            var processor = new FiscalTransactionProcessor(
                new FakeCheckReader(),        // IsB2B = false
                new B2BTransactionValidator(),
                new FakePayloadBuilder(),
                new FakeFiscalClient(),
                new FakePaymentClient(),
                new FakeSlipPrinter());

            FiscalPipelineResult result = await processor.ProcessAsync(new object());

            Assert.True(result.IsSuccess);
        }
    }
}
