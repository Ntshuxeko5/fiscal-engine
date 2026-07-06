using Fiscal.Core.Domain;
using Fiscal.Core.Interfaces;
using Fiscal.Core.PayloadEngine;
using Fiscal.Core.PayloadEngine.Config;
using Fiscal.Core.PayloadEngine.Resolution;
using Fiscal.Core.Pipeline;
using Fiscal.Core.Transformations;
using Fiscal.Core.Validation;
using Fiscal.Infrastructure.Fakes;
using System.Text.Json;

Console.WriteLine("=== Fiscal Engine - Console Runner ===");
Console.WriteLine();

// ── Load config ───────────────────────────────────────────────────────────
string configPath = Path.Combine(
    AppContext.BaseDirectory, "configs", "fiscal-config.sample.json");

string configJson = await File.ReadAllTextAsync(configPath);

FiscalEngineConfig engineConfig = JsonSerializer.Deserialize<FiscalEngineConfig>(
    configJson,
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
    ?? throw new InvalidOperationException("Failed to deserialize fiscal config.");

Console.WriteLine($"Config loaded for device: {engineConfig.StaticValues["DeviceCode"]}");
Console.WriteLine();

var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true,
    Converters = { new DynamicRecordJsonConverter() }
};

// ── Wire up transformations ───────────────────────────────────────────────
ITransformation[] transformations =
[
    new TimestampTransformation(),
    new LineTotalTransformation()
];

var registry = new TransformationRegistry(transformations);

// ── Wire up field resolvers ───────────────────────────────────────────────
IFieldResolver[] resolvers =
[
    new PosFieldResolver(),
    new StaticFieldResolver(engineConfig),
    new GeneratedFieldResolver(),
    new InputFieldResolver(),
    new CalculatedFieldResolver(registry),
    new ModeFieldResolver(engineConfig)
];

// ── Wire up pipeline dependencies ─────────────────────────────────────────
ICheckReader checkReader = new FakeCheckReader();
ITransactionValidator validator = new B2BTransactionValidator();
IFiscalPayloadBuilder builder = new FiscalPayloadBuilder(engineConfig, resolvers);
FakeFiscalClient fiscalClient = new FakeFiscalClient();
FakePaymentClient paymentClient = new FakePaymentClient();
ISlipPrinter slipPrinter = new FakeSlipPrinter();

var processor = new FiscalTransactionProcessor(
    checkReader,
    validator,
    builder,
    fiscalClient,
    paymentClient,
    slipPrinter);

// ── Scenario 1: Happy path ────────────────────────────────────────────────
Console.WriteLine(">> Scenario 1: Successful transaction");
Console.WriteLine();

var result = await processor.ProcessAsync(new object());

if (result.IsSuccess)
{
    Console.WriteLine();
    Console.WriteLine("Pipeline completed successfully.");
    Console.WriteLine();
    Console.WriteLine("Fiscal payload sent to device:");
    Console.WriteLine(JsonSerializer.Serialize(
        result.CompletedContext?.FiscalResult, jsonOptions));
}
else
{
    Console.WriteLine($"Pipeline failed at : {result.FailedAtStage}");
    Console.WriteLine($"Reason             : {result.FailureReason}");
}

Console.WriteLine();
Console.WriteLine("────────────────────────────────────────");
Console.WriteLine();

// ── Scenario 2: Fiscal failure (gate test) ────────────────────────────────
Console.WriteLine(">> Scenario 2: Fiscal device rejects transaction");
Console.WriteLine();

fiscalClient.ShouldFail = true;
paymentClient.Reset();

var failResult = await processor.ProcessAsync(new object());

Console.WriteLine($"Pipeline stopped at : {failResult.FailedAtStage}");
Console.WriteLine($"Reason              : {failResult.FailureReason}");
Console.WriteLine($"Payment was called  : {paymentClient.WasCalled}");

Console.WriteLine();
Console.WriteLine("────────────────────────────────────────");
Console.WriteLine();

// ── Scenario 3: Credit transaction ───────────────────────────────────────
Console.WriteLine(">> Scenario 3: Credit transaction (negative amount)");
Console.WriteLine();

fiscalClient.ShouldFail = false;
paymentClient.Reset();

ICheckReader creditCheckReader = new FakeCreditCheckReader();
var creditProcessor = new FiscalTransactionProcessor(
    creditCheckReader,
    validator,
    builder,
    fiscalClient,
    paymentClient,
    slipPrinter);

var creditResult = await creditProcessor.ProcessAsync(new object());

if (creditResult.IsSuccess)
{
    Console.WriteLine($"Mode detected       : {creditResult.CompletedContext?.Mode}");
    Console.WriteLine("Credit transaction completed successfully.");
    Console.WriteLine();
    Console.WriteLine("Fiscal payload sent to device:");
    Console.WriteLine(JsonSerializer.Serialize(
        creditResult.CompletedContext?.FiscalResult, jsonOptions));
}
else
{
    Console.WriteLine($"Pipeline failed at : {creditResult.FailedAtStage}");
    Console.WriteLine($"Reason             : {creditResult.FailureReason}");
}

Console.WriteLine();
Console.WriteLine("=== Done ===");
