// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpiritSling.TableTop
{
    [CreateAssetMenu(fileName = "HexGrid Config", menuName = "SpiritSling/TableTop/HexGrid Config")]
    public class HexGridConfig : ScriptableObject
    {
        [Header("Grid")]
        public byte radius = 5;

        [Min(0.01f)]
        public float cellSize = 0.04f;

        public float yShift = 0.01f;

        [Header("Height")]
        public int minHeight = -1;

        public int maxHeight = 4;

        [Header("Randomization")]
        public BoardPart[] parts;
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public struct BoardPart
    {
        /// <summary>
        /// The number of hex in each parts.
        /// Update with extra caution!
        /// </summary>
        public const int EXPECTED_SLICE_SIZE = 15;

        public string partName;

        [Range(0, 10)]
        public int weight;

        public bool rotationEnabled;
        public TileElement[] elements;

        public static Vector2Int[] Positions =>
            new[]
            {
                //     o
                //    o o 
                //   o o o
                //  o o o o
                // o o o o o 
                new Vector2Int(1, -2), new Vector2Int(0, -1), new Vector2Int(1, -1), new Vector2Int(-1, 0), new Vector2Int(0, 0),
                new Vector2Int(1, 0), new Vector2Int(-2, 1), new Vector2Int(-1, 1), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(-3, 2),
                new Vector2Int(-2, 2), new Vector2Int(-1, 2), new Vector2Int(0, 2), new Vector2Int(1, 2)
            };
    }

    /// <summary>
    /// 
    /// </summary>
    [Serializable]
    public struct TileElement
    {
        /// <summary>
        /// Q coordinate of the hex
        /// </summary>
        public int q;

        /// <summary>
        /// R coordinate of the hex
        /// </summary>
        public int r;

        // s is deduced

        public static float heightLavaPercent = 0.01f;
        public static float height0Percent = 0.29f;
        public static float height1Percent = 0.45f;
        public static float height2Percent = 0.15f;
        public static float height3Percent = 0.05f;
        public static float height4Percent = 0.05f;

        public static int GetHeightForPercent(float prc)
        {
            if (prc < heightLavaPercent) return -1;
            prc -= heightLavaPercent;

            if (prc < height0Percent) return 0;
            prc -= height0Percent;

            if (prc < height1Percent) return 1;
            prc -= height1Percent;

            if (prc < height2Percent) return 2;
            prc -= height2Percent;

            if (prc < height3Percent) return 3;
            prc -= height3Percent;

            if (prc < height4Percent) return 4;

            return 5;
        }

        public TileElement(int q, int r)
        {
            this.q = q;
            this.r = r;
        }

        public TileElement(Vector2Int p) : this(p.x, p.y) { }

        /// <summary>
        /// 120° degree rotation of the element
        /// </summary>
        /// <returns></returns>
        public TileElement Rotate()
        {
            // Copy
            var e = this;

            var p = new Vector2Int(q, r);

            // We couldn't find a proper formula for that, hence the hard coded positions...
            if (q == 1 && r == -2) p = new(1, 2);
            else if (q == 0 && r == -1) p = new(1, 1);
            else if (q == 1 && r == -1) p = new(0, 2);
            else if (q == -1 && r == 0) p = new(1, 0);
            else if (q == 0 && r == 0) p = new(0, 1);
            else if (q == 1 && r == 0) p = new(-1, 2);
            else if (q == -2 && r == 1) p = new(1, -1);
            else if (q == -1 && r == 1) p = new(0, 0);
            else if (q == 0 && r == 1) p = new(-1, 1);
            else if (q == 1 && r == 1) p = new(-2, 2);
            else if (q == -3 && r == 2) p = new(1, -2);
            else if (q == -2 && r == 2) p = new(0, -1);
            else if (q == -1 && r == 2) p = new(-1, 0);
            else if (q == 0 && r == 2) p = new(-2, 1);
            else if (q == 1 && r == 2) p = new(-3, 2);
            else Log.Error($"Hex position {Position} is not valid in a slice");

            e.q = p.x;
            e.r = p.y;

            return e;
        }

        public Vector3Int Position
        {
            get => new Vector3Int(q, r, -q - r);
        }
    }
}