namespace Fiscal.Core.Context;

using Fiscal.Core.Domain;

/// <summary>
/// Carries state through every step of the fiscal transaction pipeline.
/// Starts with just the raw POS check; each step adds its output as the
/// pipeline progresses. Properties are nullable until the step that
/// populates them has actually run.
/// </summary>
public class FiscalContext
{
    public required PosCheck Check { get; init; }

    /// <summary>
    /// Operator-entered values from the B2B buyer-info form (when used),
    /// keyed by the same field names the config references via "Input:".
    /// Empty until that form is triggered - pipeline functions without it.
    /// </summary>
    public DynamicRecord OperatorInput { get; } = new();

    /// <summary>
    /// Populated by step 2 (Fiscalize). Null until that step runs.
    /// </summary>
    public DynamicRecord? FiscalResult { get; set; }

    /// <summary>
    /// Populated by step 3 (RunPayment). Null until that step runs,
    /// and only ever set if the fiscal gate passed.
    /// </summary>
    public DynamicRecord? PaymentOutcome { get; set; }

    /// <summary>
    /// True once step 4 (PrintSlip) has successfully completed.
    /// </summary>
    public bool SlipPrinted { get; set; }
}
