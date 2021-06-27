using System;
using System.Buffers;
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
    class SyncPlayerDeserializer : IStructureDeserializer<SyncPlayer>
    {
        IStructureDeserializer<NetString> netStringDeserializer;
        IStructureDeserializer<Color> colorDeserializer;
        IStructureDeserializer<DifficultyFlags> difficultyFlagsDeserializer;
        IStructureDeserializer<TorchFlags> torchFlagsDeserializer;
        public SyncPlayerDeserializer(
            IStructureDeserializer<NetString> netStringDeserializer,
            IStructureDeserializer<Color> colorDeserializer,
            IStructureDeserializer<DifficultyFlags> difficultyFlagsDeserializer,
            IStructureDeserializer<TorchFlags> torchFlagsDeserializer)
        {
            this.netStringDeserializer = netStringDeserializer;
            this.colorDeserializer = colorDeserializer;
            this.difficultyFlagsDeserializer = difficultyFlagsDeserializer;
            this.torchFlagsDeserializer = torchFlagsDeserializer;
        }

        public async Task<SyncPlayer> Deserialize(PipeReader pipeReader, CancellationToken cancellationToken = default)
        {
            var syncPlayer = new SyncPlayer();
            var readResult = await pipeReader.ReadAsync(cancellationToken).ConfigureAwait(false);
            var buffer = readResult.Buffer;

            static void ReadAllBeforeName(ref ReadOnlySequence<byte> buffer, SyncPlayer syncPlayer)
            {
                var reader = new SequenceReader<byte>(buffer);
                if (!reader.TryRead(out var value)) throw new InvalidOperationException();
                syncPlayer.PlayerId = value;
                if (!reader.TryRead(out value)) throw new InvalidOperationException();
                syncPlayer.SkinVarient = value;
                if (!reader.TryRead(out value)) throw new InvalidOperationException();
                syncPlayer.Hair = value;
                buffer = reader.UnreadSequence;
            }
            ReadAllBeforeName(ref buffer, syncPlayer);
            pipeReader.AdvanceTo(buffer.Start);

            syncPlayer.Name = await this.netStringDeserializer.Deserialize(pipeReader, cancellationToken).ConfigureAwait(false);

            readResult = await pipeReader.ReadAsync(cancellationToken).ConfigureAwait(false);
            buffer = readResult.Buffer;
            static void ReadAllBeforeHairColor(ref ReadOnlySequence<byte> buffer, SyncPlayer syncPlayer)
            {
                var reader = new SequenceReader<byte>(buffer);
                if (!reader.TryRead(out var value)) throw new InvalidOperationException();
                syncPlayer.HairDye = value;
                if (!reader.TryRead(out value)) throw new InvalidOperationException();
                syncPlayer.HideVisibleAccessoryBitsByte1 = value;
                if (!reader.TryRead(out value)) throw new InvalidOperationException();
                syncPlayer.HideVisibleAccessoryBitsByte2 = value;
                if (!reader.TryRead(out value)) throw new InvalidOperationException();
                syncPlayer.HideMisc = value;
                buffer = reader.UnreadSequence;
            }
            ReadAllBeforeHairColor(ref buffer, syncPlayer);
            pipeReader.AdvanceTo(buffer.Start);

            syncPlayer.HairColor = await this.colorDeserializer.Deserialize(pipeReader, cancellationToken).ConfigureAwait(false);
            syncPlayer.SkinColor = await this.colorDeserializer.Deserialize(pipeReader, cancellationToken).ConfigureAwait(false);
            syncPlayer.EyeColor = await this.colorDeserializer.Deserialize(pipeReader, cancellationToken).ConfigureAwait(false);
            syncPlayer.ShirtColor = await this.colorDeserializer.Deserialize(pipeReader, cancellationToken).ConfigureAwait(false);
            syncPlayer.UnderShirtColor = await this.colorDeserializer.Deserialize(pipeReader, cancellationToken).ConfigureAwait(false);
            syncPlayer.PantsColor = await this.colorDeserializer.Deserialize(pipeReader, cancellationToken).ConfigureAwait(false);
            syncPlayer.ShoeColor = await this.colorDeserializer.Deserialize(pipeReader, cancellationToken).ConfigureAwait(false);
            syncPlayer.DifficultyFlags = await this.difficultyFlagsDeserializer.Deserialize(pipeReader, cancellationToken).ConfigureAwait(false);
            syncPlayer.TorchFlags = await this.torchFlagsDeserializer.Deserialize(pipeReader, cancellationToken).ConfigureAwait(false);

            return syncPlayer;
        }
    }
}
