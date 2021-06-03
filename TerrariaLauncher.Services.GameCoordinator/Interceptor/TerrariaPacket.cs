using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator
{
    enum PacketOpCode : byte
    {
        Connect = 1,
        Unknown = 255
    }

    public enum PacketOrigin
    {
        Client,
        Server,
        Interceptor
    }

    class TerrariaPacket
    {
        public const int MinimumPacketLength = LengthHeaderLength + OpCodeHeaderLength;
        public const int MaximumPacketLength = short.MaxValue;

        public const int LengthHeaderPosition = 0;
        public const int LengthHeaderLength = sizeof(short);        
        public const int OpCodeHeaderPosition = LengthHeaderPosition + LengthHeaderLength;
        public const int OpCodeHeaderLength = sizeof(byte);

        private readonly ArrayPool<byte> arrayPool;
        private byte[] buffer = null;
        private Memory<byte> bufferWrapper = Memory<byte>.Empty;
        private int length = 0;

        public TerrariaPacket(ArrayPool<byte> arrayPool)
        {
            this.arrayPool = arrayPool;
        }

        public Memory<byte> Buffer
        {
            get => this.bufferWrapper;
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

                if (value != this.length)
                {
                    if (this.length != 0)
                    {
                        this.arrayPool.Return(this.buffer);
                    }

                    if (value != 0)
                    {
                        // Rented buffer length may larger than the value.
                        this.buffer = this.arrayPool.Rent(value);
                        this.bufferWrapper = new Memory<byte>(this.buffer, 0, value);
                        System.Buffers.Binary.BinaryPrimitives.WriteInt16LittleEndian(this.bufferWrapper.Slice(LengthHeaderPosition, LengthHeaderLength).Span, (short)value);
                    }
                    else
                    {
                        this.buffer = null;
                        this.bufferWrapper = Memory<byte>.Empty;
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
                if (Enum.IsDefined(typeof(PacketOpCode), opByte))
                {
                    return (PacketOpCode)opByte;
                }
                return PacketOpCode.Unknown;
            }
            set
            {
                this.bufferWrapper.Span.Slice(OpCodeHeaderPosition, OpCodeHeaderLength)[0] = (byte)value;
            }
        }
    }
}
