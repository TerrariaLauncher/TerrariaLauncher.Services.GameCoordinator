using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads;
using TerrariaLauncher.Services.GameCoordinator.Plugins.Helpers;
using TerrariaLauncher.Services.GameCoordinator.Pools;
using TerrariaLauncher.Services.GameCoordinator.Proxy;
using TerrariaLauncher.Services.GameCoordinator.Proxy.Events;

namespace TerrariaLauncher.Services.GameCoordinator.Plugins
{
    class ConnectingPhase : Plugin
    {
        PacketEvents packetEvents;
        InstanceClientEvents instanceClientEvents;
        InterceptorEvents interceptorEvents;
        TerrariaPacketPoolHelper terrariaPacketPoolHelper;

        enum ConnectingState : byte
        {
            Unset,
            FirstConnect,
            LaterConnect,
            SetPlayerSlot,
            RequestTileData,
            SpawnPlayer
        }

        ConcurrentDictionary<InstanceClient, byte> _playerId = new ConcurrentDictionary<InstanceClient, byte>();
        ConcurrentDictionary<InstanceClient, ConnectingState> _connectingState = new ConcurrentDictionary<InstanceClient, ConnectingState>();

        public ConnectingPhase(
            PacketEvents packetEvents,
            InstanceClientEvents instanceClientEvents,
            InterceptorEvents interceptorEvents,
            TerrariaPacketPoolHelper terrariaPacketPoolHelper)
        {
            this.packetEvents = packetEvents;
            this.instanceClientEvents = instanceClientEvents;
            this.interceptorEvents = interceptorEvents;
            this.terrariaPacketPoolHelper = terrariaPacketPoolHelper;
        }

        public override Task Load(CancellationToken cancellationToken = default)
        {
            this.interceptorEvents.InterceptorStartedHandlers.Register(this.HandleIntercetorStarted);
            this.interceptorEvents.InterceptorStoppedHandlers.Register(this.HandleInterceptorStopped);
            this.instanceClientEvents.SocketConnectedHandlers.Register(this.HandleInstanceClientSocketConnected);
            this.packetEvents.PacketReceivedHandlers.Register(this.HandlePacket);
            this.packetEvents.SetPlayerSlot.Register(this.HandleSetPlayerSlotPacket);
            this.packetEvents.ResponseWorldInfoHandlers.Register(this.HandleResponseWorldInfoPacket);
            this.packetEvents.StartPlayingHandlers.Register(this.HandleStartPlayingPacket);
            return Task.CompletedTask;
        }

        public override Task Unload(CancellationToken cancellationToken = default)
        {
            this.interceptorEvents.InterceptorStartedHandlers.Deregister(this.HandleIntercetorStarted);
            this.interceptorEvents.InterceptorStoppedHandlers.Deregister(this.HandleInterceptorStopped);
            this.instanceClientEvents.SocketConnectedHandlers.Deregister(this.HandleInstanceClientSocketConnected);
            this.packetEvents.PacketReceivedHandlers.Deregister(this.HandlePacket);
            this.packetEvents.SetPlayerSlot.Deregister(this.HandleSetPlayerSlotPacket);
            this.packetEvents.ResponseWorldInfoHandlers.Deregister(this.HandleResponseWorldInfoPacket);
            this.packetEvents.StartPlayingHandlers.Deregister(this.HandleStartPlayingPacket);
            return Task.CompletedTask;
        }

        private Task HandleIntercetorStarted(Interceptor sender, InterceptorStartedArgs args)
        {
            this._connectingState.TryAdd(sender.InstanceClient, ConnectingState.Unset);
            return Task.CompletedTask;
        }

        private Task HandleInterceptorStopped(Interceptor sender, InterceptorStoppedArgs args)
        {
            this._connectingState.TryRemove(sender.InstanceClient, out _);
            this._playerId.TryRemove(sender.InstanceClient, out _);
            return Task.CompletedTask;
        }

        private async Task HandleInstanceClientSocketConnected(InstanceClient sender, InstanceClientSocketConnectedArgs args)
        {
            if (!this._connectingState.TryGetValue(sender, out var state)) return;
            if (state == ConnectingState.Unset)
            {
                this._connectingState.TryUpdate(sender, ConnectingState.FirstConnect, state);
                return;
            }
            this._connectingState.TryUpdate(sender, ConnectingState.LaterConnect, state);

            while (sender.SendingPacketChannel.Reader.TryRead(out _)) { }

            await this.terrariaPacketPoolHelper.ReturnPoolIfFailed(async (packet) =>
            {
                await packet.SerializePayload(PacketOrigin.Client, new Connect()
                {
                    Version = $"Terraria238"
                }, args.CancellationToken);

                await sender.SendingPacketChannel.Writer.WriteAsync(packet, args.CancellationToken);
            });
        }

