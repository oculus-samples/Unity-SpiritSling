// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Assert = UnityEngine.Assertions.Assert;
using Random = System.Random;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// Hexagonal grid implementation 
    /// </summary>
    /// <remarks>See https://www.redblobgames.com/grids/hexagons/ for documentation about Hexagrids</remarks>
    [MetaCodeSample("SpiritSling")]
    public class HexGrid
    {
        /// <summary>
        /// The 6 possible directions to navigate in the grid
        /// </summary>
        public static Vector3Int[] Directions = {
            new Vector3Int(+1, 0, -1), new Vector3Int(+1, -1, 0), new Vector3Int(0, -1, +1), new Vector3Int(-1, 0, +1), new Vector3Int(-1, +1, 0),
            new Vector3Int(0, +1, -1)
        };

        /// <summary>
        /// The 6 diagonals
        /// </summary>
        public static Vector3Int[] Diagonals = {
            new Vector3Int(+2, -1, -1), new Vector3Int(+1, -2, +1), new Vector3Int(-1, -1, +2), new Vector3Int(-2, +1, +1),
            new Vector3Int(-1, +2, -1), new Vector3Int(+1, +1, -2)
        };

        private Dictionary<Vector3Int, HexCell> _grid = new();
        private Random _rand;
        private HexGridConfig _config;

        /// <summary>
        /// Get all the cells 
        /// </summary>
        public IEnumerable<HexCell> Cells => _grid.Values;

        /// <summary>
        /// Grid radius
        /// </summary>
        public int Radius { get; }

        /// <summary>
        /// Grid configuration used on init
        /// </summary>
        public HexGridConfig Config => _config;

        #region Grid creation

        /// <summary>
        /// Initialize the grid. 
        /// </summary>
        /// <param name="random"></param>
        /// <param name="config"></param>
        public HexGrid(Random random, HexGridConfig config)
        {
            _config = config;
            Radius = _config.radius;
            _rand = random;

            // Create all cells
            for (var q = -Radius; q <= Radius; q++)
            {
                for (var r = -Radius; r <= Radius; r++)
                {
                    for (var s = -Radius; s <= Radius; s++)
                    {
                        if (q + r + s == 0)
                        {
                            var cell = new HexCell(q, r, s, 0);
                            _grid.Add(cell.Position, cell);
                        }
                    }
                }
            }

            // Now apply the random height selection
            // 1/ Pick 6 parts
            var parts = GetRandomBoardParts();
            Assert.AreEqual(parts.Length, 6, "Need 6 slices to create the board!");

            // 2/ Eventually rotate them and get a copy of the tile elements
            var listTiles = new TileElement[parts.Length][];
            for (var i = 0; i < parts.Length; i++)
            {
                listTiles[i] = GetTilesElements(parts[i]);
            }

            // 3/ Get height for each cell and use it in our grid
            var angle = 0;
            var sliceCenter = new Vector3Int(-2, 3, -1);
            for (var i = 0; i < listTiles.Length; i++)
            {
                foreach (var tileElement in listTiles[i])
                {
                    var p = sliceCenter + tileElement.Position;

                    // Rotate 60° by 60°
                    for (var j = 0; j < angle; j++)
                    {
                        p = RotatePosition60(p);
                    }

                    var height = GetRandomCellHeight();

                    Assert.IsTrue(
                        _grid.ContainsKey(p),
                        $"Invalid grid position {p} (sliceCenter={sliceCenter} tile pos={tileElement.Position} angle={angle}");
                    _grid[p].Height = height;
                }

                angle += 1; // slice by slice
            }

            // Central tile
            _grid[Vector3Int.zero].Height = GetMiddleCellHeight();
        }

        /// <summary>
        /// Get the 6 random parts of the board
        /// </summary>
        /// <returns></returns>
        private BoardPart[] GetRandomBoardParts()
        {
            var parts = new BoardPart[6];

            var weightedParts = new List<BoardPart>();
            foreach (var bp in _config.parts)
            {
                if (bp.weight != 0)
                {
                    for (var i = 0; i < bp.weight; i++)
                    {
                        weightedParts.Add(bp);
                    }
                }
            }

            for (var i = 0; i < parts.Length; i++)
            {
                var rng = _rand.Next(0, weightedParts.Count);

                // Log.Debug($"> Selected board part {i}: {weightedParts[rng].partName}");
                parts[i] = weightedParts[rng];
            }

            return parts;
        }

        /// <summary>
        /// Returns a (copy of the) list of the tiles of the part, eventually randomly rotated 
        /// </summary>
        /// <param name="part"></param>
        /// <returns></returns>
        private TileElement[] GetTilesElements(BoardPart part)
        {
            // Create a copy
            var elements = new TileElement[part.elements.Length];
            for (var i = 0; i < elements.Length; i++)
            {
                elements[i] = part.elements[i];
            }

            // Rotate if necessary
            if (part.rotationEnabled)
            {
                var rotation = _rand.Next(0, 3);
                for (var i = 0; i < rotation; i++)
                {
                    for (var j = 0; j < elements.Length; j++)
                    {
                        elements[j] = elements[j].Rotate();
                    }
                }
            }

            return elements.ToArray();
        }

        /// <summary>
        /// Get a height using the height probability list of a given TileElement
        /// </summary>
        /// <param name="tileElement"></param>
        /// <returns></returns>
        public int GetRandomCellHeight()
        {
            var rng = (float)_rand.NextDouble(); // Works only because we suppose the sum of all percents is 1

            return TileElement.GetHeightForPercent(rng);
        }

        /// <summary>
        /// Rotate the position in the hex grid 60° counter-clockwise
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private Vector3Int RotatePosition60(Vector3Int position)
        {
            return new Vector3Int(-position.z, -position.x, -position.y);
        }

        /// <summary>
        /// Compute height for middle cell
        /// </summary>
        /// <returns></returns>
        private int GetMiddleCellHeight()
        {
            float sum = 0;
            var count = 0;

            // Get neighbors
            var middleCell = Get(Vector3Int.zero);
            var neighbors = new List<HexCell>();
            FillRangeRadius(neighbors, Vector3Int.zero, 1);
            foreach (var n in neighbors)
            {
                if (n != middleCell)
                {
                    sum += n.Height;
                    count++;
                }
            }

            return Mathf.CeilToInt(sum / Mathf.Max(count, 1));
        }

        #endregion

        #region Get Cells

        /// <summary>
        /// Get a cell using cubic grid coordinates
        /// </summary>
        /// <returns></returns>
        public HexCell Get(Vector3Int coords)
        {
            return _grid.GetValueOrDefault(coords);
        }

        /// <summary>
        /// Pick the closest cell from world coordinates
        /// </summary>
        /// <param name="worldCoordinates"></param>
        /// <returns></returns>
        public HexCell GetClosestCell(Vector3 worldCoordinates)
        {
            var p = WorldToCell(worldCoordinates);
            return _grid.GetValueOrDefault(p);
        }

        /// <summary>
        /// Randomly select a free cell of the grid
        /// </summary>
        /// <remarks>This will use the deteministic random, so don't use it for preview!</remarks>
        /// <returns></returns>
        public HexCell GetRandomFreeCell(int minHeight = 0, bool avoidKodamaSpawnCells = false)
        {
            var list = _grid.Values.Where(
                s => !s.IsOccupiedByPawn && !s.WillBeOccupiedByPawn &&
                     s.Height >= minHeight && s.LootBox == null).ToList();

            if (avoidKodamaSpawnCells)
            {
                var kodamaSpawnCells = TabletopGameManager.Instance.Grid.FindFurthestPoints(BaseTabletopPlayer.TabletopPlayers.Count);
                list.RemoveAll(cell => kodamaSpawnCells.Contains(cell));
            }

            if (list.Count == 0)
            {
                return null;
            }

            var index = _rand.Next(0, list.Count);
            return list[index];
        }

        /// <summary>
        /// Randomly select a cell of the grid
        /// </summary>
        /// <remarks>This will use the deteministic random, so don't use it for preview!</remarks>
        /// <returns></returns>
        public HexCell GetRandomCell(Func<HexCell, bool> predicate = null)
        {
            HexCell[] compatibleCells;
            if (predicate != null) compatibleCells = _grid.Values.Where(predicate).ToArray();
            else compatibleCells = _grid.Values.ToArray();

            if (compatibleCells.Length == 0) return null;

            var index = _rand.Next(0, compatibleCells.Length);
            return compatibleCells[index];
        }

        /// <summary>
        /// Get N random cells that are not in the given list
        /// </summary>
        /// <param name="n"></param>
        /// <param name="excludedCells"></param>
        /// <returns></returns>
        public List<HexCell> GetRandomCells(int n, List<HexCell> excludedCells)
        {
            List<HexCell> cells = new();
            var compatibleCells = _grid.Values.Where(c => excludedCells.Contains(c) == false).ToList();

            for (var i = 0; i < n; i++)
            {
                if (compatibleCells.Count == 0) break;

                var index = _rand.Next(0, compatibleCells.Count);
                var c = compatibleCells[index];
                cells.Add(c);
                compatibleCells.Remove(c);
            }

            return cells;
        }

        #endregion

        #region Coordinates

        /// <summary>
        /// Convert cubic coordinates to world position
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public Vector3 CellToWorld(Vector3Int hex)
        {
            var x = _config.cellSize * 1.5f * hex.x;
            var z = _config.cellSize * Mathf.Sqrt(3) * (hex.z + hex.x / 2.0f);
            return new Vector3(x, 0, z);
        }

        /// <summary>
        /// Convert world position to cubic coordinates 
        /// </summary>
        /// <param name="worldPos"></param>
        /// <returns></returns>
        public Vector3Int WorldToCell(Vector3 worldPos)
        {
            var x = worldPos.x / (_config.cellSize * 1.5f);
            var z = worldPos.z / (_config.cellSize * Mathf.Sqrt(3) - x / 2.0f);
            var y = -x - z;

            return HexCell.RoundCoordinates(new Vector3(x, y, z));
        }

        /// <summary>
        /// Get the angle between two grid positions
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns>0 = left, 60 = top left, 120 = top right, 180 = right, 240 = bot right, 300 = bot left</returns>
        public static float GetAngleInDegrees(Vector3Int from, Vector3Int to)
        {
            // Convert hex coordinates to 2D Cartesian coordinates
            var fromPos = new Vector2(
                from.x * 1.5f,
                from.y * Mathf.Sqrt(3) + from.x * Mathf.Sqrt(3) / 2
            );

            var toPos = new Vector2(
                to.x * 1.5f,
                to.y * Mathf.Sqrt(3) + to.x * Mathf.Sqrt(3) / 2
            );

            // Calculate the difference vector
            var direction = toPos - fromPos;

            // Calculate the angle in radians
            var angleRadians = Mathf.Atan2(direction.y, direction.x);

            // Convert radians to degrees
            var angleDegrees = angleRadians * Mathf.Rad2Deg;

            // Ensure the angle is between 0 and 360 degrees
            if (angleDegrees < 0)
                angleDegrees += 360;

            return angleDegrees;
        }

        #endregion

        #region Queries

        /// <summary>
        /// Get a range of cells in the radius from a center position
        /// </summary>
        /// <param name="rangeCells">the list to fill with the results</param>
        /// <param name="centerPosition"></param>
        /// <param name="radius"></param>
        /// <param name="includeCenterPosition"></param>
        /// <param name="predicate"></param>
        public void FillRangeRadius(List<HexCell> rangeCells, Vector3Int centerPosition, int radius,
            bool includeCenterPosition = true, Func<HexCell, bool> predicate = null)
        {
            rangeCells.Clear();

            if (!_grid.TryGetValue(centerPosition, out _))
                return;

            for (var q = -radius; q <= radius; q++)
            {
                for (var r = -radius; r <= radius; r++)
                {
                    for (var s = -radius; s <= radius; s++)
                    {
                        if (q + r + s != 0) continue;

                        var p = centerPosition + new Vector3Int(q, r, s);
                        if ((includeCenterPosition || p != centerPosition) && _grid.TryGetValue(p, out var c) && (predicate == null || predicate(c)))
                        {
                            rangeCells.Add(c);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Count the number of cells in a radius from a center position.
        /// </summary>
        /// <param name="centerPosition"></param>
        /// <param name="radius"></param>
        /// <param name="includeCenterPosition"></param>
        /// <param name="predicate"></param>
        /// <returns>the number of cells</returns>
        public int CountRangeRadius(Vector3Int centerPosition, int radius,
            bool includeCenterPosition = true, Func<HexCell, bool> predicate = null)
        {
            var count = 0;

            if (!_grid.TryGetValue(centerPosition, out _))
                return count;

            for (var q = -radius; q <= radius; q++)
            {
                for (var r = -radius; r <= radius; r++)
                {
                    for (var s = -radius; s <= radius; s++)
                    {
                        if (q + r + s != 0) continue;

                        var p = centerPosition + new Vector3Int(q, r, s);
                        if ((includeCenterPosition || p != centerPosition) && _grid.TryGetValue(p, out var c) && (predicate == null || predicate(c)))
                        {
                            count++;
                        }
                    }
                }
            }

            return count;
        }

        /// <summary>
        /// Get neighbors of a cell 
        /// </summary>
        /// <param name="cell"></param>
        /// <remarks>Similar to a range of 1 but without the cell in it</remarks>
        /// <returns></returns>
        public List<HexCell> GetNeighbors(HexCell cell)
        {
            var neighbors = new List<HexCell>();
            if (cell == null) return neighbors;

            foreach (var direction in Directions)
            {
                if (_grid.TryGetValue(cell.Position + direction, out var c))
                {
                    neighbors.Add(c);
                }
            }

            return neighbors;
        }

        /// <summary>
        /// Get the diagonals of a cell (without the center)
        /// </summary>
        /// <param name="cell"></param>
        /// <returns></returns>
        public List<HexCell> GetDiagonals(HexCell cell)
        {
            var diagonals = new List<HexCell>();
            if (cell == null) return diagonals;

            foreach (var direction in Diagonals)
            {
                if (_grid.TryGetValue(cell.Position + direction, out var c))
                {
                    diagonals.Add(c);
                }
            }

            return diagonals;
        }

        /// <summary>
        /// Get a path of hex between two points, until destination is reached or predicate returns false.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        private List<HexCell> GetLineCells(HexCell from, HexCell to, Func<HexCell, HexCell, bool> predicate)
        {
            var line = new List<HexCell>();
            line.Add(from);

            var n = HexCell.Distance(from, to);

            for (var i = 0; i <= n; i++)
            {
                var lerpedPosition = Vector3.Lerp(from.Position, to.Position, i / (float)n);
                var roundedPosition = HexCell.RoundCoordinates(lerpedPosition);

                var cell = Get(roundedPosition);
                if (cell != null && cell != from)
                {
                    if (predicate == null || predicate(from, cell))
                    {
                        line.Add(cell);
                    }
                    else
                    {
                        break; // Stop the line here and now
                    }
                }
            }

            return line;
        }

        /// <summary>
        /// Get the path of hex between two points
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public List<HexCell> GetLine(HexCell from, HexCell to)
        {
            return GetLineCells(from, to, null);
        }

        /// <summary>
        /// Get a path of hex between two points, testing each point with conditions 
        /// </summary>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public List<HexCell> GetLineOfSight(HexCell from, HexCell to, Func<HexCell, HexCell, bool> predicate)
        {
            return GetLineCells(from, to, predicate);
        }

        /// <summary>
        /// Simple pathfinding between two points 
        /// </summary>
        /// <remarks>https://www.redblobgames.com/pathfinding/a-star/introduction.html#astar</remarks>
        /// <param name="start"></param>
        /// <param name="destination"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public List<HexCell> GetPath(HexCell start, HexCell destination, Func<HexCell, bool> predicate)
        {
            var frontier = new Queue<HexCell>();
            frontier.Enqueue(start);

            // Breadth First Search
            Dictionary<HexCell, HexCell> cameFrom = new();
            cameFrom[start] = null;

            while (frontier.Count > 0)
            {
                var current = frontier.Dequeue();

                if (current == destination) break;

                foreach (var next in GetNeighbors(current))
                {
                    // Filter neighbors for lava cells
                    if (predicate != null && predicate(next) == false) continue;

                    if (cameFrom.ContainsKey(next) == false)
                    {
                        frontier.Enqueue(next);
                        cameFrom[next] = current;
                    }
                }
            }

            // Now get the path
            var path = new List<HexCell>();
            var p = destination;
            path.Add(p);

            while (cameFrom.ContainsKey(p))
            {
                var prev = cameFrom[p];
                if (prev != null && prev != p)
                {
                    path.Insert(0, prev);
                    p = prev;
                }
                else break;
            }

            return path;
        }

        /// <summary>
        /// Get a ring of cells at given radius
        /// </summary>
        /// <param name="radius"></param>
        /// <param name="minHeight"></param>
        /// <returns></returns>
        public List<HexCell> GetRingCells(int radius, int minHeight = -1)
        {
            var cells = new List<HexCell>();
            if (radius == 0)
            {
                cells.Add(Get(Vector3Int.zero));
                return cells;
            }

            // 4 is not a random direction, it's the one that match the ordered Directions array
            var hex = _grid[Directions[4] * radius];

            foreach (var dir in Directions)
            {
                for (var i = 0; i < radius; i++)
                {
                    if (_grid.TryGetValue(hex.Position + dir, out var h))
                    {
                        if (h.Height >= minHeight)
                        {
                            cells.Add(h);
                        }

                        hex = h;
                    }
                }
            }

            return cells;
        }

        /// <summary>
        /// Get the furthest possible ring of cell with at least one cell satisfying the minimal height
        /// </summary>
        /// <param name="minHeight"></param>
        /// <returns></returns>
        public List<HexCell> GetBorderCells(int minHeight)
        {
            // Get the border. If no cells are valid, try to get the radius - 1.
            var r = Radius;

            var ring = new List<HexCell>();
            HexCell validCell = null;

            while (validCell == null)
            {
                ring = GetRingCells(r);
                validCell = ring.FirstOrDefault(c => c.Height >= minHeight);

                r--;
                if (r - 1 <= 0)
                {
                    Log.Info($"No cells found with min height >= {minHeight}");
                    return new List<HexCell>();
                }
            }

            return ring;
        }

        /// <summary>
        /// Get the N furthest possible hex cells for the given players count
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        public List<HexCell> FindFurthestPoints(int count)
        {
            var finalCells = new List<HexCell>();
            var cells = GetBorderCells(0);
            var distance = Mathf.FloorToInt(cells.Count / count);

            // special placement for 4 players
            if (count == 4)
            {
                finalCells.Add(cells[3]);
                finalCells.Add(cells[10]);
                finalCells.Add(cells[18]);
                finalCells.Add(cells[25]);
                return finalCells;
            }

            // If alls cells are underwater
            if (cells.Count == 0)
            {
                return finalCells;
            }

            for (var i = 0; i < count; i++)
            {
                finalCells.Add(cells[(distance * i + 4) % cells.Count]);
            }

            return finalCells;
        }

        /* KEPT FOR NOW UNTIL VALIDATION BY GD
         *
        /// <summary>
        /// Get the N furthest possible hex cells for the given players count
        /// </summary>
        /// <param name="pointsCount"></param>
        /// <param name="minHeight"></param>
        /// <returns></returns>
        public List<HexCell> FindFurthestPoints(int pointsCount, int minHeight)
        {
            Assert.IsTrue(
                pointsCount >= 1 && pointsCount <= 4, $"FindFurthestPoints is for 1-4 points. Not {pointsCount}");

            List<HexCell> cells = new List<HexCell>();
            int maxDistance = 0;

            HexCell[] border = GetBorderCells(minHeight).Where(c => c.Height >= minHeight).ToArray();
            Assert.IsTrue(border.Length >= pointsCount, "Not enough valid border cells.");


            switch (pointsCount)
            {
                case 1:
                    // Any border cells will do
                    HexCell cell = border[_rand.Next(0, border.Length)];
                    cells.Add(cell);
                    break;


                case 2:
                    HexCell[] furthest2 = new HexCell[2];
                    for (int i = 0; i < border.Length; i++)
                    {
                        for (int j = i + 1; j < border.Length; j++)
                        {
                            int dist = HexCell.Distance(border[i], border[j]);
                            if (dist > maxDistance)
                            {
                                maxDistance = dist;
                                furthest2[0] = border[i];
                                furthest2[1] = border[j];
                            }
                        }
                    }

                    cells.AddRange(furthest2);
                    break;

                case 3:
                    // Find furthest 3 points
                    HexCell[] furthest3 = new HexCell[3];
                    for (int i = 0; i < border.Length; i++)
                    {
                        for (int j = i + 1; j < border.Length; j++)
                        {
                            for (int k = j + 1; k < border.Length; k++)
                            {
                                int dist = HexCell.Distance(border[i], border[j])
                                           + HexCell.Distance(border[j], border[k])
                                           + HexCell.Distance(border[k], border[i]);

                                if (dist > maxDistance)
                                {
                                    maxDistance = dist;
                                    furthest3[0] = border[i];
                                    furthest3[1] = border[j];
                                    furthest3[2] = border[k];
                                }
                            }
                        }
                    }

                    cells.AddRange(furthest3);
                    break;

                case 4:
                    // Find furthest 4 points
                    HexCell[] furthest4 = new HexCell[4];
                    for (int i = 0; i < border.Length; i++)
                    {
                        for (int j = i + 1; j < border.Length; j++)
                        {
                            for (int k = j + 1; k < border.Length; k++)
                            {
                                for (int l = k + 1; l < border.Length; l++)
                                {
                                    int dist = HexCell.Distance(border[i], border[j])
                                               + HexCell.Distance(border[j], border[k])
                                               + HexCell.Distance(border[k], border[l])
                                               + HexCell.Distance(border[l], border[i])
                                               + HexCell.Distance(border[i], border[k])
                                               + HexCell.Distance(border[j], border[l])
                                        ;

                                    if (dist > maxDistance)
                                    {
                                        maxDistance = dist;
                                        furthest4[0] = border[i];
                                        furthest4[1] = border[j];
                                        furthest4[2] = border[k];
                                        furthest4[3] = border[l];
                                    }
                                }
                            }
                        }
                    }

                    cells.AddRange(furthest4);
                    break;
            }

            return cells;
        }*/

        #endregion
    }
}
