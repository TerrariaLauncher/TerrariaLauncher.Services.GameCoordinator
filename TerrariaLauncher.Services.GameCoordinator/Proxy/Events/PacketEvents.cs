using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Pools;

namespace TerrariaLauncher.Services.GameCoordinator.Proxy.Events
{
    class PacketHandlerArgs : HandlerArgs
    {
        /// <summary>
        /// If a packet is ignored, it will not be sent to the destination.
        /// </summary>
        public bool Ignored { get; set; }

        public TerrariaPacket TerrariaPacket { get; set; }
    }

    class PacketEvents
    {
        private ObjectPool<PacketHandlerArgs> packetHandlerArgsPool;
        private Dictionary<PacketOpCode, Handler<Interceptor, PacketHandlerArgs>> packetHandlerLookup;

        public PacketEvents(ObjectPool<PacketHandlerArgs> packetHandlerArgsPool)
        {
            this.packetHandlerArgsPool = packetHandlerArgsPool;

            this.packetHandlerLookup = new Dictionary<PacketOpCode, Handler<Interceptor, PacketHandlerArgs>>() {
                { PacketOpCode.Connect, this.HandleConnectPacket },
                { PacketOpCode.Disconnect, this.HandleDisconnectPacket },
                { PacketOpCode.PlayerInfo, this.HandlePlayerInfoPacket },
                { PacketOpCode.NetModule, this.HandleNetModulePacket }
            };
        }

        private async Task SpecificPacketHandle(Interceptor source, PacketHandlerArgs args)
        {
            if (!this.packetHandlerLookup.TryGetValue(args.TerrariaPacket.OpCode, out var handler))
            {
                return;
            }

            await handler(source, args);
        }

        public HandlerList<Interceptor, PacketHandlerArgs> PacketReceivedHandlers = new HandlerList<Interceptor, PacketHandlerArgs>();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="trigger">Interceptor that trigger this event.</param>
        /// <param name="terrariaPacket"></param>
        /// <param name="cancellationToken"></param>
        /// <returns>Return true if the packet should be skipped. Return false when packet should be send to destination.</returns>
        internal async Task<bool> OnPacketReceived(Interceptor trigger, TerrariaPacket terrariaPacket, CancellationToken cancellationToken = default)
        {
            var args = this.packetHandlerArgsPool.Get();
            args.CancellationToken = cancellationToken;
            args.TerrariaPacket = terrariaPacket;

            await this.PacketReceivedHandlers.Invoke(trigger, args);
            await this.SpecificPacketHandle(trigger, args);

            bool ignored = args.Ignored || args.CancellationToken.IsCancellationRequested;
            this.packetHandlerArgsPool.Return(args);
            return ignored;
        }

        #region Connect
        public HandlerList<Interceptor, PacketHandlerArgs> ConnectHandlers = new HandlerList<Interceptor, PacketHandlerArgs>();

        private Task HandleConnectPacket(Interceptor sender, PacketHandlerArgs args)
        {
            return this.ConnectHandlers.Invoke(sender, args);
        }
        #endregion

        #region Disconnect
        public HandlerList<Interceptor, PacketHandlerArgs> DisconnectHandlers = new HandlerList<Interceptor, PacketHandlerArgs>();

        private Task HandleDisconnectPacket(Interceptor sender, PacketHandlerArgs args)
        {
            return this.DisconnectHandlers.Invoke(sender, args);
        }
        #endregion

        #region PlayerInfo
        public HandlerList<Interceptor, PacketHandlerArgs> PlayerInfoHandlers = new HandlerList<Interceptor, PacketHandlerArgs>();

        private Task HandlePlayerInfoPacket(Interceptor sender, PacketHandlerArgs args)
        {
            return this.PlayerInfoHandlers.Invoke(sender, args);
        }
        #endregion

        #region NetModuleHandlers
        public HandlerList<Interceptor, PacketHandlerArgs> NetModuleHandlers = new HandlerList<Interceptor, PacketHandlerArgs>();

        private Task HandleNetModulePacket(Interceptor trigger, PacketHandlerArgs args)
        {
            return this.NetModuleHandlers.Invoke(trigger, args);
        }
        #endregion
    }
}
