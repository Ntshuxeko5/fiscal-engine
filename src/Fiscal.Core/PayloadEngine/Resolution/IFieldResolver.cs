using Fiscal.Core.Context;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Core.PayloadEngine.Resolution
{
    /// <summary>
    /// Resolves a single source-prefixed mapping string into a concrete value.
    /// One implementation per source prefix:
    ///   PosFieldResolver      → "OpsContext.X"
    ///   StaticFieldResolver   → "Config:X"
    ///   CalculatedFieldResolver → "Calc:X"
    ///   GeneratedFieldResolver  → "Generated:X"
    ///
    /// The builder never knows how resolution works internally -
    /// it just finds the right resolver by prefix and calls Resolve().
    /// Adding a new source type = one new class implementing this interface.
    /// Nothing else changes.
    /// </summary>
    public interface IFieldResolver
    {
        /// <summary>
        /// The prefix this resolver handles, without the colon.
        /// e.g. "Config", "Calc", "Generated"
        /// For POS fields the prefix is "OpsContext" (dot-separated, not colon).
        /// </summary>
        string Prefix { get; }

        /// <summary>
        /// Resolves the mapping string into a concrete value.
        /// </summary>
        /// <param name="mapping">
        /// The full mapping string from config, e.g. "Config:SellerName"
        /// or "OpsContext.TransactionId". The resolver receives the full
        /// string and extracts what it needs.
        /// </param>
        /// <param name="context">
        /// The live pipeline context - carries the POS check data,
        /// operator input, and any already-resolved values.
        /// </param>
        /// <returns>
        /// The resolved value, or null if the field doesn't exist
        /// in the source. Callers decide how to handle null.
        /// </returns>
        object? Resolve(string mapping, FiscalContext context);
    }
}
