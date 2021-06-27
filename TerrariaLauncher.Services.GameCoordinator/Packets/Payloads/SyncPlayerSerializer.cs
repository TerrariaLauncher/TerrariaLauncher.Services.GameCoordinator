using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.BitFlags;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Commons;
using TerrariaLauncher.Services.GameCoordinator.Packets.Payloads.Structures;

namespace TerrariaLauncher.Services.GameCoordinator.Packets.Payloads
{
    class SyncPlayerSerializer : StructureSerializer<SyncPlayer>
    {
        IStructureSerializer<NetString> netStringSerializer;
        IStructureSerializer<Color> colorSerializer;
        IStructureSerializer<DifficultyFlags> difficultyFlagsSerializer;
        IStructureSerializer<TorchFlags> torchFlagsSerializer;
        public SyncPlayerSerializer(
            IStructureSerializer<NetString> netStringSerializer,
            IStructureSerializer<Color> colorSerializer,
            IStructureSerializer<DifficultyFlags> difficultyFlagsSerializer,
            IStructureSerializer<TorchFlags> torchFlagsSerializer)
        {
            this.netStringSerializer = netStringSerializer;
            this.colorSerializer = colorSerializer;
            this.difficultyFlagsSerializer = difficultyFlagsSerializer;
            this.torchFlagsSerializer = torchFlagsSerializer;
        }

        protected override async Task Implementation(SyncPlayer structure, PipeWriter pipeWriter, CancellationToken cancellationToken = default)
        {
            static void WriteBeforeName(SyncPlayer structure, PipeWriter pipeWriter)
            {
                int bufferMinSize = sizeof(byte) * 3;
                var buffer = pipeWriter.GetSpan(bufferMinSize);
                buffer[0] = structure.PlayerId;
                buffer[1] = structure.SkinVarient;
                buffer[2] = structure.Hair;
                pipeWriter.Advance(bufferMinSize);
            }
            WriteBeforeName(structure, pipeWriter);

            await this.netStringSerializer.Serialize(structure.Name, pipeWriter, false, cancellationToken).ConfigureAwait(false);

            static void WriteBeforeHairColor(SyncPlayer structure, PipeWriter pipeWriter)
            {
                int bufferMinSize = sizeof(byte) * 4;
                var buffer = pipeWriter.GetSpan(bufferMinSize);
                buffer[0] = structure.HairDye;
                buffer[1] = structure.HideVisibleAccessoryBitsByte1;
                buffer[2] = structure.HideVisibleAccessoryBitsByte2;
                buffer[3] = structure.HideMisc;
                pipeWriter.Advance(bufferMinSize);
            }
            WriteBeforeHairColor(structure, pipeWriter);

            await this.colorSerializer.Serialize(structure.HairColor, pipeWriter, false, cancellationToken).ConfigureAwait(false);
            await this.colorSerializer.Serialize(structure.SkinColor, pipeWriter, false, cancellationToken).ConfigureAwait(false);
            await this.colorSerializer.Serialize(structure.EyeColor, pipeWriter, false, cancellationToken).ConfigureAwait(false);
            await this.colorSerializer.Serialize(structure.ShirtColor, pipeWriter, false, cancellationToken).ConfigureAwait(false);
            await this.colorSerializer.Serialize(structure.UnderShirtColor, pipeWriter, false, cancellationToken).ConfigureAwait(false);
            await this.colorSerializer.Serialize(structure.PantsColor, pipeWriter, false, cancellationToken).ConfigureAwait(false);
            await this.colorSerializer.Serialize(structure.ShoeColor, pipeWriter, false, cancellationToken).ConfigureAwait(false);
            await this.difficultyFlagsSerializer.Serialize(structure.DifficultyFlags, pipeWriter, false, cancellationToken).ConfigureAwait(false);
            await this.torchFlagsSerializer.Serialize(structure.TorchFlags, pipeWriter, false, cancellationToken).ConfigureAwait(false);
        }
    }
}
