using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator
{
    class TerrariaPacketPoolPolicy : IObjectPoolPolicy<TerrariaPacket>
    {
        private readonly ArrayPool<byte> arrayPool;
        public TerrariaPacketPoolPolicy(ArrayPool<byte> arrayPool)
        {
            this.arrayPool = arrayPool;
        }

        public TerrariaPacket Create()
        {
            return new TerrariaPacket(this.arrayPool);
        }

        public bool Return(TerrariaPacket instance)
        {
            // Set length to 0 meaning return internal buffer to array pool.
            instance.Length = 0;
            instance.Origin = PacketOrigin.Client;
            return true;
        }
    }
}
