using Fiscal.Core.Context;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Core.Pipeline
{
    /// <summary>
    /// The outcome of a full pipeline run. Uses named static constructors
    /// instead of a bool + nullable fields so the caller always knows
    /// exactly which stage the pipeline reached and why it stopped.
    /// </summary>
    public class FiscalPipelineResult
    {
        public bool IsSuccess { get; private init; }
        public PipelineFailureStage? FailedAtStage { get; private init; }
        public string? FailureReason { get; private init; }
        public FiscalContext? CompletedContext { get; private init; }

        public static FiscalPipelineResult Succeeded(FiscalContext context) =>
            new()
            {
                IsSuccess = true,
                CompletedContext = context
            };

        public static FiscalPipelineResult ValidationFailed(string reason) =>
            new()
            {
                IsSuccess = false,
                FailedAtStage = PipelineFailureStage.Validation,
                FailureReason = reason
            };

        public static FiscalPipelineResult FiscalFailed(string reason) =>
            new()
            {
                IsSuccess = false,
                FailedAtStage = PipelineFailureStage.Fiscalization,
                FailureReason = reason
            };
    }

    /// <summary>
    /// Which stage of the pipeline caused the failure.
    /// Useful for logging, alerting, and test assertions.
    /// </summary>
    public enum PipelineFailureStage
    {
        Validation,
        Fiscalization
    }
}
