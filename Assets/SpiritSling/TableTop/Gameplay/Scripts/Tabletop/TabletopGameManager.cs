// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Fusion;
using UnityEngine;
using Random = System.Random;

namespace SpiritSling.TableTop
{
    public enum TableTopPhase
    {
        Setup = 0,
        Move = 1,
        Summon = 2,
        Shoot = 3,
        EndPhase = 4,
        EndTurn = 5,
        Victory = 6
    }

    /// <summary>
    /// Tabletop gameplay main class
    /// </summary>
    public class TabletopGameManager : NetworkBehaviour
    {
        // Singleton for the main manager
        private static TabletopGameManager _instance;

        public static TabletopGameManager Instance
        {
            get
            {
                if (_instance == null)
                    return null;
                if (!_instance._isSpawned)
                    return null;

                return _instance;
            }
        }

        private bool _clocCalled;

        [Header("Bindings")]
        [SerializeField]
        private PlayerBoardObjects playerBoardObjects;

        [SerializeField]
        private HexGridRenderer hexGridRenderer;

        [Header("Audio")]
        [SerializeField]
        private AudioClip[] shootTileAudioClips;

        // Networked fields
        // ------------------------------------------------

        [Networked]
        public ushort Round { get; set; }

        [Networked]
        public byte ClocRounds { get; set; }

        [Networked, OnChangedRender(nameof(OnPhaseChanged))]
        public byte Phase { get; set; }

        [Networked]
        public byte ClocRadius { get; set; }

        [Networked]
        public byte CurrentPlayerIndex { get; set; }

        [Networked]
        public float Timer { get; set; }

        // ------------------------------------------------

        private bool _isSpawned;

        private HexGrid _grid;
        private Random random;

        private List<Kodama> kodamas = new();
        private List<Slingshot> slingshots = new();
        private List<Playerboard> boards = new();

        private List<HexCell> clocRandomCells = new();
        private List<HexCell> reusableRespawnCells = new();
        private List<HexCell> reusableSurroundingCells = new();

        #region Initialization

        /// <summary>
        /// This is the entry point of the game. Initialization starts when TableTop has spawned properly
        /// </summary>
        public override void Spawned()
        {
            base.Spawned();

            Settings = Config.defaultGameSettings;

            var settingsHolder = FindAnyObjectByType<TabletopGameSettingsHolder>();
            if (settingsHolder)
            {
                Settings = settingsHolder.GameSettings;
            }
            else
            {
                Log.Warning("Missing networked Game Settings! Using default values.");
            }

            random = new Random(Settings.seed);
            _isSpawned = true;

            TabletopGameEvents.OnGameOver += OnGameOver;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);
            TabletopGameEvents.OnGameOver -= OnGameOver;
        }

        private void Awake()
        {
            _instance = this;
        }

        private void OnDestroy()
        {
            hexGridRenderer.Teardown();
            _instance = null;
        }

        private void OnGameOver(BaseTabletopPlayer _)
        {
            var winner = GetWinner();
            if (winner != null)
            {
                winner.Victory();
                TabletopGameEvents.OnGameOver -= OnGameOver;
            }
        }

        /// <summary>
        /// Returns the winner player if there is one.
        /// </summary>
        private BaseTabletopPlayer GetWinner()
        {
            // Verify the max number of players in case of a solo game without bots
            if (Runner.SessionInfo.MaxPlayers <= 1 && ConnectionManager.Instance.BotCount == 0)
            {
                return null;
            }

            BaseTabletopPlayer winner = null;
            foreach (var player in BaseTabletopPlayer.TabletopPlayers)
            {
                if (player.IsWinner)
                {
                    return player;
                }
                if (player.IsGameOver)
                {
                    continue;
                }

                if (winner == null)
                {
                    winner = player;
                }
                else
                {
                    // There is more than one player alive
                    return null;
                }
            }

            return winner;
        }

        public bool HasSomeoneWon()
        {
            return GetWinner() != null;
        }

