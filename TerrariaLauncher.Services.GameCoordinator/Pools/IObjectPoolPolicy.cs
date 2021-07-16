using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator.Pools
{
    interface IObjectPoolPolicy<T>
    {
        int RetainedSize { get; }
        T Create();
        bool Return(T instance);
    }
}
