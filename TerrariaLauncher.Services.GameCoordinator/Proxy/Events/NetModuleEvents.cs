using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Modules;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Structures;

namespace TerrariaLauncher.Services.GameCoordinator.Proxy.Events
{
    class NetModuleHandlerArgs : PacketHandlerArgs
    {
        public NetModuleId NetModuleId { get; set; }
        public Memory<byte> NetModulePayload { get; set; }
    }

    class TextModuleHandlerArgs : NetModuleHandlerArgs
    {
        public TextModule TextModule { get; set; }
    }

    class ChatModuleHandlerArgs : NetModuleHandlerArgs
    {
        public ChatModule ChatModule { get; set; }
    }

    class NetModuleEvents
    {
        private Dictionary<NetModuleId, Handler<Interceptor, NetModuleHandlerArgs>> handlers;

        IStructureDeserializerLocator structureDeserializerLocator;
        ILoggerFactory loggerFactory;
        public NetModuleEvents(
            IStructureDeserializerLocator structureDeserializerLocator,
            ILoggerFactory loggerFactory)
        {
            this.handlers = new Dictionary<NetModuleId, Handler<Interceptor, NetModuleHandlerArgs>>()
            {
                { NetModuleId.Text, this.HandleTextModuleAndChatModule }
            };

            this.structureDeserializerLocator = structureDeserializerLocator;
            this.loggerFactory = loggerFactory;
            this.TextModuleHandlers = new HandlerList<Interceptor, TextModuleHandlerArgs>(
                this.loggerFactory.CreateLogger($"{typeof(NetModuleEvents).FullName}.{nameof(TextModuleHandlers)}")
            );
            this.ChatModuleHandlers = new HandlerList<Interceptor, ChatModuleHandlerArgs>(
                this.loggerFactory.CreateLogger($"{typeof(NetModuleEvents).FullName}.{nameof(ChatModuleHandlers)}")
            );
        }

        internal async Task OnNetModule(Interceptor sender, PacketHandlerArgs args)
        {
            if (!TryReadNetModuleType(args.TerrariaPacket, out var netModuleType))
            {
                return;
            }

            if (!this.handlers.TryGetValue(netModuleType.Value, out var handler))
            {
                return;
            }

            var netModulePayload = args.TerrariaPacket.Payload.Slice(sizeof(ushort));

            var netModuleHandlerArgs = new NetModuleHandlerArgs
            {
                CancellationToken = args.CancellationToken,
                Handled = args.Handled,
                TerrariaPacket = args.TerrariaPacket,
                Ignored = args.Ignored,
                NetModuleId = netModuleType.Value,
                NetModulePayload = netModulePayload
            };

            await handler(sender, netModuleHandlerArgs);
            args.Handled = netModuleHandlerArgs.Handled;
            args.Ignored = netModuleHandlerArgs.Ignored;
        }

        private static bool TryReadNetModuleType(TerrariaPacket terrariaPacket, out NetModuleId? netModuleType)
        {
            var netModuleIdBytes = terrariaPacket.Buffer.Span.Slice(TerrariaPacket.PayloadPosition, sizeof(ushort));
            if (!System.Buffers.Binary.BinaryPrimitives.TryReadUInt16LittleEndian(netModuleIdBytes, out var netModuleId)
                || !NetModule.IsNetModuleIdValid(netModuleId))
            {
                netModuleType = null;
                return false;
            }

            netModuleType = (NetModuleId)netModuleId;
            return true;
        }

        #region TextModule
        /// <summary>
        /// Handlers for handling text module from server.
        /// </summary>
        public readonly HandlerList<Interceptor, TextModuleHandlerArgs> TextModuleHandlers;

        /// <summary>
        /// Handlers for handling chat module from client.
        /// </summary>
        public readonly HandlerList<Interceptor, ChatModuleHandlerArgs> ChatModuleHandlers;

        private async Task HandleTextModuleAndChatModule(Interceptor sender, NetModuleHandlerArgs args)
        {
            if (args.TerrariaPacket.Origin == PacketOrigin.Client)
            {
                var chatModuleHandlerArgs = new ChatModuleHandlerArgs
                {
                    CancellationToken = args.CancellationToken,
                    Handled = args.Handled,
                    TerrariaPacket = args.TerrariaPacket,
                    Ignored = args.Ignored,
                    NetModuleId = args.NetModuleId,
                    ChatModule = await ParseChatModule(args.NetModulePayload, args.CancellationToken).ConfigureAwait(false)
                };

                await this.ChatModuleHandlers.Invoke(sender, chatModuleHandlerArgs);
                args.Handled = chatModuleHandlerArgs.Handled;
                args.Ignored = chatModuleHandlerArgs.Ignored;
            }
            else
            {
                var networkTextHandlerArgs = new TextModuleHandlerArgs
                {
                    CancellationToken = args.CancellationToken,
                    Handled = args.Handled,
                    TerrariaPacket = args.TerrariaPacket,
                    Ignored = args.Ignored,
                    NetModuleId = args.NetModuleId,
                    TextModule = ParseTextModule(args.NetModulePayload.Span)
                };

                await this.TextModuleHandlers.Invoke(sender, networkTextHandlerArgs);
                args.Handled = networkTextHandlerArgs.Handled;
                args.Ignored = networkTextHandlerArgs.Ignored;
            }
        }

        private static TextModule ParseTextModule(Span<byte> netModulePayload)
        {
            var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(netModulePayload.Length);
            var bufferWrapper = new Span<byte>(buffer);
            netModulePayload.CopyTo(bufferWrapper);
            try
            {
                var textModule = new TextModule();
                using (var stream = new System.IO.MemoryStream(buffer))
                {
                    using (var bufferReader = new System.IO.BinaryReader(stream))
                    {
                        textModule.AuthorIndex = bufferReader.ReadByte();
                        textModule.MessageText = ParseNetworkText(bufferReader);
                        textModule.Color = new Color()
                        {
                            R = bufferReader.ReadByte(),
                            G = bufferReader.ReadByte(),
                            B = bufferReader.ReadByte()
                        };
                    }
                }
                return textModule;
            }
            finally
            {
                System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private static NetworkText ParseNetworkText(System.IO.BinaryReader bufferReader)
        {
            var networkText = new NetworkText();
            networkText.Mode = (NetworkText.ModeType)bufferReader.ReadByte();
            networkText.Text = bufferReader.ReadString();
            if (networkText.Mode != NetworkText.ModeType.Literal)
            {
                networkText.SubstitutionLength = bufferReader.ReadByte();
                networkText.Substitutions = new NetworkText[networkText.SubstitutionLength];
                for (int index = 0; index < networkText.SubstitutionLength; ++index)
                {
                    networkText.Substitutions[index] = ParseNetworkText(bufferReader);
                }
            }
            return networkText;
        }

        private async Task<ChatModule> ParseChatModule(Memory<byte> netModulePayload, CancellationToken cancellationToken = default)
        {            
            var pipe = new System.IO.Pipelines.Pipe();
            await pipe.Writer.WriteAsync(netModulePayload, cancellationToken).ConfigureAwait(false);
            await pipe.Writer.CompleteAsync().ConfigureAwait(false);

            var chatModuleDeserializer = this.structureDeserializerLocator.Get<ChatModule>();
            var chatModule = await chatModuleDeserializer.Deserialize(pipe.Reader, cancellationToken).ConfigureAwait(false);

            await pipe.Reader.CompleteAsync().ConfigureAwait(false);
            return chatModule;
        }
        #endregion
    }
}
