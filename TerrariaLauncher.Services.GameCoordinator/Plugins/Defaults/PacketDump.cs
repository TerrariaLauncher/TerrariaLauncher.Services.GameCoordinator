using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator.Plugins
{
    class PacketDump: Plugin
    {
        PacketEvents packetEvents;
        public PacketDump(PacketEvents packetEvents)
        {
            this.packetEvents = packetEvents;
        }

        public override Task Load(CancellationToken cancellationToken = default)
        {
            this.packetEvents.PacketReceivedHandlers.Register(this.OnPacket);
            return Task.CompletedTask;
        }

        public override Task Unload(CancellationToken cancellationToken = default)
        {
            this.packetEvents.PacketReceivedHandlers.Deregister(this.OnPacket);
            return Task.CompletedTask;
        }

        public Task OnPacket(Interceptor sender, PacketHandlerArgs args)
        {
            return Task.CompletedTask;
        }
    }
}
