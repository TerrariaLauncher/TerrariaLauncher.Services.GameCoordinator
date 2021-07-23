using System;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Pools;
using TerrariaLauncher.Services.GameCoordinator.Proxy;

namespace TerrariaLauncher.Services.GameCoordinator.Plugins.Helpers
{
    class TerrariaPacketPoolHelper
    {
        ObjectPool<TerrariaPacket> terrariaPacketPool;
        public TerrariaPacketPoolHelper(ObjectPool<TerrariaPacket> terrariaPacketPool)
        {
            this.terrariaPacketPool = terrariaPacketPool;
        }

        public async Task ReturnPoolIfFailed(Func<TerrariaPacket, Task> func)
        {
            var packet = this.terrariaPacketPool.Get();
            try
            {
                await func(packet);
            }
            catch
            {
                terrariaPacketPool.Return(packet);
                throw;
            }
        }

        public void ReturnPoolIfFailed(Action<TerrariaPacket> action)
        {
            var packet = this.terrariaPacketPool.Get();
            try
            {
                action(packet);
            }
            catch
            {
                terrariaPacketPool.Return(packet);
                throw;
            }
        }
    }
}
