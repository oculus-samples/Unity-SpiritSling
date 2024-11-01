// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using System.Linq;
using Meta.XR.MRUtilityKit;
using UnityEngine;

namespace SpiritSling
{
    public class GameVolumeManager : MonoBehaviour
    {
        public static GameVolumeManager Instance { get; private set; }

        [SerializeField]
        private GameVolumeSpawnRules _spawnRules;

        public GameVolumeSpawnRules SpawnRules => _spawnRules;

        [SerializeField]
        private float _valideGridSpacing = 0.25f;

        [SerializeField]
        private EffectMesh _effectMesh;

        private bool _effectMeshDisplayed;
        private bool _effectMeshCreated;
        
        private MRUKRoom _currentRoom;

        private MRUKRoom CurrentRoom
        {
            get
            {
                if (_currentRoom == null)
                    _currentRoom = MRUK.Instance.GetCurrentRoom();
                return _currentRoom;
            }
        }

        private List<Vector3> _validFloorSpawnPositions = new();

        public List<Vector3> ValidFloorSpawnPositions
        {
            get
            {
                if (_validFloorSpawnPositions.Count == 0)
                    GenerateRandomValidFloorPositions();
                return _validFloorSpawnPositions;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
            }
            else
            {
                DontDestroyOnLoad(gameObject);
                Instance = this;
            }
        }

        public void DisplayEffectMesh(bool display)
        {
            if (!_effectMeshCreated)
            {
                _effectMesh.CreateMesh(CurrentRoom);
                _effectMeshCreated = true;
                _effectMesh.HideMesh = !display;
            }
            
            if (display != _effectMeshDisplayed)
            {
                _effectMeshDisplayed = display;
                _effectMesh.HideMesh = !_effectMeshDisplayed;
            }
        }

        public Vector3 GetClosestFrontValidFloorPosition(Vector3 position)
        {
            if (ValidFloorSpawnPositions.Count == 0)
                return Vector3.zero;

            ValidFloorSpawnPositions.Sort(
                (pos1, pos2) =>
                {
                    var distance1 = Vector3.Distance(pos1, position);
                    var distance2 = Vector3.Distance(pos2, position);
                    var pos1InFront = pos1.z - position.z > 0;
                    var pos2InFront = pos2.z - position.z > 0;
                    // Prefer positions in front of the source
                    if (pos1InFront && !pos2InFront)
                    {
                        return -1;
                    }
                    if (!pos1InFront && pos2InFront)
                    {
                        return 1;
                    }
                    // If both positions are equally in front, sort by distance
                    return distance1.CompareTo(distance2);
                });

            return ValidFloorSpawnPositions.First();
        }

        public Vector3 GetClosestValidFloorPosition(Vector3 position)
        {
            if (ValidFloorSpawnPositions.Count == 0)
                return Vector3.zero;

            var closest = ValidFloorSpawnPositions.First();
            var closestDistance = Vector3.Distance(position, closest);

            foreach (var p in ValidFloorSpawnPositions)
            {
                var distance = Vector3.Distance(position, p);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = p;
                }
            }

            //keep same height
            closest.y = position.y;
            return closest;
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
                return;
            
            var size = new Vector3(0.03f, 0.03f, 0.03f);
            foreach (var position in ValidFloorSpawnPositions)
            {
                Gizmos.DrawCube(position, size);
            }
        }

        private void GenerateRandomValidFloorPositions()
        {
            if (!CurrentRoom || !CurrentRoom.FloorAnchor)
            {
                return;
            }

            var roomBounds = CurrentRoom.GetRoomBounds();
            var extents = roomBounds.extents;
            var minExtent = Mathf.Min(extents.x, extents.y, extents.z);
            var minDistanceToSurface = _spawnRules.GameVolumeFreeSpaceRadius + _spawnRules.VolumeAndWallDistanceBuffer;
            if (minDistanceToSurface > minExtent)
            {
                // We can exit early here as we know it's not possible to generate a position in the room that satisfies
                // the minDistanceToSurface requirement
                return;
            }

            for (var x = roomBounds.min.x + minDistanceToSurface; x < roomBounds.max.x - minDistanceToSurface; x += _valideGridSpacing)
            for (var z = roomBounds.min.z + minDistanceToSurface; z < roomBounds.max.z - minDistanceToSurface; z += _valideGridSpacing)
            {
                var spawnPosition = new Vector3(x, 0, z);

                //offset default spawn height

                //floor up seems to be on forward axis and not up
                var height = CurrentRoom.FloorAnchor.transform.forward * (_spawnRules.DefaultSpawnHeight);
                spawnPosition += height;

                if (IsValidFloorPosition(spawnPosition, minDistanceToSurface))
                {
                    _validFloorSpawnPositions.Add(spawnPosition);
                }
            }
        }

        public bool TryGetClosestTablePositionInRange(Vector3 fromPosition, float range, bool testVerticalBounds, out Vector3 position,
            out Quaternion rotation)
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            var filter = LabelFilter.Included(MRUKAnchor.SceneLabels.TABLE | MRUKAnchor.SceneLabels.BED | MRUKAnchor.SceneLabels.COUCH
                                              | MRUKAnchor.SceneLabels.OTHER);
            MRUKAnchor sceneVolume = null;
            var closestDistance = float.MaxValue;

