using System;
using System.Collections.Generic;
using System.Text;
using Fiscal.Core.Domain;

namespace Fiscal.Core.Interfaces
{
    /// <summary>
    /// Resolves named transformations referenced in config via "Calc:".
    /// The registry holds all known ITransformation implementations,
    /// indexed by name. When the payload builder hits a "Calc:combineTotals"
    /// field, it asks the registry for that transformation and runs it.
    /// Adding a new transformation = one new class + one DI registration.
    /// No existing code touched.
    /// </summary>
    public interface ITransformationRegistry
    {
        ITransformation Resolve(string transformationName);
    }
}
