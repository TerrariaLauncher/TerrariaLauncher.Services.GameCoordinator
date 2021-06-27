using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Structures
{
    class NetString : Structure
    {
        public string Value { get; set; }

        public override string ToString()
        {
            return this.Value;
        }

        public static implicit operator string(NetString v)
        {
            return v.ToString();
        }

        public static implicit operator NetString(string v)
        {
            return new NetString()
            {
                Value = v
            };
        }
    }
}
