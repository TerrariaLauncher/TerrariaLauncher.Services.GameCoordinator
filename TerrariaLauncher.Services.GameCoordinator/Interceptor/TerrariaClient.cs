using Microsoft.Extensions.ObjectPool;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator
{
    class TerrariaClient : IDisposable
    {
        private Socket socket;
        private Pipe socketPipe;

        private ObjectPool<TerrariaPacket> terrariaPacketPool;
        private System.Threading.Channels.ChannelReader<TerrariaPacket> packetChannelReader;
        private System.Threading.Channels.ChannelWriter<TerrariaPacket> packetChannelWriter;

        public TerrariaClient(
            ObjectPool<TerrariaPacket> terrariaPacketPool,
            InterceptorChannels interceptorChannels)
        {
            this.socketPipe = new Pipe();
            this.terrariaPacketPool = terrariaPacketPool;

            this.packetChannelReader = interceptorChannels.ProcessedPacketChannelForTerrariaClient.Reader;
            this.packetChannelWriter = interceptorChannels.PacketChannelForTerrariaClient.Writer;
        }

        public void SetSocket(Socket terrariaClient)
        {
            this.socket = terrariaClient;
        }

        public async Task Loop(CancellationToken cancellationToken)
        {
            if (this.socket is null) throw new InvalidOperationException("Terraria Client Socket is not set.");

            var task1 = this.ReadDataFromSocket(cancellationToken);
            var task2 = this.ParsePacket(cancellationToken);
            var task3 = this.WriteDataToSocket(cancellationToken);

            await Task.WhenAny(task1, task2, task3);
        }

        public Task Disconnect()
        {
            this.socket.Shutdown(SocketShutdown.Both);
            this.socket.Disconnect(false);
            this.socket.Close();
            this.socket.Dispose();

            return Task.CompletedTask;
        }

        private async Task ReadDataFromSocket(CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    Memory<byte> buffer = this.socketPipe.Writer.GetMemory(1024);
                    int numReceiveBytes = await this.socket.ReceiveAsync(buffer, SocketFlags.None, cancellationToken);
                    if (numReceiveBytes == 0)
                    {
                        break;
                    }

                    this.socketPipe.Writer.Advance(numReceiveBytes);
                    var result = await this.socketPipe.Writer.FlushAsync(cancellationToken);

                    if (result.IsCompleted || result.IsCanceled)
                    {
                        break;
                    }
                }
            }
            finally
            {
                await this.socketPipe.Writer.CompleteAsync();
            }
        }

        private async Task ParsePacket(CancellationToken cancellationToken)
        {
            try
            {
                while (true)
                {
                    var readResult = await this.socketPipe.Reader.ReadAsync(cancellationToken);
                    if (readResult.IsCanceled) break;

                    var buffer = readResult.Buffer;
                    while (GetPacketBuffer(ref buffer, out var packetBuffer))
                    {
                        if (packetBuffer.Length > short.MaxValue) continue;

                        var packet = this.terrariaPacketPool.Get();
                        packet.Length = (int)packetBuffer.Length;
                        packet.Origin = PacketOrigin.Client;
                        packetBuffer.CopyTo(packet.Buffer.Span);
                        await this.packetChannelWriter.WriteAsync(packet, cancellationToken);
                    }
                    this.socketPipe.Reader.AdvanceTo(buffer.Start, buffer.End);

                    if (readResult.IsCompleted)
                    {
                        break;
                    }
                }
            }
            finally
            {
                await this.socketPipe.Reader.CompleteAsync();
            }
        }

        private static bool GetPacketBuffer(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> packetBuffer)
        {
            if (ParsePacketLengthHeader(buffer, out var length) && length <= buffer.Length)
            {
                packetBuffer = buffer.Slice(0, length);
                buffer = buffer.Slice(length);

                return true;
            }

            packetBuffer = ReadOnlySequence<byte>.Empty;
            return false;
        }

        private static bool ParsePacketLengthHeader(ReadOnlySequence<byte> buffer, out short length)
        {
            var reader = new System.Buffers.SequenceReader<byte>(buffer);
            return reader.TryReadLittleEndian(out length);
        }

        private async Task WriteDataToSocket(CancellationToken cancellationToken)
        {
            while (true)
            {
                TerrariaPacket packet = null;
                try
                {
                    packet = await this.packetChannelReader.ReadAsync(cancellationToken);
                    var buffer = packet.Buffer;
                    do
                    {
                        var numSendBytes = await this.socket.SendAsync(buffer, SocketFlags.None, cancellationToken);
                        buffer = buffer.Slice(numSendBytes);
                    } while (!buffer.IsEmpty);
                }
                finally
                {
                    if (packet is not null) this.terrariaPacketPool.Return(packet);
                }
            }
        }

        #region Disposed
        private bool disposed;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    this.socket.Dispose();
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
