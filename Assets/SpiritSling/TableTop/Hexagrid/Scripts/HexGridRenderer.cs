// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// Hexagon grid displayed in the 3D world 
    /// </summary>
    [MetaCodeSample("SpiritSling")]
    public class HexGridRenderer : MonoBehaviour
    {
        public static Vector3Int OutOfBoardPosition = Vector3Int.one * 9999;

        [SerializeField]
        private HexCellRenderer cellPrefab;

        [SerializeField, Min(0)]
        private float hideGridAnimationEndDelay = 1;

        [SerializeField]
        private AudioClip[] tileDecreaseAudioClips;

        [SerializeField]
        private AudioClip tileDecreaseAfterShootAudioClip;

        [SerializeField]
        private AudioClip[] lavaApparitionAudioClips;

        [SerializeField]
        private AudioClip[] lavaLeavingAudioClips;

        public Random Random { get; private set; }

        public bool IsReady { get; private set; }

        private HexGrid _grid;
        private Dictionary<HexCell, HexCellRenderer> _cells = new();

        private TabletopConfig Config => TabletopGameManager.Instance.Config;

        private List<HexCell> _reusableHighlightCells = new();

        /// <summary> Initialize the board with the logical grid </summary>
        /// <param name="grid"> </param>
        /// <param name="seed"> </param>
        public void Initialize(HexGrid grid, int seed)
        {
            _grid = grid;
            Random = new Random(seed);
            IsReady = false;

            transform.localPosition = new Vector3(transform.localPosition.x, grid.Config.yShift, transform.localPosition.z);

            StartCoroutine(InitializeGridAnimated());
        }

        public void Teardown()
        {
            foreach (var cell in _cells)
            {
                cell.Value.GetComponent<PoolObject>().SendBackToPool();
            }
        }

        private IEnumerator InitializeGridAnimated()
        {
            foreach (var cell in _grid.Cells)
            {
                var r = PoolManager.Instance.GetPoolObject(cellPrefab.gameObject);
                r.transform.SetParent(transform);
                r.transform.localPosition = _grid.CellToWorld(cell.Position);
                r.SetActive(true);

                var hcr = r.GetComponent<HexCellRenderer>();

                hcr.SetCell(this, cell);
                hcr.SetState(HexCellRenderer.CellState.Unset, false);
                hcr.UpdateHeight();

                _cells.Add(cell, hcr);
                yield return new WaitForEndOfFrame();
                hcr.SetState(HexCellRenderer.CellState.Default, false);
            }

            IsReady = true;
        }

        public IEnumerator HideGridAnimation()
        {
            foreach (var cell in _cells.Keys)
            {
                cell.Height = -7;
                _cells[cell].UpdateHeight();

                yield return new WaitForEndOfFrame();
            }

            yield return new WaitForSeconds(hideGridAnimationEndDelay);
        }

        /// <summary> Request a visual update of a cell </summary>
        /// <param name="c"> Cell to Update</param>
        public void UpdateCell(HexCell c, int previousHeight, bool isShot, bool isCliffShot = false)
        {
            if (_cells.TryGetValue(c, out var r))
            {
                r.SetState(HexCellRenderer.CellState.Default);
                r.UpdateHeight();

                if (c.Height == previousHeight)
                {
                    return;
                }

                var movingUp = c.Height > previousHeight;

                if (c.Pawn != null && (!isShot || isCliffShot))
                {
                    c.Pawn.TileMoving(movingUp);
                }

                if (movingUp)
                {
                    if (previousHeight < 0 && c.Height >= 0)
                    {
                        AudioManager.Instance.PlayRandom(lavaLeavingAudioClips, AudioMixerGroups.SFX_Tiles, r.transform);
                        if (c.LootBox != null)
                        {
                            c.LootBox.SetWarning(false);
                        }
                    }
                }
                else
                {
                    // If the tile falls into water
                    if (previousHeight >= 0 && c.Height < 0)
                    {
                        AudioManager.Instance.PlayRandom(lavaApparitionAudioClips, AudioMixerGroups.SFX_TilesDecrease, r.transform);
                        if (c.LootBox != null)
                        {
                            c.LootBox.SetWarning(true);
                        }
                    }

                    if (isShot)
                    {
                        AudioManager.Instance.Play(tileDecreaseAfterShootAudioClip, AudioMixerGroups.SFX_Tiles, r.transform);
                    }
                    else
                    {
                        AudioManager.Instance.PlayRandom(tileDecreaseAudioClips, AudioMixerGroups.SFX_TilesDecrease, r.transform);
                    }
                }
            }
        }

        /// <summary>
        /// Return a cell renderer from a cell data
        /// </summary>
        /// <param name="c">the cell</param>
        public HexCellRenderer GetCell(HexCell c)
        {
            if (c == null)
            {
                Log.Error("Null cell provided");
                return null;
            }

            if (_cells.TryGetValue(c, out var r))
            {
                return r;
            }
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Log.Error($"Could not find cell renderer for cell {c.Position} _cells.Count:" + _cells.Count);
            foreach (var p in _cells)
            {
                Log.Debug(" Pair cell:" + p.Key.Position);
            }
#endif
            return null;
        }

        /// <summary> Return a cell renderer from a cell data </summary>
        public HexCellRenderer GetCell(Vector3Int p)
        {
            var c = _grid.Get(p);
            return GetCell(c);
        }

        public HexCell FindClosestCell(Vector3 currentDragPosition, float distanceBetweenCells, bool allowBelow = false)
        {
            // Find closest cell (in the whole grid)
            var minDistanceXZ = float.MaxValue;
            HexCell closestCell = null;
            var closestCellWorldPosition = Vector3.zero;

            using (var enumerator = (_grid.Cells).GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    var cell = enumerator.Current;
                    var cellWorldPosition = GetCell(cell).transform.position;
                    var distanceXZ = MathUtils.DistanceXZPlane(currentDragPosition, cellWorldPosition);

                    if (distanceXZ < minDistanceXZ)
                    {
                        minDistanceXZ = distanceXZ;
                        closestCell = cell;
                        closestCellWorldPosition = cellWorldPosition;
                    }
                }
            }

            // Deselect cell if too far
            if (closestCell != null && minDistanceXZ > distanceBetweenCells)
            {
                closestCell = null;
            }

            // Deselect if dragged object is below the target cell
            if (!allowBelow)
            {
                // Only in real build
#if UNITY_ANDROID && !UNITY_EDITOR
                var distanceToUnsnap = Config.gridConfig.cellSize;
                if (currentDragPosition.y < closestCellWorldPosition.y - distanceToUnsnap)
                {
                    closestCell = null;
                }
#endif                
            }

            return closestCell;
        }

        /// <summary>
        /// Highlight tiles that can be shot around the slingshot
        /// </summary>
        /// <param name="startCell"></param>
        /// <param name="currentShotPosition"></param>
        /// <param name="range"></param>
        /// <param name="cellState"></param>
        public HexCell HighlightShootableCells(HexCell startCell, HexCell targetCell, int range,
            HexCellRenderer.CellState cellState, bool hitCliff)
        {
            //Debug.Log("HighlightShootableCells c:" + c.Position + " range:" + range + " cellState:" + cellState);
            ClearHighlights();

            _grid.FillRangeRadius(_reusableHighlightCells, startCell.Position, range);

            for (var index = 0; index < _reusableHighlightCells.Count; index++)
            {
                var cell = _reusableHighlightCells[index];

                _cells[cell].SetState(/*index == 0 ? HexCellRenderer.CellState.Selected :*/ cellState);
            }

            if (targetCell != null && _reusableHighlightCells.Contains(targetCell) == false)
            {
                targetCell = null;
            }

            // Select cell 
            if (targetCell != null)
            {
                var r = GetCell(targetCell);

                r.SetState(hitCliff ? HexCellRenderer.CellState.CliffSelected : HexCellRenderer.CellState.Selected);
            }

            return targetCell;
        }

        /// <summary>
        /// Highlight a cell to indicate specific ations
        /// </summary>
        /// <param name="centerCell">The cell at the center of the action range</param>
        /// <param name="currentCell">The current cell that will be selected too</param>
        /// <param name="range"></param>
        /// <param name="canDropInLava"></param>
        /// <param name="currentDragPosition"></param>
        /// <param name="distanceBetwwenCells"></param>
        /// <param name="cellState"></param>
        /// <returns></returns>
        public HexCell HighlightDroppableCells(HexCell centerCell, HexCell closestCell, int range, bool canDropInLava, HexCellRenderer.CellState cellState)
        {
            ClearHighlights();

            _grid.FillRangeRadius(_reusableHighlightCells, centerCell.Position, range, true, cell =>
                {
                    // if cell is lava/water we can't drop on it
                    if (!canDropInLava && cell.Height < 0)
                        return false;

                    return cell != centerCell
                           && cell.IsOccupiedByPawn == false;
                });

            // Set valid cells state
            for (var i = 0; i < _reusableHighlightCells.Count; i++)
            {
                var cell = _reusableHighlightCells[i];
                _cells[cell].SetState(/*i == 0 ? HexCellRenderer.CellState.Selected :*/ cellState);
            }

            // Closest is valid?
            if (_reusableHighlightCells.Contains(closestCell) == false)
            {
                closestCell = null;
            }

            // Select cell 
            if (closestCell != null)
            {
                GetCell(closestCell).SetState(HexCellRenderer.CellState.Selected);
            }

            return closestCell;
        }

        /// <summary>
        /// Clear the selection state on all cells
        /// </summary>
        public void ClearHighlights()
        {
            using (var enumerator = (_cells.Values).GetEnumerator())
            {
                while (enumerator.MoveNext())
                {
                    enumerator.Current.SetState(HexCellRenderer.CellState.Default);
                }
            }
        }

        public Vector3 GetWorldPosition(HexCell c)
        {
            return GetCell(c).transform.position;
        }
    }
}
