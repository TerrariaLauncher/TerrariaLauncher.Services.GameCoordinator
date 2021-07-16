using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Pools;

namespace TerrariaLauncher.Services.GameCoordinator.Proxy
{
    class TerrariaClient : IDisposable
    {
        private Socket socket;
        private Pipe socketPipe;

        private ObjectPool<TerrariaPacket> terrariaPacketPool;
        private InterceptorChannels interceptorChannels;

        private bool disconnected = false;

        public TerrariaClient(
            ObjectPool<TerrariaPacket> terrariaPacketPool,
            InterceptorChannels interceptorChannels)
        {
            this.socketPipe = new Pipe();
            this.terrariaPacketPool = terrariaPacketPool;
            this.interceptorChannels = interceptorChannels;
        }

        public IPEndPoint IPEndPoint { get; protected set; }

        internal void Connect(Socket terrariaClient)
        {
            this.socket = terrariaClient;
            this.IPEndPoint = this.socket.RemoteEndPoint as IPEndPoint;
        }

        internal async Task ReadPacketsFromSocket(CancellationToken cancellationToken)
        {
            if (this.socket is null) throw new InvalidOperationException("Terraria Client Socket is not set.");

            var task1 = this.ReadDataFromSocket(cancellationToken);
            var task2 = this.ParsePacket(cancellationToken);

            await Task.WhenAll(task1, task2);
        }

        internal Task WritePacketsToSocket(CancellationToken cancellationToken)
        {
            if (this.socket is null) throw new InvalidOperationException("Terraria Client Socket is not set.");

            var task3 = this.WriteDataToSocket(cancellationToken);
            return task3;
        }

        public void Disconnect()
        {
            if (this.disconnected) return;

            try
            {
                this.socket.Shutdown(SocketShutdown.Both);
                this.socket.Disconnect(false);
                this.socket.Close();
            }
            catch
            {

            }
            finally
            {
                this.socket.Dispose();
                this.disconnected = true;
            }
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
                        try
                        {
                            await this.interceptorChannels.TerrariaClientRaw.Writer.WriteAsync(packet, cancellationToken);
                        }
                        catch
                        {
                            this.terrariaPacketPool.Return(packet);
                        }
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
            while (await this.interceptorChannels.TerrariaClientProcessed.Reader.WaitToReadAsync(cancellationToken))
            {
                while (this.interceptorChannels.TerrariaClientProcessed.Reader.TryRead(out var packet))
                {
                    try
                    {
                        var buffer = packet.Buffer;
                        do
                        {
                            var numSendBytes = await this.socket.SendAsync(buffer, SocketFlags.None, cancellationToken);
                            buffer = buffer.Slice(numSendBytes);
                        } while (!buffer.IsEmpty);
                    }
                    finally
                    {
                        this.terrariaPacketPool.Return(packet);
                    }
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
                    this.Disconnect();
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