        public void InitializeGrid(int playerCount)
        {
            // Create & display the grid
            var seed = Settings.seed;
            Log.Info($"Creating hexgrid with seed {seed}");
            _grid = new HexGrid(random, Config.gridConfig);

            // Ensures kodama spawn cells aren't underwater
            foreach (var spawnCell in _grid.FindFurthestPoints(playerCount))
            {
                while (spawnCell.Height < 0)
                {
                    spawnCell.Height = _grid.GetRandomCellHeight();
                }
            }

            hexGridRenderer.Initialize(_grid, seed);

            TabletopGameEvents.GameStart?.Invoke();
        }

        public Kodama GetPlayerKodama(int playerId)
        {
            return kodamas.Find(s => s.OwnerId == playerId);
        }

        public List<Kodama> GetOpponentKodamas(int playerId)
        {
            return kodamas.FindAll(s => s.OwnerId != playerId);
        }

        /// <summary>
        /// Get a random pawn on the board.
        /// </summary>
        /// <param name="kodama"></param>
        /// <param name="opponent">true if we return an opponent to the given kodama. False for a pawn in the same team (it may be the kodama itself).</param>
        /// <returns></returns>
        public Pawn GetRandomPawn(Kodama kodama, bool opponent)
        {
            var targetableSlingshots = slingshots.FindAll(s => (opponent == (s.OwnerId != kodama.OwnerId)) && !s.IsKilled && s.IsOnGrid);

            // Equal chances to select a kodama or a sling
            var getaKodama = targetableSlingshots.Count == 0 || UnityEngine.Random.value >= .5f;

            var pawns = new List<Pawn>();
            if (getaKodama)
            {
                pawns.AddRange(kodamas.FindAll(s => (opponent == (s != kodama)) && !s.IsKilled));
            }
            else
            {
                pawns.AddRange(targetableSlingshots);
            }

            if (pawns.Count == 0)
            {
                return null;
            }

            // Random object
            var rnd = new Random();
            var index = rnd.Next(pawns.Count);
            return pawns[index];
        }

        public List<Pawn> GetRandomPawns(int count, Kodama kodama, bool opponent)
        {
            var pawns = new List<Pawn>();
            var maxIter = 20;
            var iter = 0;

            while (pawns.Count < count)
            {
                var p = GetRandomPawn(kodama, opponent);
                if (p != null && !pawns.Contains(p))
                {
                    pawns.Add(p);
                }

                if (++iter >= maxIter)
                    break;
            }

            return pawns;
        }

        #endregion

        #region Gameplay specific functions

        /// <summary>
        /// Get a safe respawn cell position for local player kodama. If none are found, returns a random cell.
        /// </summary>
        /// <param name="kodama">The Kodama looking for a cell</param>
        /// <returns></returns>
        public HexCell GetSafeRespawnCell(Kodama kodama)
        {
            // List tiles around opponent kodamas
            // to avoid respawning next to them
            var opponentKodamas = GetOpponentKodamas(kodama.OwnerId);
            var unsafeCells = new List<HexCell>();

            var radius = TabletopConfig.Get().kodamaUnsafeRange;

            for (var i = 0; i < opponentKodamas.Count; i++)
            {
                var c = opponentKodamas[i].CurrentCell;
                if (c != null)
                {
                    Grid.FillRangeRadius(reusableRespawnCells, c.Position, radius);
                    unsafeCells.AddRange(reusableRespawnCells);
                }
            }

            HexCell cell;

            var freeCells = _grid.Cells.Where(
                c => !c.IsOccupiedByPawn &&
                     !c.WillBeOccupiedByPawn &&
                     c.LootBox == null &&
                     c.Height >= 0 &&
                     !unsafeCells.Contains(c)).ToList();

            // Look if we have any free tile available.
            if (freeCells.Count > 0)
            {
                // If yes, randomly pick one (weight by height)
                var weightedList = freeCells.ToDictionary(c => c, c => c.Height + 1);
                cell = GetWeightedRandomCell(weightedList);
            }
            else
            {
                // If no, do we have any slingshot on the board that aren't going underwater?
                var kodamaOwner = kodama.Owner;
                var slingShotsOnBoard = kodamaOwner.Slingshots.Where(s => s.CurrentCell != null && s.CurrentCell.Height >= 0).ToList();

                if (slingShotsOnBoard.Count > 0)
                {
                    // Yes: remove the slingshot and place the player on the cell
                    var slingShot = slingShotsOnBoard[UnityEngine.Random.Range(0, slingShotsOnBoard.Count)];
                    cell = slingShot.CurrentCell;
                    slingShot.SendBackToBoard();
                }
                else
                {
                    // No: Remove immunity and respawn anywhere in lava
                    kodama.ResetImmunity();
                    cell = _grid.GetRandomFreeCell(int.MinValue);
                }
            }

            // Fallback
            if (cell == null)
            {
                Log.Error("Safe respawn position not found!");
                cell = _grid.GetRandomFreeCell(int.MinValue);
            }

            return cell;
        }

