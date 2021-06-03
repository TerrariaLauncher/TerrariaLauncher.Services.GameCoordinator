using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator
{
    class PacketHandlerArgs: EventArgs
    {
        public CancellationToken CancellationToken { get; set; }
        public bool Handled { get; set; }
        public bool Ignored { get; set; }
        public TerrariaPacket TerrariaPacket { get; set; }
    }

    class PacketHandlers
    {
        private Dictionary<PacketOpCode, HandlerList<Interceptor, PacketHandlerArgs>.Handler> packetHandlerLookup;

        public PacketHandlers()
        {
            this.packetHandlerLookup = new Dictionary<PacketOpCode, HandlerList<Interceptor, PacketHandlerArgs>.Handler>() {
                { PacketOpCode.Connect, this.HandleConnectPacket }
            };
        }

        public async Task Handle(Interceptor sender, PacketHandlerArgs args)
        {
            if (!this.packetHandlerLookup.TryGetValue(args.TerrariaPacket.OpCode, out var handler))
            {
                return;
            }

            await handler(sender, args);
        }

        #region Connect
        public HandlerList<Interceptor, PacketHandlerArgs> ConnectHandlers = new HandlerList<Interceptor, PacketHandlerArgs>();

        private Task HandleConnectPacket(Interceptor sender, PacketHandlerArgs args)
        {
            return OnConnectPacket(sender, args);
        }

        private async Task OnConnectPacket(Interceptor sender, PacketHandlerArgs args)
        {
            await this.ConnectHandlers.Invoke(sender, args);
        }
        #endregion

        public HandlerList<Interceptor, PacketHandlerArgs> PlayerInfoHandlers = new HandlerList<Interceptor, PacketHandlerArgs>();

    }
}
