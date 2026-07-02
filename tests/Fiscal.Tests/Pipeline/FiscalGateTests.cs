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
    /// Tests for the fiscal gate - the most critical correctness property
    /// in the system. Payment and printing must never be reachable after
    /// a fiscal failure, regardless of any other state.
    /// </summary>
    public class FiscalGateTests
    {
        private static FiscalTransactionProcessor BuildProcessor(
            FakeFiscalClient fiscalClient,
            FakePaymentClient paymentClient,
            FakeSlipPrinter slipPrinter)
        {
            return new FiscalTransactionProcessor(
                new FakeCheckReader(),
                new B2BTransactionValidator(),
                new FakePayloadBuilder(),
                fiscalClient,
                paymentClient,
                slipPrinter);
        }

        [Fact]
        public async Task WhenFiscalFails_PaymentIsNeverCalled()
        {
            var fiscalClient = new FakeFiscalClient { ShouldFail = true };
            var paymentClient = new FakePaymentClient();
            var slipPrinter = new FakeSlipPrinter();

            var processor = BuildProcessor(fiscalClient, paymentClient, slipPrinter);

            await processor.ProcessAsync(new object());

            Assert.False(paymentClient.WasCalled,
                "Payment must never run after a fiscal failure.");
        }

        [Fact]
        public async Task WhenFiscalFails_SlipIsNeverPrinted()
        {
            var fiscalClient = new FakeFiscalClient { ShouldFail = true };
            var paymentClient = new FakePaymentClient();
            var slipPrinter = new FakeSlipPrinter();

            var processor = BuildProcessor(fiscalClient, paymentClient, slipPrinter);

            await processor.ProcessAsync(new object());

            Assert.False(slipPrinter.WasCalled,
                "Slip must never print after a fiscal failure.");
        }

        [Fact]
        public async Task WhenFiscalFails_ResultIndicatesFiscalizationStage()
        {
            var fiscalClient = new FakeFiscalClient { ShouldFail = true };
            var paymentClient = new FakePaymentClient();
            var slipPrinter = new FakeSlipPrinter();

            var processor = BuildProcessor(fiscalClient, paymentClient, slipPrinter);

            FiscalPipelineResult result = await processor.ProcessAsync(new object());

            Assert.False(result.IsSuccess);
            Assert.Equal(PipelineFailureStage.Fiscalization, result.FailedAtStage);
        }

        [Fact]
        public async Task WhenFiscalSucceeds_PaymentRuns()
        {
            var fiscalClient = new FakeFiscalClient { ShouldFail = false };
            var paymentClient = new FakePaymentClient();
            var slipPrinter = new FakeSlipPrinter();

            var processor = BuildProcessor(fiscalClient, paymentClient, slipPrinter);

            await processor.ProcessAsync(new object());

            Assert.True(paymentClient.WasCalled,
                "Payment must run after a successful fiscal response.");
        }

        [Fact]
        public async Task WhenFiscalSucceeds_SlipIsPrinted()
        {
            var fiscalClient = new FakeFiscalClient { ShouldFail = false };
            var paymentClient = new FakePaymentClient();
            var slipPrinter = new FakeSlipPrinter();

            var processor = BuildProcessor(fiscalClient, paymentClient, slipPrinter);

            await processor.ProcessAsync(new object());

            Assert.True(slipPrinter.WasCalled,
                "Slip must print after a successful transaction.");
        }
    }
}
