using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Modules;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Structures;
using TerrariaLauncher.Services.GameCoordinator.Pools;
using TerrariaLauncher.Services.GameCoordinator.Proxy;

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
        public TextMessageHelper(
            ObjectPool<TerrariaPacket> terrariaPacketPool
            )
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
                }, cancellationToken);

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
            return this.CreateMessage(message, Red, cancellationToken);
        }

        public Task<TerrariaPacket> CreateWarningMessage(string message, CancellationToken cancellationToken = default)
        {
            return this.CreateMessage(message, OrangeRed, cancellationToken);
        }

        public Task<TerrariaPacket> CreateInfoMessage(string message, CancellationToken cancellationToken = default)
        {
            return this.CreateMessage(message, Yellow, cancellationToken);
        }

        public Task<TerrariaPacket> CreateSuccessMessage(string message, CancellationToken cancellationToken = default)
        {
            return this.CreateMessage(message, Green, cancellationToken);
        }

        public async Task SendMessage(string message, Color color, TerrariaClient terrariaClient, CancellationToken cancellationToken = default)
        {
            var packet = await this.CreateMessage(message, color, cancellationToken);
            try
            {
                await terrariaClient.SendingPacketChannel.Writer.WriteAsync(packet, cancellationToken);
            }
            catch
            {
                this.terrariaPacketPool.Return(packet);
                throw;
            }
        }

        public Task SendErrorMessage(string message, TerrariaClient terrariaClient, CancellationToken cancellationToken = default)
        {
            return this.SendMessage(message, Red, terrariaClient, cancellationToken);
        }

        public Task SendInfoMessage(string message, TerrariaClient terrariaClient, CancellationToken cancellationToken = default)
        {
            return this.SendMessage(message, Yellow, terrariaClient, cancellationToken);
        }

        public Task SendWarningMessage(string message, TerrariaClient terrariaClient, CancellationToken cancellationToken = default)
        {
            return this.SendMessage(message, OrangeRed, terrariaClient, cancellationToken);
        }

        public Task SendSuccessMessage(string message, TerrariaClient terrariaClient, CancellationToken cancellationToken = default)
        {
            return this.SendMessage(message, Green, terrariaClient, cancellationToken);
        }
    }
}
