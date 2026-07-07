using Fiscal.Core.Context;
using Fiscal.Core.Interfaces;
using Fiscal.Core.PayloadEngine;
using Fiscal.Core.PayloadEngine.Config;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fiscal.Infrastructure.Printing
{
    /// <summary>
    /// Real ISlipPrinter implementation driven by SlipConfig.
    /// Renders the receipt using SlipRenderer and writes to console
    /// (for demo/dev). In production, replace the output target
    /// with the real printer SDK call - the renderer output
    /// (list of strings) is already the right shape for most
    /// receipt printer APIs.
    /// </summary>
    public class ConfigDrivenSlipPrinter : ISlipPrinter
    {
        private readonly SlipRenderer _renderer;

        public ConfigDrivenSlipPrinter(SlipConfig config)
        {
            _renderer = new SlipRenderer(config);
        }

        public Task PrintAsync(FiscalContext context)
        {
            IReadOnlyList<string> lines = _renderer.Render(context);

            foreach (string line in lines)
            {
                Console.WriteLine(line);
            }

            return Task.CompletedTask;
        }
    }
}
