using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Structures
{
    class NetworkText : Structure
    {
        public enum ModeType : byte
        {
            Literal = 0,
            Formattable = 1,
            LocalizationKey = 2
        }

        public ModeType Mode { get; set; }
        public string Text { get; set; }
        public byte SubstitutionLength { get; set; } = 0;
        public NetworkText[] Substitutions { get; set; } = null;
    }
}
