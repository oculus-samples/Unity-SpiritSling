// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// "AI" used in training mode. This class manages the bot's planning.
    /// </summary>
    [RequireComponent(typeof(TabletopAiInteractions))]
    public class TabletopAiPlayer : BaseTabletopPlayer
    {
        private struct ShootSettings
        {
            public Slingshot shooter;
            public Transform target;
            public int targetHeight;
            public Vector3 ballPosition;

            public ShootSettings(Slingshot shooter, Transform target, int targetHeight, Vector3 ballPosition)
            {
                this.shooter = shooter;
                this.target = target;
                this.targetHeight = targetHeight;
                this.ballPosition = ballPosition;
            }
        }

        #region Fields and properties

        private const string LOG_TAG = "[AI]";
        private const string AI_MISTAKE = "[AI] Do a mistake:";
        private const int NB_SLINGSHOT_PER_PLAYER = 3;
        private const int NB_SLINGSHOT_PRIORITIES = 4;
        private const int MIN_CELL_HEIGHT = -1;

        [Header("AI")]
        [SerializeField]
        private TabletopAiPlayerConfig aiConfig;

        public TabletopAiPlayerConfig AiConfig => aiConfig;

        public override bool IsHuman => false;

        public override int PlayerId => FakePlayerId;

        public int FakePlayerId { private get; set; }

        private TabletopGameSettings gameSettings;

        public TabletopGameSettings GameSettings { get; set; }

        private TabletopAiInteractions aiInteractions;

        private TableTopPhase currentPhase;

        private Coroutine currentPhaseCoroutine;

        private float currentPhaseStartTime;

        private WaitForSeconds waitBeforeSkipPhase;
        private WaitForSeconds waitWhenNewPhase;

        /// <summary>
        /// A list which can be reused across methods to fill it with HexCell.
        /// </summary>
        private List<HexCell> reusableCells = new();

        /// <summary>
        /// A list which can be reused across methods to fill it with HexCell.
        /// </summary>
        private List<HexCell> reusableCells2 = new();

        /// <summary>
        /// Where the bot's kodama can move. If it has already moved this turn, this list contains only the current kodama's cell.
        /// </summary>
        private List<HexCell> kodamaMoveTiles = new();

        /// <summary>
        /// Where the kodama will move.
        /// </summary>
        private HexCell kodamaMoveDestination;

        /// <summary>
        /// The key is where a bot's slingshot can be moved or summoned and the value is all the cells from which the kodama can summon or move the slingshot.
        /// </summary>
        private Dictionary<HexCell, List<HexCell>> slingshotMoveTiles;

        /// <summary>
        /// Each index in the array correspond to a priority level. Lower index means higher priority.
        /// </summary>
        private List<Slingshot>[] slingshotsPerPriority;

        private Slingshot slingshotToSummon;

        private HexCell cellWhereSummon;

        private bool isSummonPlanned;

        private Vector3Wrapper shootPosition = new();

        private List<ShootSettings> shootableKodamas = new();

        private List<ShootSettings> shootableSlingshots = new();

        private List<ShootSettings> shootableCellsWithoutPawn = new();

        #endregion

        #region Events

        private void Awake()
        {
            waitBeforeSkipPhase = new WaitForSeconds(aiConfig.DelayBeforeSkipPhaseSeconds);
            waitWhenNewPhase = new WaitForSeconds(aiConfig.NewPhaseDelaySeconds);
        }

        public override void Spawned()
        {
            base.Spawned();
            aiInteractions = GetComponent<TabletopAiInteractions>();
            TabletopGameEvents.OnGamePhaseChanged += OnGamePhaseChanged;
            IsReady = true;
            HasStartCutsceneCompleted = true;
            IsGameInitialized = true;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            TabletopGameEvents.OnGamePhaseChanged -= OnGamePhaseChanged;
        }

        public void Initialize()
        {
            var hexGrid = TabletopGameManager.Instance.Grid;

            slingshotMoveTiles = new();
            foreach (var cell in hexGrid.Cells)
            {
                var kodamaPossibleCellsCount = hexGrid.CountRangeRadius(cell.Position, GameSettings.kodamaSummonRange, false);
                slingshotMoveTiles.Add(cell, new List<HexCell>(kodamaPossibleCellsCount));
            }

            slingshotsPerPriority = new List<Slingshot>[NB_SLINGSHOT_PRIORITIES];
            for (var i = 0; i < NB_SLINGSHOT_PRIORITIES; i++)
            {
                slingshotsPerPriority[i] = new List<Slingshot>(NB_SLINGSHOT_PER_PLAYER);
            }

            aiInteractions.Initialize(AiConfig, Slingshots);
        }

        private void OnGamePhaseChanged(BaseTabletopPlayer player, TableTopPhase phase)
        {
            if (player.Index != Index)
            {
                return;
            }

            if (currentPhaseCoroutine != null)
            {
                Log.Error($"{LOG_TAG} {currentPhase} routine is still running when changing to {phase} phase, stopping it");
                StopCoroutine(currentPhaseCoroutine);
                currentPhaseCoroutine = null;
            }

            if (phase is not (TableTopPhase.Move or TableTopPhase.Summon or TableTopPhase.Shoot))
            {
                return;
            }

            StartCoroutine(NewPhase(phase));
        }

        #endregion

        #region Utils

        private IEnumerator NewPhase(TableTopPhase phase)
        {
            yield return waitWhenNewPhase;

            currentPhase = phase;
            currentPhaseStartTime = Time.time;

            var phaseCoroutine = phase switch
            {
                TableTopPhase.Move => NewTurn(),
                TableTopPhase.Summon => PlanAttack(),
                TableTopPhase.Shoot => Shoot(),
                _ => throw new ArgumentOutOfRangeException($"{LOG_TAG} {phase} is invalid")
            };
            currentPhaseCoroutine = StartCoroutine(phaseCoroutine);
        }

        private void EndPhase()
        {
            currentPhaseCoroutine = null;
            var phaseDuration = Time.time - currentPhaseStartTime;
            Log.Info($"{LOG_TAG} {currentPhase} phase took {phaseDuration}s");
        }

        private IEnumerator SkipPhase()
        {
            yield return waitBeforeSkipPhase;
            TabletopGameManager.Instance.SkipPhase();
        }

        /// <summary>
        /// Return the highest tile height from the list.
        /// </summary>
        private int GetHighestHeight(List<HexCell> cells)
        {
            var highestHeight = -1;
            foreach (var cell in cells)
            {
                if (cell.Height > highestHeight)
                {
                    highestHeight = cell.Height;
                }
            }
            return highestHeight;
        }

        /// <summary>
        /// Return a cell from the list depending on the presence of loot boxes and the heights.
        /// </summary>
        /// <param name="cells">musn't be empty or null, modified by this method.</param>
        private HexCell SelectCell(List<HexCell> cells)
        {
            var nbCellWithLootbox = 0;
            foreach (var cell in cells)
            {
                if (cell.HasLootBox)
                {
                    nbCellWithLootbox++;
                }
            }

            if (nbCellWithLootbox > 0 && nbCellWithLootbox < cells.Count)
            {
                _ = cells.RemoveAll(cell => cell.HasLootBox);
            }

            var highestHeight = GetHighestHeight(cells);
            _ = cells.RemoveAll(cell => cell.Height < highestHeight);

            return cells[Random.Range(0, cells.Count)];
        }

        /// <summary>
        /// True if at least one opponent of the given type is in the given cell's shoot range.
        /// </summary>
        private bool HasOpponentPawnInShootRange(HexCell shooterCell, Type pawnType)
        {
            var shootRange = TabletopConfig.Get().GetSlingShotSettingsForHeight(shooterCell.Height).range;
            var nbOpponentInRange = TabletopGameManager.Instance.Grid.CountRangeRadius(shooterCell.Position, shootRange, false, cell =>
            {
                return cell.IsOccupiedByPawn && cell.Pawn.OwnerId != PlayerId && cell.Pawn.GetType() == pawnType;
            });

            return nbOpponentInRange > 0;
        }

        private void FillKodamaMoveTiles()
        {
            TabletopGameManager.Instance.Grid.FillRangeRadius(kodamaMoveTiles, Kodama.Position, GameSettings.kodamaMoveRange, false, cell =>
            {
                return !cell.IsOccupiedByPawn && (cell.Height >= 0 || cell.HasLootBox);
            });
        }

        private void FillSlingshotMoveTiles()
        {
            var hexGrid = TabletopGameManager.Instance.Grid;

            foreach (var keyValuePair in slingshotMoveTiles)
            {
                keyValuePair.Value.Clear();
            }

            foreach (var kodamaMoveTile in kodamaMoveTiles)
            {
                hexGrid.FillRangeRadius(reusableCells, kodamaMoveTile.Position, GameSettings.kodamaSummonRange, false);

                foreach (var summonCell in reusableCells)
                {
                    if (summonCell.Height >= 0 && !summonCell.IsOccupiedByPawn)
                    {
                        slingshotMoveTiles[summonCell].Add(kodamaMoveTile);
                    }
                }
            }
        }

        private void FillSlingshotsPerPriority()
        {
            for (var i = 0; i < NB_SLINGSHOT_PRIORITIES; i++)
            {
                slingshotsPerPriority[i].Clear();
            }

            foreach (var slingshot in Slingshots)
            {
                if (!slingshot.IsOnGrid)
                {
                    slingshotsPerPriority[0].Add(slingshot);
                    continue;
                }

                // Check if the slingshot have at least one opponent kodama and / or one opponent slingshot in range
                if (HasOpponentPawnInShootRange(slingshot.CurrentCell, typeof(Kodama)))
                {
                    slingshotsPerPriority[3].Add(slingshot);
                }
                else
                {
                    var hasSlingshotInRange = HasOpponentPawnInShootRange(slingshot.CurrentCell, typeof(Slingshot));
                    slingshotsPerPriority[hasSlingshotInRange ? 2 : 1].Add(slingshot);
                }
            }
        }

        #endregion

        #region AI logic

        private IEnumerator NewTurn()
        {
            FillKodamaMoveTiles();
            if (Random.value < AiConfig.SkipLootboxProbability)
            {
                Log.Debug($"{AI_MISTAKE} skip loot box checks");
                yield return PlanAttack();
            }
            else
            {
                yield return MoveToLootbox();
            }
        }

        /// <summary>
        /// Move to a lootbox if there is one close enough and if this is revelant.
        /// </summary>
        private IEnumerator MoveToLootbox()
        {
            //find lootboxes on kodamamovetiles
            var tilesWithALootbox = kodamaMoveTiles.Where(t => t.HasLootBox).ToList();
            if (tilesWithALootbox.Any())
            {
                var kodamaHp = Kodama.HealthPoints;
                var isFullHp = kodamaHp == TabletopGameManager.Instance.Settings.kodamaStartHealthPoints;

                for (var l = 0; l < tilesWithALootbox.Count(); ++l)
                {
                    var tile = tilesWithALootbox.ElementAt(l);
                    var type = tile.LootBox.LootType;
                    if (type is LootItem.Types.Health or LootItem.Types.Health_Mega)
                    {
                        if (!isFullHp)
                        {
                            kodamaMoveDestination = tile;
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    if (tile.Height < 0)
                    {
                        var enemyHp = TabletopPlayers.First(p => p.IsHuman).Kodama.HealthPoints;
                        if (kodamaHp > enemyHp)
                        {
                            kodamaMoveDestination = tile;
                            break;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    kodamaMoveDestination = tile;
                    break;
                }

            }


            if (kodamaMoveDestination == null)
            {
                yield return PlanAttack();
            }
            else
            {
                yield return aiInteractions.MoveKodama(Kodama, kodamaMoveDestination);

                // The kodama can't move anymore this turn, so put only its new tile in kodamaMoveTiles
                kodamaMoveTiles.Clear();
                kodamaMoveTiles.Add(kodamaMoveDestination);

                kodamaMoveDestination = null;
                EndPhase();
            }
        }

        private IEnumerator PlanAttack()
        {
            if (!isSummonPlanned)
            {
                FillSlingshotMoveTiles();
                FillSlingshotsPerPriority();

                if (Random.value < AiConfig.SkipAttackProbability)
                {
                    Log.Debug($"{AI_MISTAKE} skip attack");
                }
                else
                {
                    // First, try to target kodamas
                    PlanSummon(true);
                    // If no kodama can be targeted, try to target slingshots
                    if (slingshotToSummon == null)
                    {
                        PlanSummon(false);
                    }
                }

                // If we can't target kodama, nor slingshot, play defensive
                if (slingshotToSummon == null)
                {
                    if (Random.value >= AiConfig.SkipDefenseProbability)
                    {
                        Log.Debug($"{AI_MISTAKE} skip defense");
                        if (kodamaMoveTiles.Count > 0)
                        {
                            kodamaMoveDestination = kodamaMoveTiles[Random.Range(0, kodamaMoveTiles.Count)];
                        }

                        var kodamaSummonCell = kodamaMoveDestination ?? Kodama.CurrentCell;
                        TabletopGameManager.Instance.Grid.FillRangeRadius(reusableCells, kodamaSummonCell.Position, GameSettings.kodamaSummonRange, false, cell =>
                        {
                            return (!cell.IsOccupiedByPawn || cell.Pawn == Kodama) && cell.Height >= 0;
                        });

                        if (reusableCells.Count > 0)
                        {
                            cellWhereSummon = reusableCells[Random.Range(0, reusableCells.Count)];
                            slingshotToSummon = Slingshots[Random.Range(0, Slingshots.Count)];
                        }
                    }
                    else
                    {
                        PlanDefense();
                    }
                }

                // Plan kodama move if he hasn't moved this turn and if no move destination has been already set
                if (currentPhase == TableTopPhase.Move && kodamaMoveDestination == null)
                {
                    PlanAttackKodamaMove();
                }
                isSummonPlanned = true;
            }

            if (currentPhase == TableTopPhase.Move)
            {
                yield return MoveToPrepareSummon();
            }
            else if (currentPhase == TableTopPhase.Summon)
            {
                yield return SummonSlingshot();
            }
            else
            {
                Log.Error($"{LOG_TAG} {currentPhase} shouldn't be the current phase during the attack planning");
            }
        }

        private void PlanSummon(bool targetKodama)
        {
            reusableCells.Clear();

            // Fill the list with all cell where the bot can move a slingshot which will have an opponent kodama in its shoot range
            foreach (var keyValuePair in slingshotMoveTiles)
            {
                var cell = keyValuePair.Key;
                if (keyValuePair.Value.Count > 0 && HasOpponentPawnInShootRange(cell, targetKodama ? typeof(Kodama) : typeof(Slingshot)))
                {
                    reusableCells.Add(cell);
                }
            }

            if (reusableCells.Count == 0)
            {
                return;
            }

            cellWhereSummon = SelectCell(reusableCells);
            List<Slingshot> bestSummonList = null;
            var lowestPriorityToUse = targetKodama ? 3 : 2;

            if (slingshotsPerPriority[0].Count > 0)
            {
                bestSummonList = slingshotsPerPriority[0];
            }
            else if (slingshotsPerPriority[1].Count > 0)
            {
                bestSummonList = slingshotsPerPriority[1];
            }
            else if (lowestPriorityToUse > 2 && slingshotsPerPriority[2].Count > 0)
            {
                bestSummonList = slingshotsPerPriority[2];
            }

            if (bestSummonList != null)
            {
                slingshotToSummon = bestSummonList[Random.Range(0, bestSummonList.Count)];
            }
            else if (slingshotsPerPriority[lowestPriorityToUse].Count > 0)
            {
                bestSummonList = slingshotsPerPriority[lowestPriorityToUse];

                // For the lowest priority list, the choice isn't random, but base on the cells' height
                foreach (var slingshot in bestSummonList)
                {
                    if (slingshot.CurrentCell.Height < cellWhereSummon.Height)
                    {
                        slingshotToSummon = slingshot;
                        break;
                    }
                }
            }
        }

        private void PlanDefense()
        {
            if (currentPhase == TableTopPhase.Move && kodamaMoveTiles.Count > 0)
            {
                // Fill reusableCells with the tiles where the kodama can move and which aren't in the shoot range of any opponent slingshot
                reusableCells.Clear();
                reusableCells.AddRange(kodamaMoveTiles);

                foreach (var slingshot in TabletopGameManager.Instance.Slingshots)
                {
                    if (!slingshot.IsOnGrid || slingshot.OwnerId == PlayerId)
                    {
                        continue;
                    }

                    var shootRange = TabletopConfig.Get().GetSlingShotSettingsForHeight(slingshot.CurrentCell.Height).range;
                    TabletopGameManager.Instance.Grid.FillRangeRadius(reusableCells2, slingshot.Position, shootRange, false);

                    _ = reusableCells.RemoveAll(cell => reusableCells2.Contains(cell));
                }
                reusableCells2.Clear();

                // Move, even if no cells are safe
                if (reusableCells.Count == 0)
                {
                    reusableCells.AddRange(kodamaMoveTiles);
                }

                // Choose randomly where to move between the highest cells
                var highestHeight = GetHighestHeight(reusableCells);
                _ = reusableCells.RemoveAll(cell => cell.Height < highestHeight);
                kodamaMoveDestination = reusableCells[Random.Range(0, reusableCells.Count)];
            }

            var kodamaSummonCell = kodamaMoveDestination ?? Kodama.CurrentCell;
            TabletopGameManager.Instance.Grid.FillRangeRadius(reusableCells, kodamaSummonCell.Position, GameSettings.kodamaSummonRange, false, cell =>
            {
                return (!cell.IsOccupiedByPawn || cell.Pawn == Kodama) && cell.Height >= 0;
            });

            if (reusableCells.Count > 0)
            {
                // Choose where to summon
                cellWhereSummon = SelectCell(reusableCells);

                // Choose which slingshot to summon (may be none)
                var firstPrioritySlingshots = slingshotsPerPriority[0];
                if (firstPrioritySlingshots.Count > 0)
                {
                    slingshotToSummon = firstPrioritySlingshots[Random.Range(0, firstPrioritySlingshots.Count)];
                }
                else
                {
                    foreach (var slingshot in slingshotsPerPriority[1])
                    {
                        if (slingshot.CurrentCell.Height < cellWhereSummon.Height)
                        {
                            slingshotToSummon = slingshot;
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Plan a kodama move in an attack strategy.
        /// </summary>
        private void PlanAttackKodamaMove()
        {
            if (cellWhereSummon == null)
            {
                return;
            }

            // If a summoned has been planned, find where to move the kodama to perform the summon
            var possibleDestinations = slingshotMoveTiles[cellWhereSummon];
            if (possibleDestinations.Count == 0)
            {
                return;
            }

            var highestHeight = GetHighestHeight(possibleDestinations);

            // Take only the highest cells
            reusableCells.Clear();
            foreach (var cell in possibleDestinations)
            {
                if (cell.Height == highestHeight)
                {
                    reusableCells.Add(cell);
                }
            }

            // Choose a random cell from the highest ones
            kodamaMoveDestination = reusableCells[Random.Range(0, reusableCells.Count)];
        }

        /// <summary>
        /// Move the kodama to prepare to summon or move a slingshot in the next phase.
        /// </summary>
        private IEnumerator MoveToPrepareSummon()
        {
            if (kodamaMoveDestination == null)
            {
                Log.Info($"{LOG_TAG} no cell to move the kodama on");
                yield return SkipPhase();
            }
            else
            {
                yield return aiInteractions.MoveKodama(Kodama, kodamaMoveDestination);
            }
            kodamaMoveDestination = null;
            EndPhase();
        }

        /// <summary>
        /// Summon or move a slingshot if revelant.
        /// </summary>
        private IEnumerator SummonSlingshot()
        {
            if (slingshotToSummon == null || cellWhereSummon == null)
            {
                Log.Info($"{LOG_TAG} no slingshot to summon or cell to summon a slingshot on");
                yield return SkipPhase();
            }
            else
            {
                yield return aiInteractions.MoveSlingshot(slingshotToSummon, cellWhereSummon);
            }
            cellWhereSummon = null;
            slingshotToSummon = null;
            isSummonPlanned = false;
            EndPhase();
        }

        /// <summary>
        /// Shoot with a slingshot if revelant. Target opponent kodama first, then slingshots, then highest tile.
        /// </summary>
        private IEnumerator Shoot()
        {
            yield return FindShootTargets(shootableKodamas, shootableSlingshots, shootableCellsWithoutPawn);

            List<ShootSettings> listToShoot = null;
            if (shootableKodamas.Count > 0)
            {
                listToShoot = shootableKodamas;
            }
            else if (shootableSlingshots.Count > 0)
            {
                listToShoot = shootableSlingshots;
            }
            else if (shootableCellsWithoutPawn.Count > 0)
            {
                listToShoot = shootableCellsWithoutPawn;
            }

            if (listToShoot == null)
            {
                Log.Info($"{LOG_TAG} no target to shoot");
                yield return SkipPhase();
            }
            else
            {
                var chosenShootSettings = listToShoot[Random.Range(0, listToShoot.Count)];
                yield return aiInteractions.Shoot(chosenShootSettings.shooter, chosenShootSettings.target, chosenShootSettings.ballPosition);
            }

            shootableKodamas.Clear();
            shootableSlingshots.Clear();
            shootableCellsWithoutPawn.Clear();
            EndPhase();
        }

        private IEnumerator FindShootTargets(List<ShootSettings> shootableKodamas, List<ShootSettings> shootableSlingshots, List<ShootSettings> shootableCellsWithoutPawn)
        {
            foreach (var shooter in Slingshots)
            {
                if (!shooter.IsOnGrid)
                {
                    continue;
                }

                // Get the targetable cells (cells without bot's pawn on it)
                var shootRange = TabletopConfig.Get().GetSlingShotSettingsForHeight(shooter.CurrentCell.Height).range;
                TabletopGameManager.Instance.Grid.FillRangeRadius(reusableCells, shooter.Position, shootRange, false, cell =>
                {
                    return !cell.IsOccupiedByPawn || cell.Pawn.OwnerId != PlayerId;
                });

                // Remove all cells without pawn which aren't the highest ones
                var highestHeight = MIN_CELL_HEIGHT;
                foreach (var cell in reusableCells)
                {
                    if (!cell.IsOccupiedByPawn && cell.Height > highestHeight)
                    {
                        highestHeight = cell.Height;
                    }
                }
                _ = reusableCells.RemoveAll(cell => !cell.IsOccupiedByPawn && cell.Height < highestHeight);

                // Sort the cells to have kodamas first, then slingshots, then cells without pawn
                reusableCells.Sort((cell1, cell2) =>
                {
                    return GetCellPriority(cell1) - GetCellPriority(cell2);
                });
                static int GetCellPriority(HexCell cell)
                {
                    if (cell.Pawn as Kodama)
                    {
                        return 0;
                    }
                    else if (cell.Pawn as Slingshot)
                    {
                        return 1;
                    }
                    else
                    {
                        // No pawn
                        return 2;
                    }
                }

                // Find which targetable cell can really be shot by the slingshot
                foreach (var targetCell in reusableCells)
                {
                    var isTargetKodama = targetCell.Pawn as Kodama;
                    var isTargetSlingshot = targetCell.Pawn as Slingshot;

                    // If at least one kodama can be shot, ignore the rest
                    // If at least one slingshot can be shot, ignore the empty cells
                    if ((!isTargetKodama && shootableKodamas.Count > 0) || (!targetCell.IsOccupiedByPawn && shootableSlingshots.Count > 0))
                    {
                        continue;
                    }

                    var targetRenderer = TabletopGameManager.Instance.GridRenderer.GetCell(targetCell);
                    yield return aiInteractions.FindShootBallPosition(shooter, targetRenderer, !targetCell.IsOccupiedByPawn, shootPosition);

                    // If the target can't be shot
                    if (shootPosition.Value == Vector3.zero)
                    {
                        continue;
                    }

                    var shootSettings = new ShootSettings(shooter, targetRenderer.transform, targetCell.Height, shootPosition.Value);
                    if (isTargetKodama)
                    {
                        shootableKodamas.Add(shootSettings);
                        // We can shoot a kodama, so no need to look for the rest anymore
                        shootableSlingshots.Clear();
                        shootableCellsWithoutPawn.Clear();
                        break;
                    }
                    else if (isTargetSlingshot)
                    {
                        shootableSlingshots.Add(shootSettings);
                        // We can shoot a slingshot, so no need to look for empty cells anymore
                        shootableCellsWithoutPawn.Clear();
                    }
                    else
                    {
                        shootableCellsWithoutPawn.Add(shootSettings);
                    }
                }
            }
        }

        #endregion
    }
}