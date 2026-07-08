# Fiscal Engine — Technical Documentation

**Version:** 1.0  
**Project:** fiscal-engine  
**Stack:** C# / .NET 10  
**Status:** Engine complete. Real adapters (Symphony POS, MRA EIS API) pending.

---

## Overview

The Fiscal Engine is a config-driven C#/.NET system that handles fiscal transactions at a point of sale. It eliminates the need to write custom code for each new client or fiscal device. Instead, one JSON config file per client describes the payload shape, field mappings, and receipt layout — the engine reads it at startup and handles everything else.

**The core promise:** to support a new client, drop in a new config file. Do not touch the engine code.

---

## Architecture

The solution contains four projects:

```
FiscalEngine.sln
├── src/
│   ├── Fiscal.Core/           # Domain models, interfaces, pipeline, payload engine
│   ├── Fiscal.Infrastructure/ # Adapters (fake + real), console host
│   └── Fiscal.Console/        # Entry point / demo runner
└── tests/
    └── Fiscal.Tests/          # xUnit test suite (57 tests)
```

**Dependency rule:** `Fiscal.Core` never depends on `Fiscal.Infrastructure`. Core defines interfaces; Infrastructure implements them. This means adapters are swappable without touching the engine.

---

## The Pipeline

Every transaction runs through the same steps in a fixed, unbypassable order:

```
1. Read check       → ICheckReader reads transaction data from the POS
2. Detect mode      → Engine checks if total < 0 (credit) or >= 0 (invoice)
3. Collect input    → IOperatorInputCollector prompts for fiscalNo (credit) or buyer info (B2B)
4. Validate         → ITransactionValidator enforces B2B buyer info rules
5. Build payload    → IFiscalPayloadBuilder assembles the fiscal JSON from config
6. Fiscalize        → IFiscalClient sends payload to fiscal device ← THE GATE
7. Run payment      → IPaymentClient executes POS payment (only if fiscal succeeded)
8. Print slip       → ISlipPrinter renders and prints the receipt
```

**The fiscal gate** is the most critical rule in the system. If step 6 fails, the method returns immediately. Steps 7 and 8 are physically unreachable after a fiscal failure — not by convention, by code structure. This is proven by automated tests.

---

## Value Sources

Every field in the fiscal payload is described by a source-prefixed string in the JSON config. The engine resolves each one at runtime:

| Prefix | Example | Source |
|---|---|---|
| `OpsContext.X` | `OpsContext.TransactionId` | Live POS check data |
| `Config:X` | `Config:SellerName` | Static value in config file |
| `Calc:X` | `Calc:lineTotal` | Named transformation (registered in DI) |
| `Generated:X` | `Generated:guid` | Engine creates at runtime (GUID, timestamp) |
| `Mode:X` | `Mode:documentType` | Credit/invoice mode, output value from config |
| `Input:X` | `Input:BuyerTaxNumber` | Operator-entered form data |

---

## JSON Config Schema

One file per client. File location: `configs/{client}.json`

```json
{
  "TenderMediaNumber": 4,

  "ModeValues": {
    "Invoice": "INVOICE",
    "Credit":  "CREDIT"
  },

  "StaticValues": {
    "SellerName":      "Acme Restaurant Group",
    "SellerTaxNumber": "4123456789",
    "CurrencyCode":    "ZAR",
    "DeviceCode":      "FD-100"
  },

  "BuyerInfoForm": {
    "TriggerLabel": "B2B Sale",
    "Fields": [
      { "Key": "BuyerTaxNumber", "Label": "Tax Number",    "Required": true, "Min": 9, "Max": 9 },
      { "Key": "BuyerName",      "Label": "Company Name",  "Required": true },
      { "Key": "BuyerAddress",   "Label": "Address",       "Required": false }
    ]
  },

  "SlipConfig": {
    "Width": 40,
    "Sections": [
      { "Type": "line" },
      { "Type": "text",  "Value": "FISCAL RECEIPT", "Align": "center" },
      { "Type": "line" },
      { "Type": "field", "Label": "Transaction", "FieldValue": "OpsContext.TransactionId" },
      { "Type": "field", "Label": "Fiscal No.",  "FieldValue": "FiscalResult.FiscalReceiptNumber" },
      { "Type": "field", "Label": "Tender",      "FieldValue": "PaymentOutcome.TenderMediaNumber",
        "IncludeIf": "OpsContext.TotalDue > 0" },
      { "Type": "line" },
      { "Type": "items", "LabelField": "OpsContext.Name", "ValueField": "OpsContext.Amount" },
      { "Type": "line" },
      { "Type": "field", "Label": "Total", "FieldValue": "OpsContext.TotalDue" },
      { "Type": "line" }
    ]
  },

  "Payload": {
    "DocumentInfo": {
      "FolioType":      "Mode:documentType",
      "DeviceCode":     "Config:DeviceCode",
      "FiscalId":       "Generated:guid",
      "OriginalFiscalNo": "Input:fiscalNo"
    },
    "SellerInfo": {
      "Name":       "Config:SellerName",
      "TaxNumber":  "Config:SellerTaxNumber",
      "Currency":   "Config:CurrencyCode"
    },
    "FolioInfo": {
      "TransactionId": "OpsContext.TransactionId",
      "CashierId":     "OpsContext.CashierId",
      "Timestamp":     "Generated:timestamp",
      "TotalDue":      "OpsContext.TotalDue",
      "LineTotal":     "Calc:lineTotal",
      "ServiceCharge": {
        "Value":     "OpsContext.ServiceCharge",
        "IncludeIf": "OpsContext.ServiceCharge > 0"
      }
    },
    "Postings": [
      {
        "Sku":      "OpsContext.Sku",
        "Name":     "OpsContext.Name",
        "Quantity": "OpsContext.Quantity",
        "Amount":   "OpsContext.Amount"
      }
    ]
  }
}
```

