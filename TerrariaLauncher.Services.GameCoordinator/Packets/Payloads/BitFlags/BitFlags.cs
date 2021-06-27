using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.BitFlags
{
    struct BitFlags
    {
        byte value;

        public BitFlags(bool bit0, bool bit1, bool bit2, bool bit3, bool bit4, bool bit5, bool bit6, bool bit7)
        {
            this.value = 0;
            this[0] = bit0;
            this[1] = bit1;
            this[2] = bit2;
            this[3] = bit3;
            this[4] = bit4;
            this[5] = bit5;
            this[6] = bit6;
            this[7] = bit7;
        }

        public BitFlags(bool bit0) : this(bit0, false, false, false, false, false, false, false) { }
        public BitFlags(bool bit0, bool bit1) : this(bit0, bit1, false, false, false, false, false, false) { }
        public BitFlags(bool bit0, bool bit1, bool bit2) : this(bit0, bit1, bit2, false, false, false, false, false) { }
        public BitFlags(bool bit0, bool bit1, bool bit2, bool bit3) : this(bit0, bit1, bit2, bit3, false, false, false, false) { }
        public BitFlags(bool bit0, bool bit1, bool bit2, bool bit3, bool bit4) : this(bit0, bit1, bit2, bit3, bit4, false, false, false) { }
        public BitFlags(bool bit0, bool bit1, bool bit2, bool bit3, bool bit4, bool bit5) : this(bit0, bit1, bit2, bit3, bit4, bit5, false, false) { }
        public BitFlags(bool bit0, bool bit1, bool bit2, bool bit3, bool bit4, bool bit5, bool bit6) : this(bit0, bit1, bit2, bit3, bit4, bit5, bit6, false) { }

        public BitFlags(byte value)
        {
            this.value = value;
        }

        public bool this[int key]
        {
            get => (this.value & (1 << key)) > 0;
            set
            {
                if (value)
                {
                    this.value |= (byte)(1u << key);
                }
                else
                {
                    this.value &= (byte)~(1u << key);
                }
            }
        }

        public byte Value { get => this.value; set => this.value = value; }

        public void Retrieval(out bool bit0, out bool bit1, out bool bit2, out bool bit3, out bool bit4, out bool bit5, out bool bit6, out bool bit7)
        {
            bit0 = this[0];
            bit1 = this[1];
            bit2 = this[2];
            bit3 = this[3];
            bit4 = this[4];
            bit5 = this[5];
            bit6 = this[6];
            bit7 = this[7];
        }

        public void Retrieval(out bool bit0, out bool bit1, out bool bit2, out bool bit3, out bool bit4, out bool bit5, out bool bit6)
        {
            this.Retrieval(out bit0, out bit1, out bit2, out bit3, out bit4, out bit5, out bit6, out _);
        }

        public void Retrieval(out bool bit0, out bool bit1, out bool bit2, out bool bit3, out bool bit4, out bool bit5)
        {
            this.Retrieval(out bit0, out bit1, out bit2, out bit3, out bit4, out bit5, out _, out _);
        }

        public void Retrieval(out bool bit0, out bool bit1, out bool bit2, out bool bit3, out bool bit4)
        {
            this.Retrieval(out bit0, out bit1, out bit2, out bit3, out bit4, out _, out _, out _);
        }

        public void Retrieval(out bool bit0, out bool bit1, out bool bit2, out bool bit3)
        {
            this.Retrieval(out bit0, out bit1, out bit2, out bit3, out _, out _, out _, out _);
        }

        public void Retrieval(out bool bit0, out bool bit1, out bool bit2)
        {
            this.Retrieval(out bit0, out bit1, out bit2, out _, out _, out _, out _, out _);
        }

        public void Retrieval(out bool bit0, out bool bit1)
        {
            this.Retrieval(out bit0, out bit1, out _, out _, out _, out _, out _, out _);
        }

        public void Retrieval(out bool bit0)
        {
            this.Retrieval(out bit0, out _, out _, out _, out _, out _, out _, out _);
        }

        public (bool bit0, bool bit1, bool bit2, bool bit3, bool bit4, bool bit5, bool bit6, bool bit7) Retrieval()
        {
            return (this[0], this[1], this[2], this[3], this[4], this[5], this[6], this[7]);
        }
    }
}
