// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.MRUtilityKit;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

namespace SpiritSling.TableTop
{
    public class GameVolumeSpawner : MonoBehaviour
    {
        [SerializeField]
        private GameObject _objectToSpawn;

        [SerializeField]
        private GameVolumeSpawnRules _spawnRules;

        [SerializeField]
        private bool _forceFixed;

        [SerializeField]
        private Vector3 _forceFixedPosition;

        [SerializeField]
        private GameVolumeGuide _guide;

        public UnityEvent<GameObject> GameVolumeSpawned;

        private Vector3 _position;
        private Quaternion _rotation;
        public void Spawn()
        {
            if (_forceFixed)
            {
                // If position is fixed (editor/desktop mode)
                _position = _forceFixedPosition;
                _rotation = Quaternion.identity;
                OnReadyToSpawn();
            }
            else
            {
                var frontPosition = GetFrontSpawnPosition();
                var frontRotation = GetSpawnRotation(frontPosition);
                
                _position = frontPosition;
                _rotation = frontRotation;
                TryGetSpawnPosition(ref _position,  _rotation);

                _rotation = GetSpawnRotation(_position);
                
                _guide.ReachedDestination.AddListener(OnReadyToSpawn);
                Log.Info("[BOARD] frontpos "+ frontPosition+ ", destpos " + _position);
                _guide.StartGuide(frontPosition,frontRotation, _position, _rotation);
            }
        }

        private void OnReadyToSpawn()
        {
            _guide.ReachedDestination.RemoveListener(OnReadyToSpawn);
            
            var spawnedGameVolume = Instantiate(_objectToSpawn, _position, _rotation);
            SceneManager.MoveGameObjectToScene(spawnedGameVolume, gameObject.scene);
            GameVolumeSpawned?.Invoke(spawnedGameVolume);            
        }

        /// <summary>
        /// //Find closest spawn on floor in front of the player
        /// </summary>
        private bool TryGetClosestPositionOnFloor(Vector3 fromPosition, out Vector3 position)
        {
            position = GameVolumeManager.Instance.GetClosestFrontValidFloorPosition(fromPosition);
            if (position == Vector3.zero)
            {
                return false;
            }

            return true;
        }

        private Vector3 GetFrontSpawnPosition()
        {
            Transform centerEyeAnchor = DesktopModeEnabler.IsDesktopMode ? Camera.main.transform : OVRManager.instance.GetComponent<OVRCameraRig>().centerEyeAnchor;

            Vector3 lookDir = Vector3.ProjectOnPlane(centerEyeAnchor.forward, Vector3.up);
            // Calculate the position in front of the player and slightly lowered from the head position
            return centerEyeAnchor.position + lookDir * _spawnRules.VolumeSpawnerDistance + Vector3.down * _spawnRules.VolumeSpawnerDownOffset;
        }

        private Quaternion GetSpawnRotation(Vector3 position)
        {
            Transform centerEyeAnchor = DesktopModeEnabler.IsDesktopMode ? Camera.main.transform : OVRManager.instance.GetComponent<OVRCameraRig>().centerEyeAnchor;

            // Align the content's forward vector with the look direction of the player
            Vector3 lookDir = Vector3.ProjectOnPlane((position - centerEyeAnchor.position).normalized, Vector3.up);
            return Quaternion.LookRotation(lookDir);            
        }

        private bool TryGetSpawnPosition(ref Vector3 position, Quaternion rotation)
        {
            if (IsValidPlacementPose(position, rotation))
            {
                //snap to table if in range
                if(GameVolumeManager.Instance.TryGetClosestTablePositionInRange(position, 
                        GameVolumeManager.Instance.SpawnRules.SnapSurfaceDistance, true,
                        out var snapPos, out var _))
                {
                    position = snapPos;
                }
                return true;
            }

            if (TryGetClosestPositionOnFloor(position, out position))
            {
                return false;
            }

            if (GameVolumeManager.Instance.TryGetClosestTablePositionInRange(position, -1, false, out position, out var _))
            {
                return false;
            }

            return false;
        }

        private bool IsValidPlacementPose(Vector3 position, Quaternion rotation)
        {
            // Check if the position is inside the room and isn't inside any volume
            var currentRoom = MRUK.Instance.GetCurrentRoom();
            if (!currentRoom.IsPositionInRoom(position))
            {
                Log.Warning("[BOARD] GameVolumeSpawner can't spawn because position is outside the current room.");
                return false;
            }
            
            if (currentRoom.IsPositionInSceneVolume(position, true))
            {
                // and can't snap to top surface
                if (!GameVolumeManager.Instance.TryGetClosestTablePositionInRange(position,
                        GameVolumeManager.Instance.SpawnRules.SnapSurfaceDistance, true,
                        out _, out var _))
                {
                    Log.Warning($"[BOARD] GameVolumeSpawner can't spawn because position is inside an anchor volume.");
                    return false;                    
                }
            }

            // Check that the line that connects the center of the board with the player board (grabbable handles) doesn't hit any scene anchor.
            if (currentRoom.Raycast(new Ray(position, rotation * Vector3.back), _spawnRules.VolumeSpawnerHandlesOffset, out RaycastHit hit))
            {
                Log.Warning("[BOARD] GameVolumeSpawner can't spawn because the player board is not reachable by hand.");
                return false;
            }

            return true;
        }
    }
}