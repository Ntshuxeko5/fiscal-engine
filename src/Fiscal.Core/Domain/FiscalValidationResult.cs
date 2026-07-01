using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Core.Domain
{
    /// <summary>
    /// The outcome of pre-fiscalization validation.
    /// Keeps pass/fail and the reason separate so the orchestrator
    /// can log a meaningful message on failure without knowing
    /// which rule fired.
    /// </summary>
    public class FiscalValidationResult
    {
        public bool IsValid { get; private init; }
        public string? FailureReason { get; private init; }

        public static FiscalValidationResult Success() =>
            new() { IsValid = true };

        public static FiscalValidationResult Failure(string reason) =>
            new() { IsValid = false, FailureReason = reason };
    }
}
