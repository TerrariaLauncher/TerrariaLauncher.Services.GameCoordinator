using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator.Plugins
{
    interface IPlugin
    {
        Task Load(CancellationToken cancellationToken = default);
        Task Unload(CancellationToken cancellationToken = default);
    }
}
