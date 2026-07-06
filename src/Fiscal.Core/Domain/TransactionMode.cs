using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Core.Domain
{
    /// <summary>
    /// Represents the mode of a fiscal transaction.
    /// Determined by the engine from the check amount -
    /// this rule is universal, not per-client.
    /// What changes per client is how each mode value gets
    /// written into the payload (e.g. "CREDIT" vs "C"),
    /// which is handled by ModeFieldResolver + config.
    /// </summary>
    public enum TransactionMode
    {
        Invoice,
        Credit
    }
}
