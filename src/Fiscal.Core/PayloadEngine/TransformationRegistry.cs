using Fiscal.Core.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Core.PayloadEngine
{
    /// <summary>
    /// Holds all registered ITransformation implementations, indexed
    /// by their Name property. When the payload engine hits a "Calc:X"
    /// field in config, it asks this registry for transformation X
    /// and runs it.
    ///
    /// Registrations happen at startup (in Program.cs or DI setup).
    /// The registry itself never changes when new transformations are added -
    /// you just register a new ITransformation and it becomes available
    /// to any config that references it by name.
    ///
    /// This is the Strategy pattern + dictionary lookup in practice.
    /// </summary>
    public class TransformationRegistry : ITransformationRegistry
    {
        private readonly Dictionary<string, ITransformation> _transformations;

        /// <summary>
        /// Takes all registered ITransformation implementations via DI.
        /// Each one self-identifies via its Name property - no manual
        /// mapping required, no switch statement to maintain.
        /// </summary>
        public TransformationRegistry(IEnumerable<ITransformation> transformations)
        {
            _transformations = transformations.ToDictionary(
                t => t.Name,
                t => t,
                StringComparer.OrdinalIgnoreCase);
        }

        public ITransformation Resolve(string transformationName)
        {
            if (_transformations.TryGetValue(transformationName, out var transformation))
            {
                return transformation;
            }

            throw new InvalidOperationException(
                $"No transformation registered with name '{transformationName}'. " +
                $"Available: {string.Join(", ", _transformations.Keys)}");
        }
    }
}