        static IReadOnlySet<PacketOpCode> s_connectingPhasePacketOpCodes = new HashSet<PacketOpCode>()
        {
            PacketOpCode.Connect,
            PacketOpCode.Disconnect,
            PacketOpCode.SetPlayerSlot,
            PacketOpCode.SyncPlayer,
            PacketOpCode.InventorySlot,
            PacketOpCode.RequestWorldInfo,
            PacketOpCode.ResponseWorldInfo,
            PacketOpCode.RequestTileData,
            PacketOpCode.StatusText,
            PacketOpCode.TitleSection,
            PacketOpCode.FrameSection,
            PacketOpCode.SpawnPlayer,
            PacketOpCode.SocialHandshake,
            PacketOpCode.PlayerHealth,
            PacketOpCode.PlayerMana,
            PacketOpCode.PlayerBuffs,
            PacketOpCode.SendPassword,
            PacketOpCode.ClientUUID
        };
        private async Task HandlePacket(Interceptor sender, PacketHandlerArgs args)
        {
            if (args.TerrariaPacket.Origin != PacketOrigin.Client) return;

            if (!this._connectingState.TryGetValue(sender.InstanceClient, out var state)) return;
            if (state <= ConnectingState.FirstConnect) return;
            if (state >= ConnectingState.SpawnPlayer) return;

            if (!s_connectingPhasePacketOpCodes.Contains(args.TerrariaPacket.OpCode))
            {
                args.Handled = true;
                args.Ignored = true;
            }

            await Task.CompletedTask;
        }

        private async Task HandleSetPlayerSlotPacket(Interceptor sender, PacketHandlerArgs args)
        {
            var setPlayerSlot = await args.TerrariaPacket.DeserializePayload<SetPlayerSlot>(args.CancellationToken);
            this._playerId.AddOrUpdate(sender.InstanceClient, setPlayerSlot.PlayerId, (instanceClient, existedId) =>
            {
                return setPlayerSlot.PlayerId;
            });
            
            if (!this._connectingState.TryGetValue(sender.InstanceClient, out var state)) return;
            if (state == ConnectingState.LaterConnect)
            {
                this._connectingState.TryUpdate(sender.InstanceClient, ConnectingState.SetPlayerSlot, state);
            }
        }

        private async Task HandleResponseWorldInfoPacket(Interceptor sender, PacketHandlerArgs args)
        {
            if (!this._connectingState.TryGetValue(sender.InstanceClient, out var state)) return;
            if (state != ConnectingState.SetPlayerSlot) return;

            await this.terrariaPacketPoolHelper.ReturnPoolIfFailed(async (packet) =>
            {
                await packet.SerializePayload(PacketOrigin.Client, new RequestTileData()
                {
                    X = -1,
                    Y = -1
                });

                await sender.InstanceClient.SendingPacketChannel.Writer.WriteAsync(packet, args.CancellationToken);
            });

            this._connectingState.TryUpdate(sender.InstanceClient, ConnectingState.RequestTileData, state);
        }

        private async Task HandleStartPlayingPacket(Interceptor sender, PacketHandlerArgs args)
        {
            if (!this._connectingState.TryGetValue(sender.InstanceClient, out var state)) return;
            if (state != ConnectingState.RequestTileData) return;

            if (!this._playerId.TryGetValue(sender.InstanceClient, out var playerId)) return;
            await this.terrariaPacketPoolHelper.ReturnPoolIfFailed(async (packet) =>
            {
                await packet.SerializePayload(PacketOrigin.Client, new SpawnPlayer()
                {
                    PlayerId = playerId,
                    SpawnX = -1,
                    SpawnY = -1,
                    RespawnTimeRemaining = 0,
                    PlayerSpawnContext = PlayerSpawnContext.RecallFromItem,
                });

                await sender.InstanceClient.SendingPacketChannel.Writer.WriteAsync(packet, args.CancellationToken);
            });

            this._connectingState.TryUpdate(sender.InstanceClient, ConnectingState.SpawnPlayer, state);
        }
    }
}
