using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Proxy;
using TerrariaLauncher.Services.GameCoordinator.Proxy.Events;

namespace TerrariaLauncher.Services.GameCoordinator.Plugins
{
    class PacketDump: Plugin
    {
        PacketEvents packetEvents;
        ILogger<PacketDump> logger;
        public PacketDump(
            PacketEvents packetEvents,
            ILogger<PacketDump> logger)
        {
            this.packetEvents = packetEvents;
            this.logger = logger;
        }

        public override Task Load(CancellationToken cancellationToken = default)
        {
            this.packetEvents.PacketReceivedHandlers.Register(this.OnPacket, 100, true);
            return Task.CompletedTask;
        }

        public override Task Unload(CancellationToken cancellationToken = default)
        {
            this.packetEvents.PacketReceivedHandlers.Deregister(this.OnPacket);
            return Task.CompletedTask;
        }

        public Task OnPacket(Interceptor sender, PacketHandlerArgs args)
        {
            //this.logger.LogInformation("From: {Origin} \t OpCode: {OpCode} \t Ignored: {Ignored}", 
            //    args.TerrariaPacket.Origin,
            //    args.TerrariaPacket.OpCode,
            //    args.Ignored
            //    );
            return Task.CompletedTask;
        }
    }
}
