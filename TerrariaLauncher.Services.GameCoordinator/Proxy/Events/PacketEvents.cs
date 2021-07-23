using Microsoft.Extensions.Logging;
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

        ILoggerFactory loggerFactory;

        public PacketEvents(
            ObjectPool<PacketHandlerArgs> packetHandlerArgsPool,
            ILoggerFactory loggerFactory)
        {
            this.packetHandlerArgsPool = packetHandlerArgsPool;
            this.packetHandlerLookup = new Dictionary<PacketOpCode, Handler<Interceptor, PacketHandlerArgs>>() {
                { PacketOpCode.Connect, this.HandleConnectPacket },
                { PacketOpCode.Disconnect, this.HandleDisconnectPacket },
                { PacketOpCode.PlayerInfo, this.HandlePlayerInfoPacket },
                { PacketOpCode.NetModule, this.HandleNetModulePacket }
            };
            this.loggerFactory = loggerFactory;
            
            this.PacketReceivedHandlers = new HandlerList<Interceptor, PacketHandlerArgs>(
                this.loggerFactory.CreateLogger($"{typeof(PacketEvents).FullName}.{nameof(PacketReceivedHandlers)}")
            );
            this.ConnectHandlers = new HandlerList<Interceptor, PacketHandlerArgs>(
                this.loggerFactory.CreateLogger($"{typeof(PacketEvents).FullName}.{nameof(ConnectHandlers)}")
            );
            this.DisconnectHandlers = new HandlerList<Interceptor, PacketHandlerArgs>(
                this.loggerFactory.CreateLogger($"{typeof(PacketEvents).FullName}.{nameof(DisconnectHandlers)}")
            );
            this.PlayerInfoHandlers = new HandlerList<Interceptor, PacketHandlerArgs>(
                this.loggerFactory.CreateLogger($"{typeof(PacketEvents).FullName}.{nameof(PlayerInfoHandlers)}")
            );
            this.NetModuleHandlers = new HandlerList<Interceptor, PacketHandlerArgs>(
                this.loggerFactory.CreateLogger($"{typeof(PacketEvents).FullName}.{nameof(NetModuleHandlers)}")
            );
        }

        private async Task SpecificPacketHandle(Interceptor source, PacketHandlerArgs args)
        {
            if (!this.packetHandlerLookup.TryGetValue(args.TerrariaPacket.OpCode, out var handler))
            {
                return;
            }

            await handler(source, args);
        }

        public readonly HandlerList<Interceptor, PacketHandlerArgs> PacketReceivedHandlers;

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
        public readonly HandlerList<Interceptor, PacketHandlerArgs> ConnectHandlers;

        private Task HandleConnectPacket(Interceptor sender, PacketHandlerArgs args)
        {
            return this.ConnectHandlers.Invoke(sender, args);
        }
        #endregion

        #region Disconnect
        public readonly HandlerList<Interceptor, PacketHandlerArgs> DisconnectHandlers;

        private Task HandleDisconnectPacket(Interceptor sender, PacketHandlerArgs args)
        {
            return this.DisconnectHandlers.Invoke(sender, args);
        }
        #endregion

        #region PlayerInfo
        public readonly HandlerList<Interceptor, PacketHandlerArgs> PlayerInfoHandlers;

        private Task HandlePlayerInfoPacket(Interceptor sender, PacketHandlerArgs args)
        {
            return this.PlayerInfoHandlers.Invoke(sender, args);
        }
        #endregion

        #region NetModuleHandlers
        public readonly HandlerList<Interceptor, PacketHandlerArgs> NetModuleHandlers;

        private Task HandleNetModulePacket(Interceptor trigger, PacketHandlerArgs args)
        {
            return this.NetModuleHandlers.Invoke(trigger, args);
        }
        #endregion
    }
}
