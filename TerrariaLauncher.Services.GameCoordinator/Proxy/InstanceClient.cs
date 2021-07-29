using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using TerrariaLauncher.Commons.DomainObjects;
using TerrariaLauncher.Services.GameCoordinator.Pools;
using TerrariaLauncher.Services.GameCoordinator.Proxy.Events;

namespace TerrariaLauncher.Services.GameCoordinator.Proxy
{
    /// <summary>
    /// This class represent a connection to a Terraria Server Instance.
    /// 
    /// </summary>
    class InstanceClient : IDisposable, IAsyncDisposable
    {
        Instance instance;
        Socket socket;
        Pipe socketPipe;

        InstanceClientEvents instanceClientEvents;
        ObjectPool<TerrariaPacket> terrariaPacketPool;

        public Channel<TerrariaPacket> ReceivingPacketChannel { get; }

        /// <summary>
        /// Packet written into this channel will be sent directly to connecting server.
        /// </summary>
        public Channel<TerrariaPacket> SendingPacketChannel { get; }

        public InstanceClient(
            InstanceClientEvents instanceClientEvents,
            ObjectPool<TerrariaPacket> terrariaPacketPool
            )
        {
            this.instanceClientEvents = instanceClientEvents;
            this.terrariaPacketPool = terrariaPacketPool;

            this.ReceivingPacketChannel = Channel.CreateUnbounded<TerrariaPacket>();
            this.SendingPacketChannel = Channel.CreateUnbounded<TerrariaPacket>();
        }

        public Instance Instance { get => this.instance; }

        internal async Task Connect(string realm, CancellationToken cancellationToken)
        {
            var instance = await this.instanceClientEvents.OnConnectToRealm(this, realm, cancellationToken);
            if (instance is null) throw new ArgumentException($"Could not find any server instance of the realm '{realm}'.", nameof(realm));
            await this.Connect(instance, cancellationToken);
        }

        CancellationTokenSource cancellationTokenSource;
        Task readPacketsTask;
        Task writePacketsTask;
        private async Task Connect(Instance instance, CancellationToken cancellationToken)
        {
            await this.Disconect();

            await this.instanceClientEvents.OnConnectToInstance(this, instance, cancellationToken);
            this.instance = instance;

            var instanceIpAddress = await NetworkUtils.GetIPv4(this.instance.Host);
            var instanceIPEndPoint = new IPEndPoint(instanceIpAddress, this.instance.Port);
            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await this.socket.ConnectAsync(instanceIPEndPoint, cancellationToken);
            await this.instanceClientEvents.OnSocketConnected(this, cancellationToken);

            if (this.socketPipe is null)
            {
                this.socketPipe = new Pipe();
            }
            else
            {
                this.socketPipe.Reset();
            }
            this.cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            this.readPacketsTask = this.ReadPacketsFromSocket(this.cancellationTokenSource.Token);
            this.writePacketsTask = this.WritePacketsToSocket(this.cancellationTokenSource.Token);
        }

        internal async Task Disconect()
        {
            if (this.socket is null) return;

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

            if (this.readPacketsTask is not null && !this.readPacketsTask.IsCompleted)
            {
                try
                {
                    await readPacketsTask;
                }
                catch { }
            }

            if (this.cancellationTokenSource is not null)
            {
                this.cancellationTokenSource.Cancel();
                this.cancellationTokenSource.Dispose();
                this.cancellationTokenSource = null;
            }

            if (this.writePacketsTask is not null && !this.writePacketsTask.IsCompleted)
            {
                try
                {
                    await this.writePacketsTask;
                }
                catch { }
            }
        }

        private async Task ReadPacketsFromSocket(CancellationToken cancellationToken)
        {
            var task1 = this.ReadDataFromSocket(cancellationToken);
            var task2 = this.ParsePacket(cancellationToken);

            await Task.WhenAll(task1, task2);
        }