---

## Conditional Postings

Fields can be conditionally included using `IncludeIf`. If the condition evaluates to false, the field is omitted from the payload entirely — not set to null, completely absent.

```json
"ServiceCharge": {
  "Value":     "OpsContext.ServiceCharge",
  "IncludeIf": "OpsContext.ServiceCharge > 0"
}
```

**Supported operators:** `>`, `<`, `>=`, `<=`, `==`, `!=`  
**Supported right-hand values:** numbers, `null`, `true`, `false`, string literals  
**Supported left-hand sources:** `OpsContext.X`, `Input:X`

---

## Credit Transactions

If `TotalDue` is negative, the engine detects a credit transaction automatically. This rule is universal — it lives in the engine, not config. What changes per client is the string written into the payload (`"CREDIT"`, `"C"`, etc.), defined in `ModeValues`.

Credit transactions require the operator to enter the original fiscal number being credited. This is collected by `IOperatorInputCollector` before the pipeline runs and referenced in the payload via `Input:fiscalNo`.

---

## B2B Transactions

When `IsB2B` is true on the POS check, the engine:

1. Triggers `IOperatorInputCollector` to collect buyer information
2. Validates collected values against `BuyerInfoForm` rules (`Required`, `Min`, `Max`)
3. Fails at validation (before fiscalization) if any required field is missing or invalid
4. Makes collected values available in the payload via `Input:BuyerTaxNumber`, `Input:BuyerName`, etc.

Field validation supports three constraints:
- `Required` — field must not be empty
- `Min` — minimum character length
- `Max` — maximum character length
- `Min == Max` — exact length required (e.g. `Min: 9, Max: 9` = exactly 9 characters)

---

## Adding a New Client

1. Create `configs/{new-client}.json`
2. Set `StaticValues` for this client's seller info, device code, currency, etc.
3. Set `ModeValues` for how this fiscal device expects Invoice/Credit to be expressed
4. Define the `Payload` structure matching the fiscal device's expected JSON shape, using source prefixes for each field
5. Define `SlipConfig` sections for the receipt layout
6. Define `BuyerInfoForm` if B2B transactions are supported
7. Load the config file at startup — no code changes required

---

## Adding a New Transformation

Transformations are named calculations referenced in config via `Calc:X`.

1. Create a class implementing `ITransformation` in `Fiscal.Core/Transformations/`:

```csharp
public class MyTransformation : ITransformation
{
    public string Name => "myTransform";

    public object? Apply(IReadOnlyList<string> args, FiscalContext context)
    {
        // compute and return value
    }
}
```

2. Register it in DI alongside existing transformations:

```csharp
ITransformation[] transformations =
[
    new TimestampTransformation(),
    new LineTotalTransformation(),
    new MyTransformation()          // ← add here
];
```

3. Reference it in any client config: `"Calc:myTransform"`

No existing code is modified.

---

## Key Interfaces

| Interface | Responsibility |
|---|---|
| `ICheckReader` | Reads transaction data from the POS and returns a populated `FiscalContext` |
| `IOperatorInputCollector` | Prompts operator for credit `fiscalNo` or B2B buyer info |
| `ITransactionValidator` | Validates B2B buyer info before fiscalization |
| `IFiscalPayloadBuilder` | Builds the fiscal payload from config + context |
| `IFiscalClient` | Sends payload to fiscal device, returns response |
| `IPaymentClient` | Executes payment command on POS terminal |
| `ISlipPrinter` | Renders and prints the client receipt |
| `ITransformation` | A single named calculation used in payload field resolution |
| `ITransformationRegistry` | Looks up `ITransformation` by name |
| `IFieldResolver` | Resolves a single source-prefix type (one per prefix) |

