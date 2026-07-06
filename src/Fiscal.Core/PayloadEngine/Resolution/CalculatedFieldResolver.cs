using Fiscal.Core.Context;
using Fiscal.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Core.PayloadEngine.Resolution
{
    /// <summary>
    /// Resolves "Calc:X" mappings by looking up named transformation X
    /// in the ITransformationRegistry and running it against the context.
    ///
    /// Example config entry:
    ///   "ComboTotal": "Calc:combineComboTotals"
    ///
    /// The registry holds all registered ITransformation implementations
    /// indexed by their Name property. This resolver just bridges the
    /// config string to the right transformation instance.
    ///
    /// Adding a new calculation = one new ITransformation class.
    /// This resolver never changes.
    /// </summary>
    public class CalculatedFieldResolver : IFieldResolver
    {
        private readonly ITransformationRegistry _registry;

        public CalculatedFieldResolver(ITransformationRegistry registry)
        {
            _registry = registry;
        }

        public string Prefix => "Calc";

        public object? Resolve(string mapping, FiscalContext context)
        {
            // "Calc:combineComboTotals" → "combineComboTotals"
            int colonIndex = mapping.IndexOf(':');
            if (colonIndex < 0 || colonIndex == mapping.Length - 1)
            {
                return null;
            }

            string transformationName = mapping[(colonIndex + 1)..];
            ITransformation transformation = _registry.Resolve(transformationName);

            // No args for now - args support can be added to
            // PayloadFieldConfig later when a transformation needs them
            return transformation.Apply(Array.Empty<string>(), context);
        }
    }
}
