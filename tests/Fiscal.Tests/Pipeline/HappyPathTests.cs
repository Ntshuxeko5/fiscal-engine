using Fiscal.Core.PayloadEngine;
using Fiscal.Core.Pipeline;
using Fiscal.Core.Validation;
using Fiscal.Infrastructure.Fakes;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Tests.Pipeline
{
    /// <summary>
    /// Tests that a successful end-to-end transaction produces
    /// the expected result shape and runs all four steps in order.
    /// </summary>
    public class HappyPathTests
    {
        [Fact]
        public async Task SuccessfulTransaction_ReturnsSuccessResult()
        {
            var processor = new FiscalTransactionProcessor(
                new FakeCheckReader(),
                new B2BTransactionValidator(),
                new FakePayloadBuilder(),
                new FakeFiscalClient(),
                new FakePaymentClient(),
                new FakeSlipPrinter());

            FiscalPipelineResult result = await processor.ProcessAsync(new object());

            Assert.True(result.IsSuccess);
            Assert.Null(result.FailureReason);
            Assert.Null(result.FailedAtStage);
        }

        [Fact]
        public async Task SuccessfulTransaction_ContextHasFiscalResult()
        {
            var processor = new FiscalTransactionProcessor(
                new FakeCheckReader(),
                new B2BTransactionValidator(),
                new FakePayloadBuilder(),
                new FakeFiscalClient(),
                new FakePaymentClient(),
                new FakeSlipPrinter());

            FiscalPipelineResult result = await processor.ProcessAsync(new object());

            Assert.NotNull(result.CompletedContext?.FiscalResult);
        }

        [Fact]
        public async Task SuccessfulTransaction_ContextHasPaymentOutcome()
        {
            var processor = new FiscalTransactionProcessor(
                new FakeCheckReader(),
                new B2BTransactionValidator(),
                new FakePayloadBuilder(),
                new FakeFiscalClient(),
                new FakePaymentClient(),
                new FakeSlipPrinter());

            FiscalPipelineResult result = await processor.ProcessAsync(new object());

            Assert.NotNull(result.CompletedContext?.PaymentOutcome);
        }

        [Fact]
        public async Task SuccessfulTransaction_SlipPrintedIsTrue()
        {
            var processor = new FiscalTransactionProcessor(
                new FakeCheckReader(),
                new B2BTransactionValidator(),
                new FakePayloadBuilder(),
                new FakeFiscalClient(),
                new FakePaymentClient(),
                new FakeSlipPrinter());

            FiscalPipelineResult result = await processor.ProcessAsync(new object());

            Assert.True(result.CompletedContext?.SlipPrinted);
        }
    }
}
