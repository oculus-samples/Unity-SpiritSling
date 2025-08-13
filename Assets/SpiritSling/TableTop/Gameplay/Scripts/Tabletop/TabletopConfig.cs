// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections.Generic;
using UnityEngine;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// Tabletop core variables
    /// </summary>
    [CreateAssetMenu(fileName = "TableTop Config", menuName = "SpiritSling/TableTop/TableTop Config")]
    public class TabletopConfig : ScriptableObject
    {
        // Singleton
        private static TabletopConfig Instance;

        public static TabletopConfig Get(bool reset = false)
        {
            if (Instance == null || reset)
                Instance = Resources.Load<TabletopConfig>("Data/TableTopConfig");

            if (Instance == null)
                Log.Error("Could not find any TableTopConfig object in any Resources/Data/ folder");

            return Instance;
        }

        [System.Serializable]
        public struct SlingSettingsPerHeight
        {
            public int height;
            public int range;
            public float maxVelocity;
        }

        private void OnValidate()
        {
            // This is for shader debugging purposes (to reflect the changes in the editor)
            TableTopShaderGlobalPropertySetter.SetGlobalShaderProperties(this);
        }

        [System.Serializable]
        public struct BoardSettingsPerPlayerCount
        {
            public int playerCount;
            public List<int> boardRotation;
        }

        // --------------------------------------------------------
        [Header("Game")]
        [Tooltip("Default values for game variables")]
        public TabletopGameSettings defaultGameSettings = new TabletopGameSettings
        {
            // Default values
            seed = 42,
            timePhase = 20,
            roundsBeforeCloc = 1,
            isClocRandom = false,
            clocRandomCount = 6,
            lavaDamage = 1,
            kodamaStartHealthPoints = 4,
            kodamaHasImmunity = true,
            kodamaMoveRange = 2,
            kodamaSummonRange = 1,
            kodamaSummonPerTurn = 1,
            slingshotStartHealthPoints = 2,
            slingshotDamage = 1,
            slingBallShotsPerTurn = 1,
            lootboxStartHealthPoints = 1,
            lootboxDamage = 1,
            lootboxStartCount = 3,
            lootboxMinCount = 1,
            lootBoxSpawnCountPerTurn = 1,
            lootBoxHeightDownAmount = 2,
            lootBoxHeightDownStrongAmount = 3,
            lootBoxHeightUpAmount = 2,
            lootBoxHeightUpStrongAmount = 3,
            lootBoxNormalStrongChances = 3
        };

        [Tooltip("Time (in seconds) between two phases for a player")]
        public float phaseDelay = 0.5f;

        [Header("Board")]
        [Tooltip("Grid configuration (radius, cell size, etc)")]
        public HexGridConfig gridConfig;

        [Tooltip("Local Y position of the cliff on a tile")]
        public float cellCliffYOffset = -0.04f;

        [Tooltip("Placement animation duration for all pawns")]
        public float pawnPlacementAnimationDuration = 0.3f;

        [Tooltip("Settings per player count")]
        public List<BoardSettingsPerPlayerCount> boardSettingsPerPlayerCount;

        [Tooltip("Distance from cell when falling out of the board")]
        public float pawnEjectionDistance = 0.05f;

        // --------------------------------------------------------
        [Header("Kodama")]
        [Tooltip("Distance at which the kodama will move back to his initial cell")]
        public float kodamaUnsnapDistance = 0.1f;

        [Tooltip("Distance around kodamas where we should avoid respawning")]
        public int kodamaUnsafeRange = 1;

        // --------------------------------------------------------
        [Header("Slingshot")]
        [Tooltip("Color on the slingshot balls when hightlighted to be grabbed")]
        public Color slingBallHightlightColor = Color.white;

        [Tooltip("Color on the slingshot balls when disabled")]
        public Color slingBallDisabledColor = Color.gray;

        [Tooltip("Color on the pulling line when pulling distance is valid")]
        public Color slingBallValidPullDistanceColor = Color.green;

        [Tooltip("Color on the pulling line when pulling distance is invalid")]
        public Color slingBallInvalidPullDistanceColor = Color.red;

        [Tooltip("Time to restore a ball after it has been shot")]
        public float slingBallRestoreDelay = 1f;

        [Tooltip("Distance min to pull the sling handle and validate a shot")]
        public float slingBallMinDistance = .01f;

        [Tooltip("Distance max to pull the sling handle")]
        public float slingBallMaxDistance = .14f;

        [Tooltip("Added distance before cancelling the shot")]
        public float slingBallToleranceDistance = .12f;

        [Tooltip("Settings for the slingshots depending on its height on the board")]
        public List<SlingSettingsPerHeight> slingSettingsPerHeight;

        [Tooltip("How fast the ball moves on the trajectory")]
        public float slingBallSpeed = 1.2f;

        // --------------------------------------------------------
        [Header("Players visuals")]
        public Color[] playersColors;

        public GameObject[] playerBoardPrefabs;
        public GameObject[] KodamaVisualPrefabs;
        public GameObject[] HealthPointVisualPrefabs;
        public GameObject[] HighlightVFXPrefabs;
        public GameObject[] PawnDeathVFX;
        public GameObject PawnSmokeTrailVFX;
        public GameObject PawnDropVFX;
        public GameObject PawnSpawnVFX;
        public GameObject PawnStartTileDecalVFX;

        [Tooltip("The color the pawn is tinted for a brief time when it takes damage")]
        [ColorUsage(false, true)]
        public Color DamageVFXColor = Color.white;

        [Tooltip("The color the pawn is tinted for a brief time when it heals")]
        [ColorUsage(false, true)]
        public Color HealVFXColor = Color.green;

        [Header("Player Wrist Bands VFX")]
        public GameObject[] MagicWristBandPrefabs;
        public Material[] MagicWristBandLineRendererMaterials;

        [Tooltip("Distance from which the embers of the wristband start being attracted to your PlayerBoard")]
        [Range(0, 3f)]
        public float WristBandFarAwayMinDistance = 1f;

        [Header("Tiles Shake")]
        [Range(0, 5f)]
        public float ShakeIntensity = 0.1f;

        [Range(0, 0.5f)]
        public float ShakeFrequency = 0.2f;

        [Range(0, 0.5f)]
        public float VerticalShakeFrequency = 0.1f;

        [Header("PlayerBoard Stones")]
        [Tooltip("Distance form which the stone interaction highlight starts")]
        [Range(0, 0.5f)]
        public float StoneInteractionRange = 0.2f;

        [Tooltip("Distance form which the stone skip phase highlight starts")]
        [Range(0, 0.25f)]
        public float StoneSkipRange = 0.05f;

        [Tooltip("The minimum time the player's hand has to stay in the skip range to skip the current phase")]
        [Range(0, 3)]
        public float StoneSkipMinStayDuration = 1.5f;

        [Header("LootBox VFX")]
        [Tooltip("Delay before the LootBox effect is played after picking up a LootBox." +
            "The indexes in this array correspond to the value of the LootItem.Types enums.")]
        [Range(0, 10)]
        public float[] LootBoxEffectDelays;

        [Header("Player Rig")]
        [Tooltip("Distance from the start tile to the camera rig ")]
        public float rigDistance = 0.8f;

        // --------------------------------------------------------
        [Header("Player Board")]
        [Tooltip("Distance of the playerboard from the start tile")]
        public float playerboardDistance = 0.2f;

        [Tooltip("Scale of the player board")]
        public float playerboardScale = 1f;

        [Tooltip("Y offset of the player board")]
        public float playerboardYOffset;

        [Header("Tips")]
        public float tipsDisplayTime = 4f;

        public List<string> tipsList;

        // --------------------------------------------------------

        public Color PlayerColor(int playerIndex)
        {
            return playersColors[(playerIndex) % playersColors.Length];
        }

        private T GetPlayerObject<T>(T[] allObjects, int playerIndex)
        {
            return allObjects[playerIndex % allObjects.Length];
        }

        public GameObject PlayerBoardPrefab(int playerIndex)
        {
            return GetPlayerObject(playerBoardPrefabs, playerIndex);
        }

        public GameObject PlayerKodamaVisual(int playerIndex)
        {
            return GetPlayerObject(KodamaVisualPrefabs, playerIndex);
        }

        public GameObject PlayerHealthPointVisual(int playerIndex)
        {
            return GetPlayerObject(HealthPointVisualPrefabs, playerIndex);
        }

        public GameObject PlayerHighlightVisual(int playerIndex)
        {
            return GetPlayerObject(HighlightVFXPrefabs, playerIndex);
        }

        public GameObject PlayerPawnDeathVFX(int playerIndex)
        {
            return GetPlayerObject(PawnDeathVFX, playerIndex);
        }

        public GameObject GetMagicWristBand(int playerIndex)
        {
            return GetPlayerObject(MagicWristBandPrefabs, playerIndex);
        }
        public Material GetMagicWristBandLineRendererMaterial(int playerIndex)
        {
            return GetPlayerObject(MagicWristBandLineRendererMaterials, playerIndex);
        }


        public SlingSettingsPerHeight GetSlingShotSettingsForHeight(int height) => slingSettingsPerHeight.Find(s => s.height == height);

        public BoardSettingsPerPlayerCount GetBoardSettingsPerPlayerCount(int playerCount) =>
            boardSettingsPerPlayerCount.Find(s => s.playerCount == playerCount);
    }
}