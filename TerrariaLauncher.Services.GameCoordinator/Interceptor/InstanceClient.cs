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
using TerrariaLauncher.Commons.DomainObjects;

namespace TerrariaLauncher.Services.GameCoordinator
{
    class InstanceClient : IDisposable
    {
        private Instance instance;
        private Socket socket;
        private Pipe socketPipe;

        private ObjectPool<TerrariaPacket> terrariaPacketPool;
        private InterceptorChannels interceptorChannels;

        public InstanceClient(
            ObjectPool<TerrariaPacket> terrariaPacketPool,
            InterceptorChannels interceptorChannels
            )
        {
            this.terrariaPacketPool = terrariaPacketPool;
            this.interceptorChannels = interceptorChannels;
        }

        internal async Task Loop(CancellationToken cancellationToken)
        {
            using (var cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                var task1 = this.ReadDataFromSocket(cancellationTokenSource.Token);
                var task2 = this.ParsePacket(cancellationTokenSource.Token);
                var task3 = this.WriteDataToSocket(cancellationTokenSource.Token);

                await Task.WhenAny(task1, task2, task3);

                cancellationTokenSource.Cancel();
            }
        }

        public async Task Connect(Instance instance, CancellationToken cancellationToken)
        {
            this.instance = instance;

            if (this.socketPipe is null)
            {
                this.socketPipe = new Pipe();
            }
            else
            {
                this.socketPipe.Reset();
            }

            var instanceIpAddress = await NetworkUtils.GetIPv4(this.instance.Host);
            var instanceIPEndPoint = new IPEndPoint(instanceIpAddress, this.instance.Port);
            this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            await this.socket.ConnectAsync(instanceIPEndPoint, cancellationToken);
        }

        public void Disconect()
        {
            if (this.socket is null) return;

            try
            {
                this.socket.Shutdown(SocketShutdown.Both);
                this.socket.Disconnect(false);
                this.socket.Close();
            }
            catch (ObjectDisposedException)
            {

            }
            finally
            {
                this.socket.Dispose();
                this.socket = null;
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
                        packet.Origin = PacketOrigin.Server;
                        packetBuffer.CopyTo(packet.Buffer.Span);
                        try
                        {
                            await this.interceptorChannels.InstanceClientRaw.Writer.WriteAsync(packet, cancellationToken);
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
            while (await this.interceptorChannels.InstanceClientProcessed.Reader.WaitToReadAsync(cancellationToken))
            {
                while (this.interceptorChannels.InstanceClientProcessed.Reader.TryRead(out var packet))
                {
                    try
                    {
                        var buffer = packet.Buffer;
                        do
                        {
                            int numSendBytes = await this.socket.SendAsync(buffer, SocketFlags.None, cancellationToken);
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
                    this.Disconect();
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
