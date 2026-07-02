using Fiscal.Core.Interfaces;
using Fiscal.Core.Pipeline;
using Fiscal.Core.PayloadEngine;
using Fiscal.Core.Validation;
using Fiscal.Infrastructure.Fakes;

Console.WriteLine("=== Fiscal Engine - Console Runner ===");
Console.WriteLine();

// ── Wire up dependencies manually (no DI container yet) ───────────────────
// Each interface gets its fake implementation. When real adapters are
// built, this is the only place that changes - the pipeline itself
// doesn't care whether it's talking to a fake or a real fiscal device.

ICheckReader checkReader = new FakeCheckReader();
ITransactionValidator validator = new B2BTransactionValidator();
IFiscalPayloadBuilder payloadBuilder = new FakePayloadBuilder();
FakeFiscalClient fiscalClient = new FakeFiscalClient();
FakePaymentClient paymentClient = new FakePaymentClient();
ISlipPrinter slipPrinter = new FakeSlipPrinter();

var processor = new FiscalTransactionProcessor(
    checkReader,
    validator,
    payloadBuilder,
    fiscalClient,
    paymentClient,
    slipPrinter);

// ── Run 1: Happy path ─────────────────────────────────────────────────────
Console.WriteLine(">> Scenario 1: Successful transaction");
Console.WriteLine();

FiscalPipelineResult result = await processor.ProcessAsync(new object());

if (result.IsSuccess)
{
    Console.WriteLine();
    Console.WriteLine("Pipeline completed successfully.");
}
else
{
    Console.WriteLine($"Pipeline failed at: {result.FailedAtStage}");
    Console.WriteLine($"Reason: {result.FailureReason}");
}

Console.WriteLine();
Console.WriteLine("────────────────────────────────────────");
Console.WriteLine();

// ── Run 2: Fiscal failure (gate test) ────────────────────────────────────
Console.WriteLine(">> Scenario 2: Fiscal device rejects transaction");
Console.WriteLine();

fiscalClient.ShouldFail = true;
paymentClient.Reset(); // add this

FiscalPipelineResult failResult = await processor.ProcessAsync(new object());

if (!failResult.IsSuccess)
{
    Console.WriteLine($"Pipeline stopped at : {failResult.FailedAtStage}");
    Console.WriteLine($"Reason              : {failResult.FailureReason}");
    Console.WriteLine($"Payment was called  : {paymentClient.WasCalled}");
    Console.WriteLine($"Slip was printed    : {failResult.CompletedContext?.SlipPrinted ?? false}");
}

Console.WriteLine();
Console.WriteLine("=== Done ===");
