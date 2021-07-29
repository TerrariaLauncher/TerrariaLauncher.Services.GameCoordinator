using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
                /*1  */{ PacketOpCode.Connect, this.HandleConnectPacket },
                /*2  */{ PacketOpCode.Disconnect, this.HandleDisconnectPacket },
                /*3  */{ PacketOpCode.SetPlayerSlot, this.HandleSetPlayerSlotPacket },
                /*4  */{ PacketOpCode.SyncPlayer, this.HandlePlayerInfoPacket },
                /*7  */{ PacketOpCode.ResponseWorldInfo, this.HandleResponseWorldInfoPacket },
                /*8  */{ PacketOpCode.RequestTileData, this.HandleRequestTileDataPacket },
                /*12 */{ PacketOpCode.SpawnPlayer, this.HandleSpawnPlayerPacket },
                /*49 */{ PacketOpCode.StartPlaying, this.HandleStartPlayingPacket },
                /*82 */{ PacketOpCode.NetModule, this.HandleNetModulePacket },
                /*129*/{ PacketOpCode.FinshedConnectingToServer, this.HandleFinshedConnectingToServerPacket }
            };
            this.loggerFactory = loggerFactory;

            foreach (var field in this.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            {
                if (field.FieldType != typeof(HandlerList<Interceptor, PacketHandlerArgs>))
                {
                    continue;
                }
                var handlerList = new HandlerList<Interceptor, PacketHandlerArgs>(
                    this.loggerFactory.CreateLogger($"{typeof(PacketEvents).FullName}.{field.Name}")
                );
                field.SetValue(this, handlerList);
            }
        }

        private async Task SpecificPacketHandle(Interceptor source, PacketHandlerArgs args)
        {
            if (!this.packetHandlerLookup.TryGetValue(args.TerrariaPacket.OpCode, out var handler))
            {
                return;
            }

            await handler(source, args);
        }

        public readonly HandlerList<Interceptor, PacketHandlerArgs> PacketReceivedHandlers = null;

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
        public readonly HandlerList<Interceptor, PacketHandlerArgs> ConnectHandlers = null;

        private Task HandleConnectPacket(Interceptor sender, PacketHandlerArgs args)
        {
            return this.ConnectHandlers.Invoke(sender, args);
        }
        #endregion

        #region Disconnect
        public readonly HandlerList<Interceptor, PacketHandlerArgs> DisconnectHandlers = null;

        private Task HandleDisconnectPacket(Interceptor sender, PacketHandlerArgs args)
        {
            return this.DisconnectHandlers.Invoke(sender, args);
        }
        #endregion

        #region SetPlayerSlot
        public readonly HandlerList<Interceptor, PacketHandlerArgs> SetPlayerSlot = null;
        private Task HandleSetPlayerSlotPacket(Interceptor sender, PacketHandlerArgs args)
        {
            return this.SetPlayerSlot.Invoke(sender, args);
        }
        #endregion

        #region PlayerInfo
        public readonly HandlerList<Interceptor, PacketHandlerArgs> PlayerInfoHandlers = null;

        private Task HandlePlayerInfoPacket(Interceptor sender, PacketHandlerArgs args)
        {
            return this.PlayerInfoHandlers.Invoke(sender, args);
        }
        #endregion

        #region RequestTileData
        public readonly HandlerList<Interceptor, PacketHandlerArgs> RequestTileDataHandlers = null;

        private Task HandleRequestTileDataPacket(Interceptor sender, PacketHandlerArgs args)
        {
            return this.RequestTileDataHandlers.Invoke(sender, args);
        }
        #endregion

        #region ResponseWorldInfo
        public readonly HandlerList<Interceptor, PacketHandlerArgs> ResponseWorldInfoHandlers = null;
        private Task HandleResponseWorldInfoPacket(Interceptor sender, PacketHandlerArgs args)
        {
            return this.ResponseWorldInfoHandlers.Invoke(sender, args);
        }
        #endregion

        #region SpawnPlayer
        public readonly HandlerList<Interceptor, PacketHandlerArgs> SpawnPlayerHandlers = null;
        private Task HandleSpawnPlayerPacket(Interceptor sender, PacketHandlerArgs args)
        {
            return this.SpawnPlayerHandlers.Invoke(sender, args);
        }
        #endregion

        #region StartPlaying
        public readonly HandlerList<Interceptor, PacketHandlerArgs> StartPlayingHandlers = null;
        private Task HandleStartPlayingPacket(Interceptor sender, PacketHandlerArgs args)
        {
            return this.StartPlayingHandlers.Invoke(sender, args);
        }
        #endregion

        #region NetModuleHandlers
        public readonly HandlerList<Interceptor, PacketHandlerArgs> NetModuleHandlers = null;

        private Task HandleNetModulePacket(Interceptor trigger, PacketHandlerArgs args)
        {
            return this.NetModuleHandlers.Invoke(trigger, args);
        }
        #endregion

        #region FinshedConnectingToServer
        public readonly HandlerList<Interceptor, PacketHandlerArgs> FinshedConnectingToServerHandlers = null;
        private Task HandleFinshedConnectingToServerPacket(Interceptor sender, PacketHandlerArgs args)
        {
            return this.FinshedConnectingToServerHandlers.Invoke(sender, args);
        }
        #endregion
    }
}
