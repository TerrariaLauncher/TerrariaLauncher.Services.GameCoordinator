using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons;
using TerrariaLauncher.Services.GameCoordinator.Pools;

namespace TerrariaLauncher.Services.GameCoordinator
{
    class TerrariaPacket: IDisposable
    {
        private static readonly HashSet<byte> opByteValues = new HashSet<byte>();

        static TerrariaPacket()
        {
            foreach (var opCode in Enum.GetValues<PacketOpCode>())
            {
                opByteValues.Add((byte)opCode);
            }
        }

        public const int MinimumPacketLength = HeaderLength;
        public const int MaximumPacketLength = short.MaxValue;

        public const int LengthHeaderPosition = 0;
        public const int LengthHeaderLength = sizeof(short);
        public const int OpCodeHeaderPosition = LengthHeaderPosition + LengthHeaderLength;
        public const int OpCodeHeaderLength = sizeof(byte);

        public const int HeaderLength = LengthHeaderLength + OpCodeHeaderLength;
        public const int PayloadPosition = HeaderLength;

        private readonly ArrayPool<byte> arrayPool;
        private byte[] buffer = null;
        private Memory<byte> bufferWrapper = Memory<byte>.Empty;
        private int length = 0;

        IStructureSerializerDispatcher structureSerializerDispatcher;
        IStructureDeserializerDispatcher structureDeserializerDispatcher;

        public TerrariaPacket(
            ArrayPool<byte> arrayPool,
            IStructureSerializerDispatcher structureSerializerDispatcher,
            IStructureDeserializerDispatcher structureDeserializerDispatcher)
        {
            this.arrayPool = arrayPool;
            this.structureSerializerDispatcher = structureSerializerDispatcher;
            this.structureDeserializerDispatcher = structureDeserializerDispatcher;
        }

        public ref readonly Memory<byte> Buffer
        {
            get => ref this.bufferWrapper;
        }

        public Memory<byte> Payload
        {
            get => this.bufferWrapper.Slice(PayloadPosition);
        }

        /// <summary>
        /// A <see cref="short"/> value, converted from the first two bytes of the packet. The value indicates the number of bytes of both header (length, opcode) and payload.
        /// Packet length must be in inclusive range between <see cref="MinimumPacketLength"/> and <see cref="MaximumPacketLength"/>.
        /// Specific case when the length equal to 0, meaning that the internal buffer will be return to the array pool.
        /// Because of that, always set length to 0 when not use the packet to prevent memory leak.
        /// </summary>
        public int Length
        {
            get => this.length;
            set
            {
                if ((value != 0 && value < MinimumPacketLength) || value > MaximumPacketLength)
                    throw new ArgumentOutOfRangeException($"Packet length is in range [${sizeof(short)}, {short.MaxValue}].");

                // Continue with:
                // value = 0 || MinimumPacketLength <= value <= MaximumPacketLength

                if (value != this.length)
                {
                    if (this.length != 0)
                    {
                        this.arrayPool.Return(this.buffer);
                    }

                    if (value == 0)
                    {
                        this.buffer = null;
                        this.bufferWrapper = Memory<byte>.Empty;
                    }
                    else
                    {
                        // Rented buffer length may larger than the value.
                        this.buffer = this.arrayPool.Rent(value);
                        this.bufferWrapper = new Memory<byte>(this.buffer, 0, value);
                        BinaryPrimitives.WriteInt16LittleEndian(this.bufferWrapper.Slice(LengthHeaderPosition, LengthHeaderLength).Span, (short)value);
                    }

                    this.length = value;
                }
            }
        }

        public PacketOrigin Origin { get; set; }

        public PacketOpCode OpCode
        {
            get
            {
                ref byte opByte = ref this.bufferWrapper.Span.Slice(OpCodeHeaderPosition, OpCodeHeaderLength)[0];
                if (opByteValues.Contains(opByte))
                {
                    return (PacketOpCode)opByte;
                }

                return PacketOpCode.UnListed;
            }
            set
            {
                if (this.bufferWrapper.IsEmpty) throw new InvalidOperationException();
                this.bufferWrapper.Span.Slice(OpCodeHeaderPosition, OpCodeHeaderLength)[0] = (byte)value;
            }
        }

        public void SetPayload(PacketOrigin origin, PacketOpCode opCode, Span<byte> payload)
        {
            this.Length = HeaderLength + payload.Length;
            this.Origin = origin;
            this.OpCode = opCode;
            payload.CopyTo(this.Buffer.Span.Slice(PayloadPosition));
        }

        public async Task SetPayload(PacketOrigin origin, PacketOpCode opCode, PipeReader reader, CancellationToken cancellationToken = default)
        {
            var readResult = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            this.Length = HeaderLength + (int)readResult.Buffer.Length;
            this.Origin = origin;
            this.OpCode = opCode;
            readResult.Buffer.CopyTo(this.Payload.Span);
            reader.AdvanceTo(readResult.Buffer.End);
        }

        public async Task SerializePayload<TStructure>(PacketOrigin origin, TStructure structure, CancellationToken cancellationToken = default)
        where TStructure : IPacketStructure
        {
            var pipe = new Pipe();
            await this.structureSerializerDispatcher.Serialize(structure, pipe.Writer, cancellationToken).ConfigureAwait(false);
            await pipe.Writer.CompleteAsync().ConfigureAwait(false);
            await this.SetPayload(origin, structure.OpCode, pipe.Reader, cancellationToken).ConfigureAwait(false);
            await pipe.Reader.CompleteAsync().ConfigureAwait(false);
        }

        public async Task<TStructure> DeserializePayload<TStructure>(CancellationToken cancellationToken = default)
        where TStructure : IStructure
        {
            var pipe = new Pipe();
            await pipe.Writer.WriteAsync(this.Payload, cancellationToken).ConfigureAwait(false);
            await pipe.Writer.CompleteAsync().ConfigureAwait(false);
            var structure = await this.structureDeserializerDispatcher.Deserialize<TStructure>(pipe.Reader, cancellationToken).ConfigureAwait(false);
            await pipe.Reader.CompleteAsync().ConfigureAwait(false);
            return structure;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.Length = 0;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
