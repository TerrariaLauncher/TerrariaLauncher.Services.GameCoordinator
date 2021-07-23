using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Pools;
using TerrariaLauncher.Services.GameCoordinator.Proxy.Events;

namespace TerrariaLauncher.Services.GameCoordinator.Proxy
{
    class TerrariaClient : IDisposable, IAsyncDisposable
    {
        private Socket socket;
        private Pipe socketPipe;

        private ObjectPool<TerrariaPacket> terrariaPacketPool;
        TerrariaClientEvents terrariaClientSocketEvents;

        public Channel<TerrariaPacket> ReceivingPacketChannel { get; }

        /// <summary>
        /// Packet written into this channel will be sent directly to connecting client.
        /// </summary>
        public Channel<TerrariaPacket> SendingPacketChannel { get; }

        public TerrariaClient(
            ObjectPool<TerrariaPacket> terrariaPacketPool,
            TerrariaClientEvents terrariaClientSocketEvents)
        {
            this.socketPipe = new Pipe();
            this.terrariaPacketPool = terrariaPacketPool;
            this.terrariaClientSocketEvents = terrariaClientSocketEvents;

            var channelOptions = new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            this.ReceivingPacketChannel = Channel.CreateBounded<TerrariaPacket>(channelOptions);
            this.SendingPacketChannel = Channel.CreateBounded<TerrariaPacket>(channelOptions);
        }

        public IPEndPoint IPEndPoint { get; protected set; }

        Task _readPacketTask;
        Task _writePacketTask;
        Task _completionTask;
        CancellationTokenSource cancellationTokenSource;

        public Task Completion { get => this._completionSource?.Task ?? Task.CompletedTask; }
        TaskCompletionSource _completionSource;

        internal async Task<bool> Connect(Socket terrariaClient, CancellationToken cancellationToken = default)
        {
            if (terrariaClient is null) throw new ArgumentNullException(nameof(terrariaClient));

            await this.Disconnect(cancellationToken: cancellationToken);
            this.socket = terrariaClient;
            this.IPEndPoint = this.socket.RemoteEndPoint as IPEndPoint;
            this.cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            this._completionSource = new TaskCompletionSource();
            this._readPacketTask = this.ReadPacketsFromSocket(this.cancellationTokenSource.Token);
            this._writePacketTask = this.WritePacketsToSocket(this.cancellationTokenSource.Token);
            this._completionTask = this._readPacketTask.ContinueWith((task) => { this._completionSource.TrySetResult(); });

            var shouldDisconnect = await this.terrariaClientSocketEvents.OnTerrariaClientSocketConnected(this, cancellationToken);
            if (shouldDisconnect)
            {
                await this.Disconnect(sendRemainingPackets: true, cancellationToken);
            }
            return shouldDisconnect;
        }

        private async Task ReadPacketsFromSocket(CancellationToken cancellationToken)
        {
            if (this.socket is null) throw new InvalidOperationException("Terraria Client Socket is not set.");

            var task1 = this.ReadDataFromSocket(cancellationToken);
            var task2 = this.ParsePacket(cancellationToken);

            await Task.WhenAll(task1, task2);
        }

        private async Task WritePacketsToSocket(CancellationToken cancellationToken)
        {
            if (this.socket is null) throw new InvalidOperationException("Terraria Client Socket is not set.");

            await this.WriteDataToSocket(cancellationToken);
        }

        public async Task Disconnect(bool sendRemainingPackets = false, CancellationToken cancellationToken = default)
        {
            if (this.socket is null) return;

            try
            {
                this.socket.Shutdown(SocketShutdown.Receive);
            }
            catch { }

            try
            {
                await this._readPacketTask;
            }
            catch { }

            if (sendRemainingPackets)
            {
                SpinWait.SpinUntil(() =>
                {
                    return this.SendingPacketChannel.Reader.Count == 0 || this._writePacketTask.IsCompleted;
                });
            }

            if (this.cancellationTokenSource is not null)
            {
                this.cancellationTokenSource.Cancel();
                this.cancellationTokenSource.Dispose();
                this.cancellationTokenSource = null;
            }

            await this.terrariaClientSocketEvents.OnTerrariaClientSocketDisconnected(this, cancellationToken);

            try
            {
                this.socket.Shutdown(SocketShutdown.Both);
                this.socket.Close();
            }
            catch { }
            finally
            {
                this.socket.Dispose();
                this.socket = null;
            }

            if (this._completionTask is not null)
            {
                await this._completionTask;
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
                            await this.ReceivingPacketChannel.Writer.WriteAsync(packet, cancellationToken);
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
            while (await this.SendingPacketChannel.Reader.WaitToReadAsync(cancellationToken))
            {
                while (this.SendingPacketChannel.Reader.TryRead(out var packet))
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
            if (disposed) return;

            if (disposing)
            {
                _ = this.Disconnect();
                SpinWait.SpinUntil(() =>
                {
                    return this.socket is null;
                });

                this.ReceivingPacketChannel.Writer.TryComplete();
                this.SendingPacketChannel.Writer.TryComplete();
                while (this.ReceivingPacketChannel.Reader.TryRead(out var packet))
                {
                    this.terrariaPacketPool.Return(packet);
                }
                while (this.SendingPacketChannel.Reader.TryRead(out var packet))
                {
                    this.terrariaPacketPool.Return(packet);
                }
            }

            disposed = true;
        }

        protected virtual async ValueTask DisposeAsyncCore()
        {
            if (disposed) return;

            await this.Disconnect();

            this.ReceivingPacketChannel.Writer.TryComplete();
            this.SendingPacketChannel.Writer.TryComplete();
            await foreach (var packet in this.ReceivingPacketChannel.Reader.ReadAllAsync())
            {
                this.terrariaPacketPool.Return(packet);
            }
            await foreach (var packet in this.SendingPacketChannel.Reader.ReadAllAsync())
            {
                this.terrariaPacketPool.Return(packet);
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        public async ValueTask DisposeAsync()
        {
            await this.DisposeAsyncCore();
            Dispose(disposing: false);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}
