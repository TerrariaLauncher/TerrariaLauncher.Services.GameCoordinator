using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator.Plugins
{
    public abstract class Plugin : IPlugin
    {
        public abstract Task Load(CancellationToken cancellationToken = default);

        public abstract Task Unload(CancellationToken cancellationToken = default);
    }
}
