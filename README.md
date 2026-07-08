# Fiscal Engine

A config-driven C#/.NET fiscal transaction engine for Symphony POS, targeting the Malawi Revenue Authority (MRA) Electronic Invoicing System (EIS) API.

> **Core promise:** to support a new client, drop in a new JSON config file. Do not touch the engine code.

---

## What it does

Every fiscal transaction runs through four steps in a fixed, enforced order:

1. **Read check** — pull transaction details from the POS
2. **Fiscalize** — send the assembled payload to the fiscal device ← **hard gate**
3. **Run payment** — only if fiscal succeeded
4. **Print slip** — compose the receipt from check + fiscal + payment data

The payload sent to the fiscal device is assembled dynamically at runtime from a JSON config file. Field values come from six sources — live POS data, static config values, named calculations, engine-generated values (timestamps, GUIDs), credit/invoice mode resolution, and operator-entered form input.

---

## Quick start

### Prerequisites

- .NET 10 SDK (`dotnet --version` → `10.0.x`)
- Git

### Run the demo

```bash
git clone https://github.com/Ntshuxeko5/fiscal-engine.git
cd fiscal-engine
dotnet run --project src/Fiscal.Console
```

You will see five scenarios run:
- ✅ Successful invoice transaction with receipt
- ❌ Fiscal device rejection (gate holds — payment never runs)
- ✅ Credit transaction (negative total, mode auto-detected)
- ✅ B2B transaction (buyer info validated before fiscalization)
- ❌ B2B with invalid tax number (validation stops pipeline)

### Run the tests

```bash
dotnet test
```

57 tests, 0 failures.

---

## Project structure

```
fiscal-engine/
├── src/
│   ├── Fiscal.Core/              # Domain, interfaces, pipeline, payload engine
│   │   ├── Context/              # FiscalContext — state carrier through pipeline
│   │   ├── Domain/               # DynamicRecord, PosCheck, TransactionMode
│   │   ├── Interfaces/           # All pipeline contracts (ICheckReader, IFiscalClient, etc.)
│   │   ├── Pipeline/             # FiscalTransactionProcessor (the orchestrator)
│   │   ├── PayloadEngine/        # Config POCOs, field resolvers, builder, slip renderer
│   │   ├── Transformations/      # ITransformation implementations (lineTotal, timestamp)
│   │   └── Validation/           # B2BTransactionValidator
│   ├── Fiscal.Infrastructure/    # Adapter implementations
│   │   ├── Fakes/                # Fake adapters for dev and testing
│   │   ├── ConsoleHost/          # Console-based operator input collector
│   │   └── Printing/             # ConfigDrivenSlipPrinter
│   └── Fiscal.Console/           # Entry point and demo runner
│       └── configs/              # Sample JSON config files
└── tests/
    └── Fiscal.Tests/             # xUnit test suite
        ├── Pipeline/             # Gate, happy path tests
        ├── PayloadEngine/        # Resolver, builder, conditional posting tests
        └── Validation/           # B2B validation tests
```

---

## How the config works

One JSON file per client describes everything:

```json
{
  "TenderMediaNumber": 4,
  "ModeValues": {
    "Invoice": "INVOICE",
    "Credit":  "CREDIT"
  },
  "StaticValues": {
    "SellerName": "Acme Restaurant Group",
    "DeviceCode": "FD-100"
  },
  "BuyerInfoForm": {
    "TriggerLabel": "B2B Sale",
    "Fields": [
      { "Key": "BuyerTaxNumber", "Label": "Tax Number", "Required": true, "Min": 9, "Max": 9 }
    ]
  },
  "Payload": {
    "TransactionId": "OpsContext.TransactionId",
    "SellerName":    "Config:SellerName",
    "FolioId":       "Generated:guid",
    "LineTotal":     "Calc:lineTotal",
    "FolioType":     "Mode:documentType",
    "BuyerTaxNo":    "Input:BuyerTaxNumber"
  }
}
```

### Field source prefixes

| Prefix | Example | Source |
|---|---|---|
| `OpsContext.X` | `OpsContext.TransactionId` | Live POS transaction data |
| `Config:X` | `Config:SellerName` | Static value in config file |
| `Calc:X` | `Calc:lineTotal` | Named transformation |
| `Generated:X` | `Generated:guid` | Created by engine at runtime |
| `Mode:X` | `Mode:documentType` | Credit/invoice mode value |
| `Input:X` | `Input:BuyerTaxNumber` | Operator-entered form data |

### Conditional postings

Fields can be conditionally included:

```json
"ServiceCharge": {
  "Value":     "OpsContext.ServiceCharge",
  "IncludeIf": "OpsContext.ServiceCharge > 0"
}
```

If the condition is false, the field is omitted entirely from the payload.

---

## Adding a new client

1. Create `src/Fiscal.Console/configs/{client-name}.json`
2. Set `StaticValues` (seller name, tax number, device code, currency)
3. Set `ModeValues` (how this device expects INVOICE/CREDIT to be expressed)
4. Define `Payload` — map each required field to its source prefix
5. Define `SlipConfig` — describe the receipt layout section by section
6. Define `BuyerInfoForm` if B2B is needed
7. Load the config at startup — no code changes required

---

## Adding a new transformation

1. Create a class in `src/Fiscal.Core/Transformations/`:

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

2. Register it alongside the others in `Program.cs`:

```csharp
ITransformation[] transformations =
[
    new TimestampTransformation(),
    new LineTotalTransformation(),
    new MyTransformation()
];
```

3. Reference it in config: `"MyField": "Calc:myTransform"`

No existing code is modified.

---

## CI / CD

Every pull request to `main` runs three checks:

| Check | Command | What it enforces |
|---|---|---|
| Format | `dotnet format --verify-no-changes` | Code style per `.editorconfig` |
| Build | `dotnet build --configuration Release` | Nullable types, warnings as errors |
| Tests | `dotnet test` | All 57 tests pass |

Direct pushes to `main` are blocked. All changes go through a PR.

---

## Current status

| Component | Status |
|---|---|
| Four-stage pipeline + fiscal gate | ✅ Complete |
| All six field source resolvers | ✅ Complete |
| Conditional postings (IncludeIf) | ✅ Complete |
| Credit/invoice mode detection | ✅ Complete |
| B2B buyer-info form + validation | ✅ Complete |
| Operator input collection | ✅ Complete |
| Config-driven slip/receipt | ✅ Complete |
| 57 automated tests | ✅ Passing |
| CI pipeline + branch protection | ✅ Active |
| FiscalEdgeClient (MRA EIS API) | 🔲 Next |
| SymphonyCheckReader | 🔲 Next |
| SymphonyPaymentClient | 🔲 Next |
| WPF operator input dialog | 🔲 Next |

---

## Tech stack

- **Language:** C# 13
- **Runtime:** .NET 10
- **Tests:** xUnit 3
- **Serialization:** System.Text.Json
- **CI:** GitHub Actions
- **Target POS:** Symphony by Oracle (Adapt IT)
- **Target fiscal API:** MRA EIS API (Malawi Revenue Authority)

---

## Documentation

Full technical documentation: [`FISCAL_ENGINE.md`](./FISCAL_ENGINE.md)