        public static HexCell GetWeightedRandomCell(Dictionary<HexCell, int> weightedList)
        {
            var totalWeight = weightedList.Values.Sum();
            var randomWeight = UnityEngine.Random.Range(1, totalWeight + 1);

            // /!\ Not using seeded random here on purpose

            foreach (var weightedCell in weightedList)
            {
                randomWeight -= weightedCell.Value;
                if (randomWeight <= weightedCell.Value)
                {
                    return weightedCell.Key;
                }
            }

            return null;
        }

        private void OnPhaseChanged()
        {
            if (BaseTabletopPlayer.GetByPlayerIndex(CurrentPlayerIndex) != null)
            {
                TabletopGameEvents.OnGamePhaseChanged?.Invoke(CurrentPlayer, (TableTopPhase)Phase);
            }
        }

        #endregion

        #region RPCs & Networked functions

        public void SetNextPlayer()
        {
            if (Object.HasStateAuthority == false)
            {
                Log.Error("Cannot update BoardStateData without state authority");
            }

            if (BaseTabletopPlayer.TabletopPlayers.Count == 0)
            {
                Log.Error("No more players in the game");
                return;
            }

            // Player + 1
            var currentIndex = CurrentPlayerIndex;

            // Loop through the list to find the next index
            var isNewIndexValid = false;
            foreach (var player in BaseTabletopPlayer.TabletopPlayers)
            {
                if (player.Index > currentIndex)
                {
                    CurrentPlayerIndex = (byte)player.Index;
                    isNewIndexValid = true;
                    break;
                }
            }

            if (isNewIndexValid == false)
            {
                // Loop back to first player
                CurrentPlayerIndex = (byte)BaseTabletopPlayer.FirstPlayer.Index;
            }

            Log.Info($"Next player {CurrentPlayerIndex}");

            Phase = 4;
        }

