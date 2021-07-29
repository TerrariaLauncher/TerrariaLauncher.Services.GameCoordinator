using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TerrariaLauncher.Services.GameCoordinator
{
    enum PacketOpCode : byte
    {
        Connect = 1,
        Disconnect = 2,
        /// <summary>
        /// Set player index in the index array (0, 255).
        /// 255 is server player.
        /// </summary>
        SetPlayerSlot = 3,
        SyncPlayer = 4,
        InventorySlot = 5,
        RequestWorldInfo = 6,
        ResponseWorldInfo = 7,
        RequestTileData = 8,
        StatusText = 9,
        TitleSection = 10,
        FrameSection = 11,
        SpawnPlayer = 12,
        PlayerControls = 13,
        PlayerActive = 14,
        Unused = 15,
        PlayerHealth = 16,
        TileChange = 17,
        MenuSunMoon = 18,
        DoorToggle = 19,
        TitleSquare = 20,
        SyncItem = 21,
        ItemOwner = 22,
        SyncNPC = 23,

        SpawnProjectTile = 27,

        RequestPassword = 37,
        SendPassword = 38,

        PlayerMana = 42,

        StartPlaying = 49,

        PlayerBuffs = 50,

        TileCounts = 57,

        ClientUUID = 68,

        NetModule = 82,
        NPCKillCountDeathTally = 83,

        SocialHandshake = 93,

        UpdateTowerShieldStrengths = 101,
        NebulaLevelUpRequest = 102,
        MoonLordCountDown = 103,

        FinshedConnectingToServer = 129,

        SyncCavernMonsterType = 136,

        UnListed = 255
    }
}
