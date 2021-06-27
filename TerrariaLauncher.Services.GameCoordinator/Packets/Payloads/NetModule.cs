using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads
{
    static class NetModule
    {
        public enum Type : ushort
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

        private static HashSet<ushort> netModuleIds = new HashSet<ushort>();
        static NetModule()
        {
            foreach (var type in Enum.GetValues<NetModule.Type>())
            {
                netModuleIds.Add((ushort)type);
            }
        }

        public static bool IsNetModuleIdValid(ushort netModuleId)
        {
            return netModuleIds.Contains(netModuleId);
        }
    }

    class NetModule<TPayload> : PacketStructure where TPayload: class, IStructure
    {
        public override PacketOpCode OpCode => PacketOpCode.NetModule;

        public NetModule.Type ModuleId { get; set; }
        public TPayload Payload { get; set; }
    }
}
