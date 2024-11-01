// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// Struct representing a loot item in the game.
    /// </summary>
    [Serializable]
    public struct LootItem
    {
        /// <summary>
        /// Name of the loot item.
        /// </summary>
        public string name;

        /// <summary>
        /// Enumeration of loot item types.
        /// </summary>
        public enum Types
        {
            /// <summary> Gives one health point to current player's kodama. </summary>
            Health,

            /// <summary> Gives full life to current player's kodama. </summary>
            Health_Mega,

            /// <summary> Removes one health point from a random opponent's kodama or slingshot. </summary>
            Impact,

            /// <summary> Removes one health point from three random opponents' kodamas or slingshots. </summary>
            Impact_Mega,

            /// <summary> Removes X height from a random opponent's kodama or slingshot's tile. </summary>
            HeightDown,

            /// <summary> Removes multiple height from a random opponent's kodama or slingshot's tile and surrounding tiles. </summary>
            HeightDown_Mega,

            /// <summary> Adds X height to a local player's random kodama or slingshot's tile. </summary>
            HeightUp,

            /// <summary> Adds multiple height to a local player, a random Kodama or slingshot's tile and surrounding tiles. </summary>
            HeightUp_Mega,
        }

        /// <summary>
        /// Type of the loot item.
        /// </summary>
        public Types Type;

        /// <summary>
        /// Indicates if the effect is normal or strong (mega)
        /// </summary>
        public bool isStrong;

        /// <summary>
        /// Prefab of the loot item.
        /// </summary>
        public GameObject prefab;

        /// <summary>
        /// Prefab for the VFX when picking up the lootbox
        /// </summary>
        public GameObject fxPrefab;

    }

    public static class LootTypeExtension
    {
        public static bool IsStrong(this LootItem.Types type)
        {
            return type is LootItem.Types.Health_Mega or LootItem.Types.Impact_Mega or LootItem.Types.HeightDown_Mega or LootItem.Types.HeightUp_Mega;
        }
    }

    /// <summary>
    /// Manager class for handling loot items in the game.
    /// </summary>
    public class LootsManager : NetworkBehaviour
    {
        public const float AFFECT_PAWN_DELAY = 0.5f;

        public static LootsManager Instance;

        /// <summary>
        /// List of loot items available in the game.
        /// </summary>
        [SerializeField]
        private List<LootItem> items;

        /// <summary>
        /// Prefab for the Kodama VFX when picking up a normal lootbox
        /// </summary>
        [SerializeField]
        private GameObject kodamaSummonFxPrefab;

        /// <summary>
        /// Prefab for the Kodama VFX when picking up a strong lootbox
        /// </summary>
        [SerializeField]
        private GameObject kodamaSummonStrongFxPrefab;

        /// <summary>
        /// Prefab for the VFX when showing target cell of the lootbox
        /// </summary>
        [SerializeField]
        private GameObject linkFxPrefab, linkFxPrefabStrong;

        /// <summary>
        /// Prefab for the VFX when showing target cell of the lootbox
        /// </summary>
        [SerializeField]
        private GameObject cancelFxPrefab, cancelFxPrefabStrong;
        private VFXController linkVfxController;
        private HermiteSpline linkSpline;


        [Networked]
        private bool IsFirstTurn { get; set; } = true;

        /// <summary>
        /// loot boxes currently on the board.
        /// </summary>
        public List<LootBox> LootBoxesOnBoard { get; protected set; }

        private List<HexCell> CellsTaken { get; set; }

        private TabletopGameSettings Settings => TabletopGameManager.Instance.Settings;
        private TabletopGameManager GameManager => TabletopGameManager.Instance;

        private TabletopConfig _config;

        /// <summary>
        /// Unsubscribes from events when the object is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            TabletopGameEvents.OnLootBoxPickedUp -= OnLootBoxPickedUp;
        }

        /// <summary>
        /// Subscribes to events when the object is initialized.
        /// </summary>
        private void Awake()
        {
            Instance = this;
            CellsTaken = new List<HexCell>();
            LootBoxesOnBoard = new List<LootBox>();
            TabletopGameEvents.OnLootBoxPickedUp += OnLootBoxPickedUp;
            _config = TabletopConfig.Get();
        }

        private static void DisplayTileEffect(LootBox box, HexCell target, Action onComplete = null) =>
            Instance.DisplayTileFX(target, box.LootType, onComplete);

        public static void DisplayVFXAndAffectPawn(Pawn pawn, LootBox box, bool decreaseTileHeight = false, int amount = 1,
            bool increaseTileHeight = false,
            bool damagePawn = false,
            bool increaseHealth = false,
            bool decreaseSurroundingTileHeight = false,
            bool increaseSurroundingTileHeight = false
            )
        {

            //no target play CancelVFX
            if (pawn == null)
            {
                Instance.RPC_DisplayCancelVFX(box);
                return;
            }

            //Kodama already have all health points play Cancel VFX and animation
            if (increaseHealth && pawn.HealthPoints == Instance.Settings.kodamaStartHealthPoints)
            {
                Instance.RPC_DisplayCancelVFX(box);
                return;
            }

            //Kodama is immune and damage loot
            if (damagePawn && pawn is Kodama)
            {
                Kodama k = (Kodama)pawn;
                if (k.IsImmune)
                {
                    Instance.RPC_DisplayCancelVFX(box);
                    return;
                }
            }

            // Display the Link VFX to target
            if (box.showLinkVfx)
                Instance.RPC_DisplayLinkVFX(box, pawn.CurrentCell.Position);

            DisplayTileEffect(
                box, pawn.CurrentCell,
                () => Instance.AffectPawn(
                    pawn, decreaseTileHeight, amount, increaseTileHeight, damagePawn, increaseHealth, decreaseSurroundingTileHeight,
                    increaseSurroundingTileHeight, AFFECT_PAWN_DELAY));
        }

        /// <summary>
        /// Handles the event when a loot box is picked up.
        /// </summary>
        /// <param name="type">The type of the loot item picked up.</param>
        private void OnLootBoxPickedUp(LootBox box, Kodama kodama)
        {
            if (LootBoxesOnBoard == null)
                return;

            // Display the Summon VFX on top of the kodama that picked up the loot
            Instance.RPC_DisplayKodamaVFX(kodama.OwnerId, box.LootType);

            Log.Info("[LOOT] OnLootBoxPickedUp type:" + box.LootType);

            switch (box.LootType)
            {
                case LootItem.Types.Health:
                    DisplayVFXAndAffectPawn(kodama, box, increaseHealth: true);
                    break;

                case LootItem.Types.Health_Mega:
                    DisplayVFXAndAffectPawn(kodama, box, increaseHealth: true, amount: Settings.kodamaStartHealthPoints);
                    break;

                case LootItem.Types.Impact:
                    DisplayVFXAndAffectPawn(GameManager.GetRandomPawn(kodama, true), box, damagePawn: true);
                    break;

                case LootItem.Types.Impact_Mega:
                    box.LootType = LootItem.Types.Impact;
                    GameManager.GetRandomPawns(3, kodama, true).ForEach(p => DisplayVFXAndAffectPawn(p, box, damagePawn: true));
                    break;

                case LootItem.Types.HeightDown:
                    DisplayVFXAndAffectPawn(
                        GameManager.GetRandomPawn(kodama, true), box, decreaseTileHeight: true, amount: Settings.lootBoxHeightDownAmount);
                    break;

                case LootItem.Types.HeightDown_Mega:
                    DisplayVFXAndAffectPawn(
                        GameManager.GetRandomPawn(kodama, true), box, decreaseSurroundingTileHeight: true,
                        amount: Settings.lootBoxHeightDownStrongAmount);
                    break;

                case LootItem.Types.HeightUp:
                    DisplayVFXAndAffectPawn(
                        GameManager.GetRandomPawn(kodama, false), box, increaseTileHeight: true, amount: Settings.lootBoxHeightUpAmount);
                    break;

                case LootItem.Types.HeightUp_Mega:
                    DisplayVFXAndAffectPawn(
                        GameManager.GetRandomPawn(kodama, false), box, increaseSurroundingTileHeight: true,
                        amount: Settings.lootBoxHeightUpStrongAmount);
                    break;
            }
        }

        /// <summary>
        /// Display a visual effect on the affected tile
        /// </summary>
        /// <param name="currentCell"></param>
        /// <param name="type"></param>
        public void DisplayTileFX(HexCell currentCell, LootItem.Types type, Action onComplete = null)
        {
            RPC_DisplayTileVFX(currentCell.Position, type);

            // here we call the AffectPawn method when the VFX is at its climax
            DelayedAction(onComplete, _config.LootBoxEffectDelays[(int)type]);
        }

        /// <summary>
        /// Network call for displaying the VFX to all clients
        /// </summary>
        /// <param name="target"></param>
        /// <param name="type"></param>
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_DisplayTileVFX(Vector3Int target, LootItem.Types type)
        {
            var li = items.Find(s => s.Type == type);
            if (li.fxPrefab != null)
            {
                var hcr = TabletopGameManager.Instance.GridRenderer.GetCell(target);

                // here we added an offset to avoid z-fighting with tiles
                var inst = Instantiate(li.fxPrefab, hcr.transform.position + new Vector3(0, 0.01f, 0), Quaternion.identity);
                Destroy(inst, 5f);
            }
        }

        /// <summary>
        /// Display a visual effect on the affected tile
        /// </summary>
        /// <param name="currentCell"></param>
        /// <param name="type"></param>
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_DisplayLinkVFX(LootBox box, Vector3Int target)
        {
            var linkFx = box.LootType.IsStrong() ? linkFxPrefabStrong : linkFxPrefab;
            var hcr = TabletopGameManager.Instance.GridRenderer.GetCell(target);
            var origin = TabletopGameManager.Instance.GridRenderer.GetCell(box.CurrentCell);

            var inst = Instantiate(linkFx, origin.transform, false);

            inst.transform.localPosition = Vector3.up * 0.1f;
            linkVfxController = inst.GetComponent<VFXController>();
            linkSpline = inst.GetComponentInChildren<HermiteSpline>();
            linkSpline.SetEndPosition(hcr.transform.position + Vector3.up * 0.1f);

            //waiting instrument animation start
            DelayedAction(() => { linkSpline.SetEnable(true); }, 1f);

            // Stop Link VFX and Drestoy it
            DelayedAction(() => { linkSpline.SetEnable(false); Destroy(inst.gameObject, 0.35f); }, _config.LootBoxEffectDelays[(int)box.LootType]);
        }

        /// <summary>
        /// Display a visual effect when a lootbox dont have any target
        /// </summary>
        /// <param name="currentCell"></param>
        /// <param name="type"></param>
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_DisplayCancelVFX(LootBox box)
        {

            Log.Info("[LOOT] Play CancelVFX lootbox :" + box.LootType);

            var cancelFx = box.LootType.IsStrong() ? cancelFxPrefabStrong : cancelFxPrefab;
            var origin = TabletopGameManager.Instance.GridRenderer.GetCell(box.CurrentCell);

            var inst = Instantiate(cancelFx, origin.transform, false);

            VFXController cancelVfxController = inst.GetComponent<VFXController>();

            //waiting instrument animation start
            DelayedAction(() => { cancelVfxController.Activate(); }, 0.5f);

            // Stop VFX and Drestoy it
            DelayedAction(() => { Destroy(inst.gameObject, 0.0f); }, _config.LootBoxEffectDelays[(int)box.LootType]);
        }

        public void DelayedAction(Action action, float delay)
        {
            if (action == null)
            {
                return;
            }

            StartCoroutine(DelayedActionCoroutine(action, delay));

            IEnumerator DelayedActionCoroutine(Action action, float delay)
            {
                yield return new WaitForSeconds(delay);

                action?.Invoke();
            }
        }

        /// <summary>
        /// Network call for displaying the VFX to all clients
        /// </summary>
        /// <param name="target"></param>
        /// <param name="type"></param>
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_DisplayKodamaVFX(int playerId, LootItem.Types lootType)
        {
            var pawn = TabletopGameManager.Instance.GetPlayerKodama(playerId);
            var hcr = TabletopGameManager.Instance.GridRenderer.GetCell(pawn.CurrentCell);
            var summonFx = lootType.IsStrong() ? kodamaSummonStrongFxPrefab : kodamaSummonFxPrefab;

            var inst = Instantiate(summonFx, hcr.transform.position, Quaternion.identity, hcr.transform);
            pawn.UseLootBox(lootType, inst);
        }

        /// <summary>
        /// Affect a pawn by a lootbox
        /// </summary>
        /// <param name="pawn"></param>
        /// <param name="decreaseTileHeight"></param>
        /// <param name="increaseTileHeight"></param>
        /// <param name="damagePawn"></param>
        /// <param name="increaseHealth"></param>
        /// <param name="decreaseSurroundingTileHeight"></param>
        /// <param name="increaseSurroundingTileHeight"></param>
        public void AffectPawn(Pawn pawn, bool decreaseTileHeight = false, int amount = 1,
            bool increaseTileHeight = false,
            bool damagePawn = false,
            bool increaseHealth = false,
            bool decreaseSurroundingTileHeight = false,
            bool increaseSurroundingTileHeight = false,
            float delay = 0f)
        {
            var gridConfig = _config.gridConfig;

            if (pawn == null)
            {
                Log.Error("[LOOT] Can't find any pawn to apply the loot to");
                return;
            }

            Action WaitDelayedAction = () => { };

            if (damagePawn)
            {
                // Decrease its health points
                WaitDelayedAction += () =>
                {
                    pawn.Damage(amount);
                    pawn.PlayStaticDamageAnim();
                };

            }
            else if (increaseHealth)
            {
                WaitDelayedAction += () =>
                {
                    pawn.GainHealth(amount);
                };
            }

            var cell = pawn.CurrentCell;

            if (increaseTileHeight)
            {
                WaitDelayedAction += () =>
                {
                    GameManager.RPC_ChangeCellHeight(cell.Position, Mathf.Min(gridConfig.maxHeight, cell.Height + amount), false, false);
                };
            }
            else if (increaseSurroundingTileHeight)
            {
                WaitDelayedAction += () =>
                {
                    GameManager.RPC_ChangeCellAndSurroundingsHeight(cell.Position, amount);
                };
            }
            else if (decreaseSurroundingTileHeight)
            {
                WaitDelayedAction += () =>
                {
                    GameManager.RPC_ChangeCellAndSurroundingsHeight(cell.Position, -amount);
                };
            }
            else if (decreaseTileHeight)
            {
                WaitDelayedAction += () =>
                {
                    GameManager.RPC_ChangeCellHeight(cell.Position, Mathf.Max(gridConfig.minHeight, cell.Height - amount), true, false);
                };
            }

            DelayedAction(WaitDelayedAction, delay);
        }

        /// <summary>
        /// Spawns a specified number of loot boxes on the board.
        /// </summary>
        /// <param name="count">The number of loot boxes to spawn.</param>
        private IEnumerator SpawnLootBoxes(int count, bool avoidKodamaSpawnCells)
        {
            for (var i = 0; i < count; i++)
            {
                // Find a random tile
                var cell = GameManager.Grid.GetRandomFreeCell(0, avoidKodamaSpawnCells);

                if (cell == null)
                    yield break;

                // Select a random loot box type
                var loot = GetRandomLootItem();
                Log.Debug("[LOOT] Spawning random loot:" + loot.Type);

                // Spawn the box
                yield return SpawnLootBox(loot, cell);
                yield return null;
            }
        }

        /// <summary>
        /// Selects a random loot item from the available items.
        /// </summary>
        /// <returns>A random loot item.</returns>
        private LootItem GetRandomLootItem()
        {
            // There is strong_chance(default 3) chance to have a normal power type than a strong one. ?
            var normalItems = items.FindAll(s => !s.isStrong);
            var strongItems = items.FindAll(s => s.isStrong);

            var chances = 1f / (1f + TabletopGameManager.Instance.Settings.lootBoxNormalStrongChances);
            var strongItem = UnityEngine.Random.value <= chances;
            if (strongItem)
                return strongItems[UnityEngine.Random.Range(0, strongItems.Count)];

            return normalItems[UnityEngine.Random.Range(0, normalItems.Count)];
        }

        /// <summary>
        /// Spawns a loot box at a specified cell.
        /// </summary>
        /// <param name="loot">The loot item to spawn.</param>
        /// <param name="cell">The cell where the loot item will be spawned.</param>
        private IEnumerator SpawnLootBox(LootItem loot, HexCell cell)
        {
            var rd = TabletopGameManager.Instance.GridRenderer.GetCell(cell);

            var asyncOp = Runner.SpawnAsync(
                loot.prefab,
                rd.transform.position,
                Quaternion.identity,
                Runner.LocalPlayer, (runner, obj) =>
                {
                    var p = obj.GetComponent<LootBox>();
                    p.OwnerId = Runner.LocalPlayer.PlayerId;
                    p.LootType = loot.Type;
                    p.InitalizePosition(cell.Position);
                });
            yield return new WaitUntil(() => asyncOp.IsSpawned);
        }

        /// <summary>
        /// Removes a lootbox from the current lootboxes on board
        /// </summary>
        /// <param name="p"></param>
        public void DereferenceLootBox(LootBox p)
        {
            LootBoxesOnBoard.Remove(p);
            CellsTaken.Remove(p.CurrentCell);
        }

        /// <summary>
        /// Add a lootbox to the known lootboxes list
        /// </summary>
        /// <param name="p"></param>
        /// <param name="cell"></param>
        public void ReferenceLootBox(LootBox p, HexCell cell)
        {
            Log.Debug("[LOOT] ReferenceLootBox " + p.LootType);
            if (!LootBoxesOnBoard.Contains(p))
                LootBoxesOnBoard.Add(p);
            if (!CellsTaken.Contains(cell))
                CellsTaken.Add(cell);
        }

        public void SpawnNewLootBoxes()
        {
            Log.Debug("[LOOT] New Round: SpawnNewLootBoxes HasStateAuthority:" + Object.HasStateAuthority);
            if (Object.HasStateAuthority == false)
                return;

            int minLootBoxCount = TabletopGameManager.Instance.Settings.lootboxMinCount;
            int lootBoxSpawnCountPerTurn = TabletopGameManager.Instance.Settings.lootBoxSpawnCountPerTurn;
            int startLootBoxCount = TabletopGameManager.Instance.Settings.lootboxStartCount;

            if (IsFirstTurn)
            {
                Log.Debug("[LOOT] about to spawn " + startLootBoxCount + " boxes");
                StartCoroutine(SpawnLootBoxes(startLootBoxCount, true));
            }
            else
            {
                LootBoxesOnBoard = LootBoxesOnBoard.FindAll(s => s != null && s.IsKilled == false);
                var spawnCount = LootBoxesOnBoard.Count;
                Log.Debug("[LOOT] remaining boxes:" + spawnCount);
                Log.Debug("[LOOT] minLootBoxCount:" + minLootBoxCount);
                while (spawnCount < minLootBoxCount)
                {
                    spawnCount += lootBoxSpawnCountPerTurn;
                }

                spawnCount -= LootBoxesOnBoard.Count;
                Log.Debug("[LOOT] about to spawn " + spawnCount + " boxes");
                StartCoroutine(SpawnLootBoxes(spawnCount, false));
            }

            IsFirstTurn = false;
        }
    }
}