        private Task WritePacketsToSocket(CancellationToken cancellationToken)
        {
            var task3 = this.WriteDataToSocket(cancellationToken);
            return task3;
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
                    if (readResult.IsCanceled)
                    {
                        break;
                    }

                    var buffer = readResult.Buffer;
                    while (GetPacketBuffer(ref buffer, out var packetBuffer))
                    {
                        if (packetBuffer.Length > short.MaxValue) continue;

                        var packet = this.terrariaPacketPool.Get();
                        packet.Length = (int)packetBuffer.Length;
                        packet.Origin = PacketOrigin.Server;
                        packetBuffer.CopyTo(packet.Buffer.Span);
                        try
                        {
                            await this.ReceivingPacketChannel.Writer.WriteAsync(packet, cancellationToken);
                        }
                        catch
                        {
                            this.terrariaPacketPool.Return(packet);
                            throw;
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
                            int numSendBytes = await this.socket.SendAsync(buffer, SocketFlags.None, cancellationToken);
                            buffer = buffer.Slice(numSendBytes);
                        } while (!buffer.IsEmpty);

                        this.terrariaPacketPool.Return(packet);
                    }
                    catch
                    {
                        this.terrariaPacketPool.Return(packet);
                        throw;
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
                _ = this.Disconect();
                this.ReceivingPacketChannel.Writer.TryComplete();
                this.SendingPacketChannel.Writer.TryComplete();

                if (this.writePacketsTask is not null)
                {
                    SpinWait.SpinUntil(() =>
                    {
                        return this.writePacketsTask.IsCompleted;
                    });
                }
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
            if (this.disposed) return;

            await this.Disconect();
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
            this.Dispose(disposing: false);
            GC.SuppressFinalize(this);
        }
        #endregion
    }
}

/* This code is reserved for reference.
 * The method is use to process received data after call ReceiveAsync (with SocketAsyncEventArgs).
 * 
private void ProcessTerrariaClientReceive(
    MemoryStream packetStream, BinaryWriter packetWriter,
    SocketAsyncEventArgs readArgs,
    Action<byte[], int> packetDataHandler)
{
    const int LengthHeaderSize = sizeof(short);

    Span<byte> receivedBytes = new Span<byte>(readArgs.Buffer, 0, readArgs.BytesTransferred);
    int numReadBytes = 0;
    do
    {
        int numBytesToRead;
        if (this.terrariaClientIncomingPacketStream.Position < LengthHeaderSize)
        {
            int numRemainingLengthHeaderBytes = LengthHeaderSize - (int)this.terrariaClientIncomingPacketStream.Position;
            numBytesToRead = numRemainingLengthHeaderBytes;
            if (numBytesToRead < readArgs.BytesTransferred - numReadBytes)
            {
                numBytesToRead = readArgs.BytesTransferred - numReadBytes;
            }
            this.terrariaClientIncomingPacketWriter.Write(receivedBytes.Slice(numReadBytes, numBytesToRead));
            numReadBytes += numBytesToRead;
        }

        if (this.terrariaClientIncomingPacketStream.Position < LengthHeaderSize)
        {
            continue;
        }

        Span<byte> lengthHeaderBytes = this.terrariaClientIncomingPacketStream.GetBuffer().AsSpan(0, LengthHeaderSize);
        int packetLength = BitConverter.ToInt16(lengthHeaderBytes);
        numBytesToRead = packetLength - (int)this.terrariaClientIncomingPacketStream.Position;

        if (numBytesToRead > readArgs.BytesTransferred - numReadBytes)
        {
            numBytesToRead = readArgs.BytesTransferred - numReadBytes;
        }

        this.terrariaClientIncomingPacketWriter.Write(receivedBytes.Slice(numReadBytes, numBytesToRead));
        numReadBytes += numBytesToRead;

        if (this.terrariaClientIncomingPacketStream.Position == packetLength)
        {
            var packetBytes = this.terrariaClientIncomingPacketStream.GetBuffer()
                .AsMemory(0, (int)this.terrariaClientIncomingPacketStream.Position);
            var packet = terrariaPacketPool.Get();
            packet.LengthHeader = (int)this.terrariaClientIncomingPacketStream.Position;
            packetBytes.CopyTo(packet.Buffer);
                    
            // Push packet.

            this.terrariaClientIncomingPacketStream.Seek(0, SeekOrigin.Begin);
        }
    } while (numReadBytes < receivedBytes.Length);
}
*/

/* This code is reserved for reference.
 * SocketAsyncEventArgs with Event-based Asynchronous Pattern (EAP) to Task-based.
 * 
sealed class SocketAwaitable : System.Runtime.CompilerServices.INotifyCompletion
{
    private readonly static Action SENTINEL = () => { };

    internal bool wasCompleted;
    internal Action continuation;
    internal SocketAsyncEventArgs eventArgs;

    public SocketAsyncEventArgs EventArgs { get => this.eventArgs; }

    public SocketAwaitable(SocketAsyncEventArgs eventArgs)
    {
        if (eventArgs is null) throw new ArgumentNullException(nameof(eventArgs));
        this.eventArgs = eventArgs;
        this.eventArgs.Completed += HandleSocketEventComplete;
    }

    internal void HandleSocketEventComplete(object sender, SocketAsyncEventArgs e)
    {
        var prev = this.continuation ?? Interlocked.CompareExchange(
                ref this.continuation, SENTINEL, null);
        if (prev is not null) prev();
    }

    internal void Reset()
    {
        this.wasCompleted = false;
        this.continuation = null;
    }

    public SocketAwaitable GetAwaiter()
    {
        return this;
    }

    public bool IsCompleted { get => this.wasCompleted; }

    public void OnCompleted(Action continuation)
    {
        // Nếu HandleSocketEventComplete chạy trước, this.continuation chỉ có thể là SENTINEL.
        // Nếu HandleSocketEventComplete chạy sau, this.continuation gán bằng continuation và HandleSocketEventComplete sẽ chạy continuation.
        if (this.continuation == SENTINEL || Interlocked.CompareExchange(ref this.continuation, continuation, null) == SENTINEL)
        {
            Task.Run(continuation);
        }
    }

    public int GetResult()
    {
        if (this.eventArgs.SocketError != SocketError.Success)
        {
            throw new SocketException((int)this.eventArgs.SocketError);
        }

        return this.eventArgs.BytesTransferred;
    }
}

static class SocketExtensions
{
    public static SocketAwaitable ReceiveAsync(this Socket socket, SocketAwaitable awaitable)
    {
        awaitable.Reset();
        if (!socket.ReceiveAsync(awaitable.eventArgs))
        {
            awaitable.wasCompleted = true;
        }
        return awaitable;
    }

    public static SocketAwaitable SendAsync(this Socket socket, SocketAwaitable awaitable)
    {
        awaitable.Reset();
        if (!socket.SendAsync(awaitable.eventArgs))
        {
            awaitable.wasCompleted = true;
        }
        return awaitable;
    }

    public static async Task<Socket> AcceptAsync(this Socket socket, SocketAwaitable awaitable)
    {
        awaitable.Reset();
        if (!socket.AcceptAsync(awaitable.eventArgs))
        {
            awaitable.wasCompleted = true;
        }
        await awaitable;
        return awaitable.EventArgs.AcceptSocket;
    }
}

static class SocketAwaitableExtensions
{
    public static void ResetBeforeReturnToPool(this SocketAwaitable awaitable)
    {
        awaitable.eventArgs.Completed -= awaitable.HandleSocketEventComplete;
    }
}
*/
