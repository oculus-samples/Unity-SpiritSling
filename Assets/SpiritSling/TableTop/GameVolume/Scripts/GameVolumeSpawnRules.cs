// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;

namespace SpiritSling
{
    [CreateAssetMenu(fileName = "GameVolumeSpawnRules", menuName = "SpiritSling/TableTop/GameVolume Spawn Rules")]
    public class GameVolumeSpawnRules : ScriptableObject
    {
        // distance from the player's head to the board center
        public float VolumeSpawnerDistance = 0.9f;

        // vertical offset from the player's head to the board center
        public float VolumeSpawnerDownOffset = 0.4f;

        // distance from the board center to the grabbable handles
        public float VolumeSpawnerHandlesOffset = 0.5f;

        // minimal offset distance between the game volume and any wall or other furniture
        public float VolumeAndWallDistanceBuffer = 0.1f;

        // free radius space on a surface or floor to be able to spawn on
        public float GameVolumeFreeSpaceRadius = 0.5f;

        // height at which the game volume is spawned when above the floor
        public float DefaultSpawnHeight = 0.7f;

        // distance at which ghost is snapped to closest top surface
        public float SnapSurfaceDistance = 0.30f;

        // max height distance at which we display the gamevolume leg on floor 
        public float LegMaxHeightDisplay = 0.90f;

        // max height distance at which we display the gamevolume leg on floor 
        public float LegMinHeightDisplay = 0.25f;
    }
}