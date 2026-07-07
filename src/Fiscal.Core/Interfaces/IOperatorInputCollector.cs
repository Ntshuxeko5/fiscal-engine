using Fiscal.Core.Context;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Core.Interfaces
{
    /// <summary>
    /// Collects operator-entered values before fiscalization runs.
    /// Called by the orchestrator after mode detection, before validation.
    ///
    /// Two use cases share this interface:
    ///   1. Credit transactions — operator enters the original fiscal number
    ///   2. B2B transactions — operator enters buyer tax number, name, address
    ///
    /// The implementation decides what to collect based on context state
    /// (Mode, Check data, etc). In a WPF host, this shows a dialog.
    /// In a console host, it prompts stdin. In tests, a fake returns
    /// preconfigured values.
    ///
    /// Collected values are written to context.OperatorInput, making them
    /// available to the payload engine via "Input:fieldName" mappings.
    /// </summary>
    public interface IOperatorInputCollector
    {
        Task CollectAsync(FiscalContext context);
    }
}
