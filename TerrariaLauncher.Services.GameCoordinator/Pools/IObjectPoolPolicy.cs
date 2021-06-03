using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator
{
    interface IObjectPoolPolicy<T>
    {
        T Create();
        bool Return(T instance);
    }
}
