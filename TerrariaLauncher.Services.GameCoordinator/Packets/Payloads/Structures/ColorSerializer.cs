using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Structures
{
    class ColorSerializer : StructureSerializer<Color>
    {
        protected override Task Implementation(Color structure, PipeWriter writer, CancellationToken cancellationToken = default)
        {
            var buffer = writer.GetSpan(3);
            buffer[0] = structure.R;
            buffer[1] = structure.G;
            buffer[2] = structure.B;
            writer.Advance(sizeof(byte) * 3);
            return Task.CompletedTask;
        }
    }
}
