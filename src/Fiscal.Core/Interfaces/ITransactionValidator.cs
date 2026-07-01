using Fiscal.Core.Context;
using Fiscal.Core.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Fiscal.Core.Interfaces
{
    /// <summary>
    /// Validates the transaction before fiscalization runs.
    /// Currently enforces one rule: B2B transactions require a tax number
    /// via the operator input form before the pipeline can proceed.
    /// Kept as its own interface so future rules slot in without
    /// touching the orchestrator.
    /// </summary>
    public interface ITransactionValidator
    {
        FiscalValidationResult Validate(FiscalContext context);
    }
}
