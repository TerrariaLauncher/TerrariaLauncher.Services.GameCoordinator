using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Modules;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads
{
    public enum NetModuleId : ushort
    {
        Liquid = 0,
        Text = 1,
        Ping = 2,
        Ambience = 3,
        Bestiary = 4,
        CreativeUnlocks = 5,
        CreativePowers = 6,
        CreativeUnlocksPowerReport = 7,
        TeleportPylon = 8,
        Particles = 9,
        CreativePowerPermissions = 10
    }

    static class NetModule
    {
        private static HashSet<ushort> netModuleIds = new HashSet<ushort>();
        static NetModule()
        {
            foreach (var type in Enum.GetValues<NetModuleId>())
            {
                netModuleIds.Add((ushort)type);
            }
        }

        public static bool IsNetModuleIdValid(ushort netModuleId)
        {
            return netModuleIds.Contains(netModuleId);
        }
    }

    class NetModule<TPayload> : PacketStructure where TPayload : class, IModuleStructure
    {
        public override PacketOpCode OpCode => PacketOpCode.NetModule;

        public NetModuleId ModuleId { get; set; }
        public TPayload Payload { get; set; }
    }
}
