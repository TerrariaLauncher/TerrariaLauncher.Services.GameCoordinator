using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator.Plugins
{
    class Register : Plugin
    {
        public override Task Load(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public override Task Unload(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
