using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Modules;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Structures;
using TerrariaLauncher.Services.GameCoordinator.Pools;

namespace TerrariaLauncher.Services.GameCoordinator.Plugins.Helpers
{
    class TextMessageHelper
    {
        public static Color Red = new Color() { R = 255, G = 0, B = 0 };
        public static Color Green = new Color() { R = 0, G = 255, B = 0 };
        public static Color Blue = new Color() { R = 0, G = 0, B = 255 };
        public static Color OrangeRed = new Color() { R = 255, G = 69, B = 0 };
        public static Color Yellow = new Color() { R = 255, G = 255, B = 0 };

        ObjectPool<TerrariaPacket> terrariaPacketPool;
        public TextMessageHelper(ObjectPool<TerrariaPacket> terrariaPacketPool)
        {
            this.terrariaPacketPool = terrariaPacketPool;
        }

        public string EmbedStringWithColorTag(string message, byte r = 255, byte g = 255, byte b = 255)
        {
            return $"[c/{r.ToString("X2")}{g.ToString("X2")}{b.ToString("X2")}:{message}]";
        }

        public async Task<TerrariaPacket> CreateMessage(string message, Color color, CancellationToken cancellationToken = default)
        {
            var packet = this.terrariaPacketPool.Get();
            try
            {
                var textModule = new TextModule()
                {
                    AuthorIndex = 255,
                    Color = new Packets.Payloads.Structures.Color()
                    {
                        R = color.R,
                        G = color.G,
                        B = color.B
                    },
                    MessageText = new Packets.Payloads.Structures.NetworkText()
                    {
                        Mode = Packets.Payloads.Structures.NetworkText.ModeType.Literal,
                        Text = message
                    }
                };
                await packet.SerializePayload(PacketOrigin.Server, new NetModule<TextModule>()
                {
                    ModuleId = textModule.NetModuleId,
                    Payload = textModule
                });

                return packet;
            }
            catch
            {
                this.terrariaPacketPool.Return(packet);
                throw;
            }
        }

        public Task<TerrariaPacket> CreateErrorMessage(string message, CancellationToken cancellationToken = default)
        {
            return this.CreateMessage(message, Red);
        }

        public Task<TerrariaPacket> CreateWarningMessage(string message, CancellationToken cancellationToken = default)
        {
            return this.CreateMessage(message, OrangeRed);
        }

        public Task<TerrariaPacket> CreateInfoMessage(string message, CancellationToken cancellationToken = default)
        {
            return this.CreateMessage(message, Yellow);
        }

        public Task<TerrariaPacket> CreateSuccessMessage(string message, CancellationToken cancellationToken = default)
        {
            return this.CreateMessage(message, Green);
        }
    }
}