---

## FiscalContext

The single object that flows through the entire pipeline. Each step reads what it needs and writes what it produces:

```csharp
public class FiscalContext
{
    public required PosCheck Check { get; init; }      // set by ICheckReader
    public DynamicRecord OperatorInput { get; }        // populated by IOperatorInputCollector
    public TransactionMode? Mode { get; set; }         // set by orchestrator after ReadCheck
    public DynamicRecord? FiscalResult { get; set; }   // set after IFiscalClient succeeds
    public DynamicRecord? PaymentOutcome { get; set; } // set after IPaymentClient runs
    public bool SlipPrinted { get; set; }              // set after ISlipPrinter runs
}
```

---

## DynamicRecord

The schema-agnostic data container used throughout the engine. All client-facing data (POS check, fiscal response, payment outcome) flows through `DynamicRecord` rather than fixed C# classes, so the engine never needs to be changed when a client's field names or structure differ.

```csharp
var record = new DynamicRecord();
record.Set("TransactionId", "TXN-001");
string? txnId = record.Get<string>("TransactionId");
```

---

## Test Coverage

57 tests covering:

- Fiscal gate: payment and printing never run after fiscal failure
- Happy path: all four steps run in order, context populated correctly
- B2B validation: missing/invalid buyer info stops pipeline at validation stage
- B2B form validation: `Required`, `Min`, `Max`, exact length (`Min == Max`)
- Conditional postings: `IncludeIf` includes/excludes fields correctly
- All six field resolvers: `OpsContext`, `Config`, `Generated`, `Input`, `Calc`, `Mode`
- Payload builder: static, POS, generated, calculated, mode, and input fields
- Credit mode detection and mode value resolution

Run with: `dotnet test`

---

## CI Pipeline

Every pull request to `main` must pass three jobs before merging:

1. **Format check** — `dotnet format --verify-no-changes`
2. **Build** — warnings treated as errors, nullable reference types enforced
3. **Unit tests** — all 57 tests must pass

Direct pushes to `main` are blocked by branch protection.

---

## What's Next

| Item | Description |
|---|---|
| `FiscalEdgeClient` | Real `IFiscalClient` posting to MRA EIS API |
| `SymphonyCheckReader` | Real `ICheckReader` reading from Symphony POS object |
| `SymphonyPaymentClient` | Real `IPaymentClient` executing Symphony payment command |
| `ReceiptPrinterSlipPrinter` | Real `ISlipPrinter` sending lines to receipt printer hardware |
| WPF `IOperatorInputCollector` | Generic config-driven dialog for credit and B2B input |
| Startup config validation | Fail fast if config is malformed before first transaction |
| `IncludeIf` on array item fields | Conditional fields inside line item templates |

---

## Repository Structure

```
fiscal-engine/
├── .github/workflows/ci.yml          # CI pipeline
├── .editorconfig                      # Formatting rules
├── Directory.Build.props              # Nullable + warnings-as-errors for all projects
├── global.json                        # SDK version pin
├── src/
│   ├── Fiscal.Core/
│   │   ├── Context/FiscalContext.cs
│   │   ├── Domain/
│   │   │   ├── PosCheck.cs            # DynamicRecord + PosCheck
│   │   │   ├── FiscalValidationResult.cs
│   │   │   └── TransactionMode.cs
│   │   ├── Interfaces/                # All pipeline contracts
│   │   ├── Pipeline/
│   │   │   ├── FiscalTransactionProcessor.cs
│   │   │   └── FiscalPipelineResult.cs
│   │   ├── PayloadEngine/
│   │   │   ├── Config/                # FiscalEngineConfig, SlipConfig, BuyerInfoFormConfig
│   │   │   ├── Resolution/            # One IFieldResolver per source prefix
│   │   │   ├── FiscalPayloadBuilder.cs
│   │   │   ├── TransformationRegistry.cs
│   │   │   ├── ConditionEvaluator.cs
│   │   │   └── SlipRenderer.cs
│   │   ├── Transformations/           # ITransformation implementations
│   │   └── Validation/
│   │       └── B2BTransactionValidator.cs
│   ├── Fiscal.Infrastructure/
│   │   ├── Fakes/                     # Fake adapters for dev/test
│   │   ├── ConsoleHost/               # Console-based operator input
│   │   └── Printing/
│   │       └── ConfigDrivenSlipPrinter.cs
│   └── Fiscal.Console/
│       ├── Program.cs                 # Wires up all dependencies, runs scenarios
│       └── configs/
│           └── fiscal-config.sample.json
└── tests/
    └── Fiscal.Tests/
        ├── Pipeline/                  # Gate + happy path tests
        ├── PayloadEngine/             # Resolver + builder + conditional tests
        └── Validation/                # B2B validation tests
```
