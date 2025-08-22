// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.Assertions;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// A single hexagon of the grid
    /// </summary>
    [MetaCodeSample("SpiritSling")]
    public class HexCell
    {
        /// <summary>
        /// Cubic (q,r,s) coordinates
        /// </summary>
        public Vector3Int Position { get; }

        /// <summary>
        /// Height of the cell
        /// </summary>
        public int Height { get; set; }

        private Pawn _pawn;

        /// <summary>
        /// Pawn/Unit on the cell
        /// </summary>
        public Pawn Pawn
        {
            get => _pawn;
            set
            {
                if (value == null && _pawn != null)
                {
                    OnCellFreed?.Invoke(this);
                }
                else if (value != null && _pawn == null)
                {
                    OnCellOccupied?.Invoke(this);
                }

                _pawn = value;
                WillBeOccupiedByPawn = false;
            }
        }

        /// <summary>
        /// LootBox on the cell
        /// </summary>
        public LootBox LootBox { get; set; }

        /// <summary>
        /// Will be in the CLOC radius starting next round
        /// </summary>
        public bool WillHaveCLOC { get; set; }

        public bool WillBeOccupiedByPawn { get; set; }

        /// <summary>
        /// Set if the tile is occupied or free
        /// </summary>
        public bool IsOccupiedByPawn => Pawn != null;

        public bool HasLootBox => LootBox != null;

        public delegate void OnCellHandler(HexCell cell);

        public static OnCellHandler OnCellOccupied;
        public static OnCellHandler OnCellFreed;

        /// <summary>
        /// Cubic coordinates constructor
        /// </summary>
        /// <param name="q"></param>
        /// <param name="r"></param>
        /// <param name="s"></param>
        /// <param name="height"></param>
        public HexCell(int q, int r, int s, int height)
        {
            Position = new Vector3Int(q, r, s);
            Assert.AreEqual(0, q + r + s);

            Height = height;
        }

        /// <summary>
        /// Axial coordinates constructor
        /// </summary>
        /// <param name="q"></param>
        /// <param name="r"></param>
        /// <param name="height"></param>
        public HexCell(int q, int r, int height)
            : this(q, r, -q - r, height)
        {
        }

        public int Length => (Mathf.Abs(Position.x) + Mathf.Abs(Position.y) + Mathf.Abs(Position.z)) / 2;

        public static int Distance(HexCell a, HexCell b) => (a - b).Length;

        /// <summary>
        /// Round a world position to the nearest grid cubic position
        /// </summary>
        /// <param name="worldCoordinates"></param>
        /// <returns></returns>
        public static Vector3Int RoundCoordinates(Vector3 worldCoordinates)
        {
            var q = Mathf.RoundToInt(worldCoordinates.x);
            var r = Mathf.RoundToInt(worldCoordinates.y);
            var s = Mathf.RoundToInt(worldCoordinates.z);

            var qdiff = (int)Mathf.Abs(q - worldCoordinates.x);
            var rdiff = (int)Mathf.Abs(r - worldCoordinates.y);
            var sdiff = (int)Mathf.Abs(s - worldCoordinates.z);

            if (qdiff > rdiff && qdiff > sdiff)
            {
                q = -r - s;
            }
            else if (rdiff > sdiff)
            {
                r = -q - s;
            }
            else
            {
                s = -q - r;
            }

            return new Vector3Int(q, r, s);
        }

        public void FreeOccupation()
        {
            if (Pawn)
            {
                Pawn.CurrentCell = null;
            }
        }

        #region Operators

        public static HexCell operator +(HexCell a, HexCell b)
        {
            return new HexCell(a.Position.x + b.Position.x, a.Position.y + b.Position.y, Mathf.Max(a.Height, b.Height));
        }

        public static HexCell operator -(HexCell a, HexCell b)
        {
            return new HexCell(a.Position.x - b.Position.x, a.Position.y - b.Position.y, Mathf.Max(a.Height, b.Height));
        }

        public static HexCell operator *(HexCell a, int k)
        {
            return new HexCell(a.Position.x * k, a.Position.y * k, a.Height);
        }

        public static bool operator ==(HexCell a, HexCell b)
        {
            if (ReferenceEquals(a, b))
                return true;
            if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
                return false;

            return a.Equals(b);
        }

        public static bool operator !=(HexCell a, HexCell b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (obj is HexCell hc)
                return hc.Position.x == Position.x && hc.Position.y == Position.y && hc.Position.z == Position.z;

            return false;
        }

        public override int GetHashCode() => Position.x ^ Position.y ^ Position.z;

        #endregion

        public override string ToString() => $"{Position} {Height}";
    }
}