            if (CurrentRoom == null)
                return false;

            for (var i = 0; i < CurrentRoom.Anchors.Count; i++)
            {
                if (!filter.PassesFilter(CurrentRoom.Anchors[i].Label))
                {
                    continue;
                }

                var anchor = CurrentRoom.Anchors[i];
                anchor.GetClosestSurfacePosition(fromPosition, out var thisSurfPos);

                // close enough
                var dist = testVerticalBounds ? Vector3.Distance(fromPosition, thisSurfPos) : MathUtils.DistanceXZPlane(fromPosition, thisSurfPos);

                if (dist > closestDistance) //we already have a closest spawn point
                    continue;

                if (range < 0 || dist <= range)
                {
                    var localPosition = anchor.transform.InverseTransformPoint(thisSurfPos);
                    var extents = _spawnRules.GameVolumeFreeSpaceRadius * 0.5f;
                    Rect bounds;
                    Matrix4x4 faceTransform;
                    if (anchor.PlaneRect.HasValue)
                    {
                        if (!(anchor.transform.forward.y >= Utilities.InvSqrt2)) //facing up
                            continue;

                        bounds = anchor.PlaneRect.Value;
                        faceTransform = anchor.transform.localToWorldMatrix;
                    }
                    else if (anchor.VolumeBounds.HasValue)
                    {
                        //get top surface only
                        bounds = new()
                        {
                            xMin = anchor.VolumeBounds.Value.min.x,
                            xMax = anchor.VolumeBounds.Value.max.x,
                            yMin = anchor.VolumeBounds.Value.min.y,
                            yMax = anchor.VolumeBounds.Value.max.y
                        };
                        faceTransform = anchor.transform.localToWorldMatrix * Matrix4x4.TRS(
                            new Vector3(0f, 0f, anchor.VolumeBounds.Value.max.z), Quaternion.identity, Vector3.one);
                    }
                    else
                    {
                        continue;
                    }

                    //check if enough space on top surface
                    var size = bounds.size;
                    if (size.x < _spawnRules.GameVolumeFreeSpaceRadius
                        || size.y < _spawnRules.GameVolumeFreeSpaceRadius)
                        continue;

                    var minX = bounds.min.x + extents;
                    var maxX = bounds.max.x - extents;
                    var minY = bounds.min.y + extents;
                    var maxY = bounds.max.y - extents;

                    localPosition.x = Mathf.Clamp(localPosition.x, minX, maxX);
                    localPosition.y = Mathf.Clamp(localPosition.y, minY, maxY);

                    var foundPosition = faceTransform.MultiplyPoint3x4(new Vector3(localPosition.x, localPosition.y, 0f));

                    dist = testVerticalBounds ?
                        Vector3.Distance(fromPosition, foundPosition) :
                        MathUtils.DistanceXZPlane(fromPosition, foundPosition);

                    if (range < 0 || dist <= range)
                    {
                        closestDistance = dist;
                        sceneVolume = anchor;

                        position = foundPosition;
                        rotation = Quaternion.LookRotation(anchor.transform.up);
                    }
                }
            }

            if (sceneVolume != null)
                return true;

            return false;
        }

        public bool GetHeightFromFloor(Vector3 testPosition, out float height)
        {
            height = Mathf.Infinity;

            if (CurrentRoom.FloorAnchor == null)
                return false;

            height = testPosition.y - CurrentRoom.FloorAnchor.transform.position.y;

            return true;
        }

        public bool IsValidFloorPosition(Vector3 testPosition, float distanceBuffer)
        {
            if (!CurrentRoom.IsPositionInRoom(testPosition))
            {
                // Reject points that are outside the room
                return false;
            }

            var filter = LabelFilter.Included(MRUKAnchor.SceneLabels.WALL_FACE);
            var closestDist = CurrentRoom.TryGetClosestSurfacePosition(testPosition, out var _, out var _, filter);
            if (closestDist <= distanceBuffer)
            {
                // Reject points that are too close to the walls
                return false;
            }

            var excludeFilter = LabelFilter.Excluded(
                MRUKAnchor.SceneLabels.WALL_FACE
                | MRUKAnchor.SceneLabels.FLOOR
                | MRUKAnchor.SceneLabels.CEILING
                | MRUKAnchor.SceneLabels.DOOR_FRAME
                | MRUKAnchor.SceneLabels.WINDOW_FRAME
                | MRUKAnchor.SceneLabels.WALL_ART);

            //reject points that are inside other scene volumes
            if(CurrentRoom.IsPositionInSceneVolume(testPosition, out _, true, distanceBuffer))
                return false;
            
            // use TryGetClosestSurfacePosition instead of IsPositionInSceneVolume that does not get precise result enough
            //var spawnDist = CurrentRoom.TryGetClosestSurfacePosition(
            //    testPosition, out _, out _, excludeFilter);
            //if (spawnDist < distanceBuffer)
            //    return false;

            return true;
        }

        public bool IsPositionInSceneVolume(Vector3 testPosition, bool testVerticalBounds, float distanceBuffer)
        {
            return CurrentRoom.IsPositionInSceneVolume(testPosition, out _, testVerticalBounds, distanceBuffer);
        }
    }
}