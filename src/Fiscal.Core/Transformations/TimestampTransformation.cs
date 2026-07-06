using Fiscal.Core.Context;
using Fiscal.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Core.Transformations
{
    /// <summary>
    /// Named "timestamp" - returns the current UTC time formatted
    /// for fiscal device payloads. Referenced in config as "Calc:timestamp"
    /// when the Generated: prefix isn't sufficient (e.g. when you need
    /// the timestamp in a specific format per client, passed as an arg).
    /// </summary>
    public class TimestampTransformation : ITransformation
    {
        public string Name => "timestamp";

        public object? Apply(IReadOnlyList<string> args, FiscalContext context)
        {
            // If a format arg is provided, use it. Otherwise ISO 8601.
            string format = args.Count > 0 ? args[0] : "o";
            return DateTime.UtcNow.ToString(format);
        }
    }
}
