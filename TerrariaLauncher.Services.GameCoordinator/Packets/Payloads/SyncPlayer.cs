using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.BitFlags;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Structures;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads
{
    [PacketStructure(OpCode: PacketOpCode.SyncPlayer)]
    class SyncPlayer: PacketStructure
    {
        public override PacketOpCode OpCode => PacketOpCode.SyncPlayer;

        public byte PlayerId { get; set; }
        public byte SkinVarient { get; set; }
        public byte Hair { get; set; }
        public NetString Name { get; set; }
        public byte HairDye { get; set; }
        public byte HideVisibleAccessoryBitsByte1 { get; set; }
        public byte HideVisibleAccessoryBitsByte2 { get; set; }
        public byte HideMisc { get; set; }
        public Color HairColor { get; set; }
        public Color SkinColor { get; set; }
        public Color EyeColor { get; set; }
        public Color ShirtColor { get; set; }
        public Color UnderShirtColor { get; set; }
        public Color PantsColor { get; set; }
        public Color ShoeColor { get; set; }
        public DifficultyFlags DifficultyFlags { get; set; }
        public TorchFlags TorchFlags { get; set; }
    }
}
