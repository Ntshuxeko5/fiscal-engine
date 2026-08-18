using Fiscal.Core.Domain;
using Fiscal.Core.Interfaces;
using Fiscal.Core.PayloadEngine;
using Fiscal.Core.PayloadEngine.Config;
using Fiscal.Core.PayloadEngine.Resolution;
using Fiscal.Core.Pipeline;
using Fiscal.Core.Transformations;
using Fiscal.Core.Validation;
using Fiscal.Infrastructure.Fakes;
using Fiscal.Infrastructure.FiscalEdge;
using Fiscal.Infrastructure.MraEis;
using Fiscal.Infrastructure.Printing;
using System.Text.Json;

Console.WriteLine("=== Fiscal Engine - Console Runner ===");
Console.WriteLine();

// ── Determine mode FIRST - everything else depends on this ───────────────
bool useRealFiscalDevice = args.Contains("--real");

string configFileName = useRealFiscalDevice
    ? "fiscal-config.malawi.json"
    : "fiscal-config.sample.json";

// ── Load config ───────────────────────────────────────────────────────────
string configPath = Path.Combine(
    AppContext.BaseDirectory, "configs", configFileName);

string configJson = await File.ReadAllTextAsync(configPath);

FiscalEngineConfig engineConfig = JsonSerializer.Deserialize<FiscalEngineConfig>(
    configJson,
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
    ?? throw new InvalidOperationException("Failed to deserialize fiscal config.");

Console.WriteLine($"Config loaded: {configFileName}");
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

// ── Wire up shared pipeline dependencies ──────────────────────────────────
ICheckReader checkReader = new FakeCheckReader();
ITransactionValidator validator = new B2BTransactionValidator();
IFiscalPayloadBuilder builder = new FiscalPayloadBuilder(engineConfig, resolvers);
FakeFiscalClient fakeFiscalClient = new FakeFiscalClient();
FakePaymentClient paymentClient = new FakePaymentClient();
ISlipPrinter slipPrinter = engineConfig.SlipConfig is not null
    ? new ConfigDrivenSlipPrinter(engineConfig.SlipConfig)
    : new FakeSlipPrinter();
IOperatorInputCollector inputCollector = new FakeOperatorInputCollector();

// ── Resolve the ACTUAL fiscal client to use, before building any processor ─
IFiscalClient fiscalClientToUse;

if (useRealFiscalDevice)
{
    string credsPath = Path.Combine(
        AppContext.BaseDirectory, "configs", "local", "fiscal-edge-credentials.local.json");

    if (!File.Exists(credsPath))
    {
        Console.WriteLine(
            $"ERROR: {credsPath} not found. Create it with your real " +
            $"Fiscal Edge credentials before running with --real.");
        return;
    }

    string credsJson = await File.ReadAllTextAsync(credsPath);
    var feConfig = JsonSerializer.Deserialize<FiscalEdgeConfig>(
        credsJson,
        new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
        ?? throw new InvalidOperationException("Failed to load Fiscal Edge credentials.");

    var httpClient = new HttpClient
    {
        Timeout = TimeSpan.FromSeconds(feConfig.TimeoutSeconds)
    };

    fiscalClientToUse = new FiscalEdgeClient(httpClient, feConfig);

    Console.WriteLine("Using REAL Fiscal Edge client (dev sandbox).");
}
else
{
    fiscalClientToUse = fakeFiscalClient;
    Console.WriteLine("Using FAKE fiscal client (demo mode).");
}

Console.WriteLine();

// ── NOW build the processor, using whichever client was actually resolved ──
var processor = new FiscalTransactionProcessor(
    checkReader,
    inputCollector,
    validator,
    builder,
    fiscalClientToUse,   // ← the correct one, decided above
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

// ── Remaining demo scenarios ALWAYS use the fake client ───────────────────
// These are demo/regression scenarios, not real API tests - only
// scenario 1 above hits the real MRA sandbox when --real is passed.

Console.WriteLine(">> Scenario 2: Fiscal device rejects transaction");
Console.WriteLine();

fakeFiscalClient.ShouldFail = true;
paymentClient.Reset();

var fakeProcessor = new FiscalTransactionProcessor(
    checkReader, inputCollector, validator, builder,
    fakeFiscalClient, paymentClient, slipPrinter);

var failResult = await fakeProcessor.ProcessAsync(new object());

Console.WriteLine($"Pipeline stopped at : {failResult.FailedAtStage}");
Console.WriteLine($"Reason              : {failResult.FailureReason}");
Console.WriteLine($"Payment was called  : {paymentClient.WasCalled}");

Console.WriteLine();
Console.WriteLine("────────────────────────────────────────");
Console.WriteLine();

// ── Scenario 3: Credit transaction ───────────────────────────────────────
Console.WriteLine(">> Scenario 3: Credit transaction (negative amount)");
Console.WriteLine();

fakeFiscalClient.ShouldFail = false;
paymentClient.Reset();

ICheckReader creditCheckReader = new FakeCreditCheckReader();
IOperatorInputCollector creditInputCollector = new FakeOperatorInputCollector(
    fiscalNo: "TXN-0001",
    refundReason: "Customer requested refund");
var creditProcessor = new FiscalTransactionProcessor(
    creditCheckReader,
    creditInputCollector,
    validator,
    builder,
    fakeFiscalClient,
    paymentClient,
    slipPrinter);

var creditResult = await creditProcessor.ProcessAsync(new object());

if (creditResult.IsSuccess)
{
    Console.WriteLine($"Mode detected       : {creditResult.CompletedContext?.Mode}");
    Console.WriteLine("Credit transaction completed successfully.");
}
else
{
    Console.WriteLine($"Pipeline failed at : {creditResult.FailedAtStage}");
    Console.WriteLine($"Reason             : {creditResult.FailureReason}");
}

Console.WriteLine();
Console.WriteLine("────────────────────────────────────────");
Console.WriteLine();

// ── Scenario 4: B2B transaction ───────────────────────────────────────────
Console.WriteLine(">> Scenario 4: B2B transaction");
Console.WriteLine();

fakeFiscalClient.ShouldFail = false;
paymentClient.Reset();

ICheckReader b2bCheckReader = new FakeB2BCheckReader();

IOperatorInputCollector b2bInputCollector = new FakeOperatorInputCollector(
    buyerValues: new Dictionary<string, string>
    {
        ["BuyerTaxNumber"] = "123456789",
        ["BuyerName"] = "Acme Corp",
        ["BuyerAddress"] = "123 Main St"
    });

var b2bValidator = new B2BTransactionValidator(engineConfig.BuyerInfoForm);

var b2bProcessor = new FiscalTransactionProcessor(
    b2bCheckReader,
    b2bInputCollector,
    b2bValidator,
    builder,
    fakeFiscalClient,
    paymentClient,
    slipPrinter);

var b2bResult = await b2bProcessor.ProcessAsync(new object());

Console.WriteLine(b2bResult.IsSuccess
    ? "B2B transaction completed successfully."
    : $"Pipeline failed at : {b2bResult.FailedAtStage}, Reason: {b2bResult.FailureReason}");

Console.WriteLine();
Console.WriteLine("────────────────────────────────────────");
Console.WriteLine();

// ── Scenario 5: B2B with invalid tax number ───────────────────────────────
Console.WriteLine(">> Scenario 5: B2B with invalid tax number (too short)");
Console.WriteLine();

paymentClient.Reset();

IOperatorInputCollector badB2BCollector = new FakeOperatorInputCollector(
    buyerValues: new Dictionary<string, string>
    {
        ["BuyerTaxNumber"] = "12345",
        ["BuyerName"] = "Acme Corp"
    });

var badB2BProcessor = new FiscalTransactionProcessor(
    b2bCheckReader,
    badB2BCollector,
    b2bValidator,
    builder,
    fakeFiscalClient,
    paymentClient,
    slipPrinter);

var badB2BResult = await badB2BProcessor.ProcessAsync(new object());

Console.WriteLine($"Pipeline stopped at : {badB2BResult.FailedAtStage}");
Console.WriteLine($"Reason              : {badB2BResult.FailureReason}");
Console.WriteLine($"Payment was called  : {paymentClient.WasCalled}");

Console.WriteLine();
Console.WriteLine("=== Done ===");