        /// <summary>
        /// Change current player
        /// </summary>
        /// <param name="nextPlayerIndex"></param>
        /// <param name="forceNoNewRound">true if the next turn call shouldn't trigger a new round</param>
        [Rpc(RpcSources.All, RpcTargets.All)]
        public void RPC_NextTurn(byte nextPlayerIndex, bool forceNoNewRound = false)
        {
            TabletopGameEvents.OnNextTurnCalled?.Invoke(nextPlayerIndex, forceNoNewRound);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_ApplyCLOC(int clocRadius, bool randomSelection)
        {
            Log.Info($"CLOC - Radius={clocRadius}");
            List<HexCell> clocCells = null;
            List<HexCell> nextCells = null;

            if (!_clocCalled)
            {
                _clocCalled = true;
                TabletopGameEvents.GameClocStart?.Invoke();
            }

            if (randomSelection == false)
            {
                // Apply -1 height on every cell >= CLOC radius
                clocCells = new List<HexCell>();
                for (var r = _grid.Radius; r >= clocRadius; r--)
                {
                    clocCells.AddRange(_grid.GetRingCells(r, 0));
                }

                // Set bool on cells affected next round
                if (clocRadius > 0)
                {
                    nextCells = _grid.GetRingCells(clocRadius - 1, 0);
                }
            }
            else
            {
                // Decrease height of previously selected cells
                if (clocRandomCells != null && clocRandomCells.Count > 0)
                {
                    clocCells = new List<HexCell>(clocRandomCells);
                }
                else
                {
                    clocRandomCells = new List<HexCell>();
                }

                // Pick 6 new tiles that are not in the current list
                var randomCellCount = Settings.clocRandomCount * clocRadius;
                nextCells = _grid.GetRandomCells(randomCellCount, clocRandomCells);
                clocRandomCells.AddRange(nextCells);
            }

            if (clocCells != null)
            {
                foreach (var c in clocCells)
                {
                    var previousHeight = c.Height;

                    // Apply CLOC
                    if (c.Height > Config.gridConfig.minHeight)
                    {
                        c.Height--;
                    }

                    hexGridRenderer.UpdateCell(c, previousHeight, false);
                }
            }

            if (nextCells != null)
            {
                foreach (var c in nextCells)
                {
                    c.WillHaveCLOC = true;
                    hexGridRenderer.UpdateCell(c, c.Height, false);
                }
            }

            // Go through all lava tiles of the level, damage units on it
            foreach (var cell in _grid.Cells)
            {
                CheckCellForLavaOnPawns(cell);
            }
        }

        private void CheckCellForLavaOnPawns(HexCell cell)
        {
            if (cell.Height < 0 && cell.Pawn != null)
            {
                Log.Info(cell.Pawn + " is in lava");
                if (cell.Pawn.HasStateAuthority)
                {
                    cell.Pawn.DamageLava(Settings.lavaDamage);
                }
            }
        }

        /// <summary>
        /// Cell
        /// </summary>
        /// <param name="target"></param>
        /// <param name="newHeight"></param>
        /// <param name="checkLava"></param>
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_ChangeCellHeight(Vector3Int target, int newHeight, bool checkLava, bool isShot,
            bool isCliffShot = false)
        {
            Log.Info($"Cell {target} set height to {newHeight}");
            var cell = _grid.Get(target);
            var previousHeight = cell.Height;
            cell.Height = newHeight;

            hexGridRenderer.UpdateCell(cell, previousHeight, isShot, isCliffShot);
            if (checkLava) CheckCellForLavaOnPawns(cell);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_ChangeCellAndSurroundingsHeight(Vector3Int target, int amount)
        {
            Log.Info($"Cell {target} and surroundings change height by {amount}");

            var config = TabletopConfig.Get().gridConfig;

            Instance.Grid.FillRangeRadius(reusableSurroundingCells, target, 1);
            for (var i = 0; i < reusableSurroundingCells.Count; i++)
            {
                var cell = reusableSurroundingCells[i];
                var previousHeight = cell.Height;
                cell.Height = Mathf.Clamp(cell.Height + amount, config.minHeight, config.maxHeight);
                hexGridRenderer.UpdateCell(cell, previousHeight, false);
                CheckCellForLavaOnPawns(cell);
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_ClearHighlights()
        {
            GridRenderer.ClearHighlights();
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_HighlightDropKodamaChanged(Vector3Int currentCellPos, Vector3Int closestCellPos)
        {
            var currentCell = _grid.Get(currentCellPos);
            var closestCell = _grid.Get(closestCellPos);

            GridRenderer.HighlightDroppableCells(currentCell, closestCell,
                Settings.kodamaMoveRange, true, HexCellRenderer.CellState.Range);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_HighlightDropSlingshotChanged(Vector3Int currentCellPos, Vector3Int closestCellPos)
        {
            var currentCell = _grid.Get(currentCellPos);
            var closestCell = _grid.Get(closestCellPos);

            GridRenderer.HighlightDroppableCells(currentCell, closestCell,
                Settings.kodamaSummonRange, false, HexCellRenderer.CellState.Range);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_HighlightShootChanged(Vector3Int currentCellPos, Vector3Int foundCellPos, int range, byte hitCliff)
        {
            var currentCell = _grid.Get(currentCellPos);
            var foundTile = _grid.Get(foundCellPos);
            GridRenderer.HighlightShootableCells(currentCell, foundTile,
                range, HexCellRenderer.CellState.Range, hitCliff != 0);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_PlayHitTileSound(Vector3Int cellPosition)
        {
            var target = GridRenderer.GetCell(cellPosition).transform;
            AudioManager.Instance.PlayRandom(shootTileAudioClips, AudioMixerGroups.SFX_InteractionsSlings, target);
        }

        /// <summary>
        /// Change current player
        /// </summary>
        /// <param name="nextPlayerIndex"></param>
        [Rpc(RpcSources.All, RpcTargets.All)]
        public void RPC_LeaveToLobby()
        {
            Log.Debug("RPC_LeaveToLobby");
            StartCoroutine(LeaveToLobbyRoutine());
        }

        IEnumerator LeaveToLobbyRoutine()
        {
            if (!ConnectionManager.Instance.RejoinLastRoom)
            {
                ConnectionManager.Instance.RejoinLastRoom = true;

                // Delay between the RPC and destroying the runner
                yield return new WaitForSeconds(1f);

                TabletopGameEvents.OnRequestLeaveToLobby?.Invoke();
            }
        }

        #endregion

        #region Pushback

        public struct PushbackData
        {
            public Pawn pawn;
            public Vector3Int position;
            public Vector3 direction;

            public PushbackData(Pawn p, Vector3Int pos, Vector3 dir)
            {
                pawn = p;
                position = pos;
                direction = dir;
            }
        }

        /// <summary>
        /// Compute the pushback sequence to execute
        /// </summary>
        /// <param name="target"></param>
        /// <param name="source"></param>
        /// <returns></returns>
        public List<PushbackData> GetPushbackChain(Vector3Int target, Vector3Int source)
        {
            var cell = _grid.Get(target);
            List<PushbackData> pushedElements = new();

            if (target == source) return pushedElements;

            var chain = true;
            var currentPawn = cell.Pawn;

            var currentSource = source;
            var currentTarget = target;

            while (chain)
            {
                var pushbackPosition = GetPushbackDestination(currentTarget, currentSource);
                var pushbackTarget = _grid.Get(pushbackPosition);

                var direction =
                    Vector3.Normalize(_grid.CellToWorld(currentTarget) - _grid.CellToWorld(currentSource));

                if (pushbackTarget != null)
                {
                    // Register pushback
                    pushedElements.Insert(0, new(currentPawn, pushbackPosition, direction));

                    // Is there already something on the destination cell?
                    var c = GridRenderer.GetCell(pushbackTarget);
                    if (c.Cell.Pawn != null)
                    {
                        // Chain! Push it too!
                        currentSource = currentTarget;
                        currentTarget = c.Cell.Position;
                        currentPawn = c.Cell.Pawn;
                    }
                    else
                    {
                        // Nothing but grass: stop chain
                        chain = false;
                    }
                }
                else
                {
                    // This last pawn will be destroyed
                    pushedElements.Insert(0, new(currentPawn, HexGridRenderer.OutOfBoardPosition, direction));
                    chain = false;
                }
            }

            return pushedElements;
        }

        /// <summary>
        /// Get the best cell to pushback a pawn on
        /// </summary>
        /// <param name="target"></param>
        /// <param name="origin"></param>
        /// <returns></returns>
        private Vector3Int GetPushbackDestination(Vector3Int target, Vector3Int origin)
        {
            // Determine the angle of the shot 
            var angle = HexGrid.GetAngleInDegrees(target, origin);

            // Loop through neighbors. Sort them by closeness to angle
            List<Tuple<Vector3Int, float>> sortedNeighbors = new();
            foreach (var dir in HexGrid.Directions)
            {
                var n = target + dir;

                var nAngle = HexGrid.GetAngleInDegrees(n, target);
                var nAngleDiff = Mathf.Abs(angle - nAngle);

                // Sort
                var index = sortedNeighbors.Count;
                for (var i = 0; i < sortedNeighbors.Count; i++)
                {
                    if (sortedNeighbors[i].Item2 >= nAngleDiff)
                    {
                        index = i;
                        break;
                    }
                }

                sortedNeighbors.Insert(index, new Tuple<Vector3Int, float>(n, nAngleDiff));
            }

            var bestNeighbor1 = sortedNeighbors[0].Item1;
            var bestNeighbor2 = sortedNeighbors[1].Item1;
            var c1 = _grid.Get(bestNeighbor1);
            var c2 = _grid.Get(bestNeighbor2);

            // If best angle is out of bounds... fall!
            if (c1 == null) return HexGridRenderer.OutOfBoardPosition;

            // Do we have 2 possible good angles?
            const float
                ANGLE_THRESHOLD = 20; // Degrees of maximum spreading between the two best tiles from the original angle
            if (sortedNeighbors[1].Item2 > ANGLE_THRESHOLD)
            {
                // Nope: return the best one
                Log.Debug($"Pushback: only one best angle {sortedNeighbors[0].Item2} {sortedNeighbors[0].Item1}");
                return bestNeighbor1;
            }

            // Yes: find the best one
            if (c2 == null)
            {
                Log.Debug(
                    $"Pushback: only one good angle {sortedNeighbors[0].Item2} {sortedNeighbors[0].Item1}, other one is void");
                return c1.Position;
            }

            // Pick the best outcome between the two cells
            // Different height? Pick the lowest one (but not lava)
            if (c1.Height >= 0 && c1.Height < c2.Height)
            {
                Log.Debug($"Pushback: c1 lower height {sortedNeighbors[0].Item1}");
                return c1.Position;
            }

            if (c2.Height >= 0 && c2.Height < c1.Height)
            {
                Log.Debug($"Pushback: c2 lower height {sortedNeighbors[1].Item1}");
                return c2.Position;
            }

            // Same height? 50/50
            Log.Debug("Pushback: random pick");
            if (UnityEngine.Random.Range(0f, 1f) <= 0.5f) return c1.Position;

            return c2.Position;
        }

        #endregion

        #region End Game

        public void ResetCloc()
        {
            _clocCalled = false;
        }

        public void BackToMenu()
        {
            ResetCloc();
            TabletopGameEvents.OnRequestQuitGame?.Invoke();
        }

        public void SkipPhase()
        {
            if (CanSkipPhase)
                TabletopGameEvents.OnRequestSkipPhase?.Invoke();
        }

        #endregion

        #region Properties

        public bool CanSkipPhase { get; set; }

        /// <summary>
        /// Game session settings 
        /// </summary>
        public TabletopGameSettings Settings { get; set; }

        /// <summary>
        /// General game configuration
        /// </summary>
        public TabletopConfig Config => TabletopConfig.Get();

        public HexGrid Grid => _grid;

        public HexGridRenderer GridRenderer => hexGridRenderer;

        public List<Kodama> Kodamas => kodamas;
        public List<Slingshot> Slingshots => slingshots;
        public List<Playerboard> Playerboards => boards;

        public PlayerBoardObjects BoardObjects => playerBoardObjects;

        public BaseTabletopPlayer CurrentPlayer
        {
            get
            {
                var currentPlayer = BaseTabletopPlayer.GetByPlayerIndex(CurrentPlayerIndex);
                if (currentPlayer != null) return currentPlayer;

                Log.Error($"Current player index {CurrentPlayerIndex} not found");

                // Always return something, too many scripts relies on it
                return BaseTabletopPlayer.TabletopPlayers.FirstOrDefault();
            }
        }

        #endregion
    }
}