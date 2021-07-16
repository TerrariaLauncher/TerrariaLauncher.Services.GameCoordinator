using System;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Pools;

namespace TerrariaLauncher.Services.GameCoordinator.Plugins.Helpers
{
    class TerrariaPacketPoolHelper
    {
        ObjectPool<TerrariaPacket> terrariaPacketPool;
        public TerrariaPacketPoolHelper(ObjectPool<TerrariaPacket> terrariaPacketPool)
        {
            this.terrariaPacketPool = terrariaPacketPool;
        }

        public async Task ReturnPoolIfFailed(TerrariaPacket packet, Func<TerrariaPacket, Task> func)
        {
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

        public void ReturnPoolIfFailed(TerrariaPacket packet, Action<TerrariaPacket> action)
        {
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
