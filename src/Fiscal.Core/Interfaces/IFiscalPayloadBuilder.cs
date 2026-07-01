using Fiscal.Core.Context;
using Fiscal.Core.Domain;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Core.Interfaces
{
    /// <summary>
    /// Builds the fiscal payload DynamicRecord from two inputs:
    /// the live check data on the context, and the JSON config that
    /// describes what the payload should look like for this client.
    /// This is the core of the plug-and-play promise - swap the config,
    /// get a different payload shape, zero code changes.
    /// </summary>
    public interface IFiscalPayloadBuilder
    {
        DynamicRecord Build(FiscalContext context);
    }
}
