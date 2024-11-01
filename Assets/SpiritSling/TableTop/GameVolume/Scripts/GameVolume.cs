// Copyright (c) Meta Platforms, Inc. and affiliates.

using System;
using System.Collections.Generic;
using System.Linq;
using SpiritSling.TableTop;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace SpiritSling
{
    [SelectionBase]
    public class GameVolume : MonoBehaviour
    {
        // Game volume can now be a singleton since we only need one kind
        public static GameVolume Instance;

        [SerializeField]
        private GameObject _ghost;

        [SerializeField]
        private GameObject _noTableLeg;

        [SerializeField]
        private GameObject _legFloor;

        [SerializeField]
        private Transform _ghostPivot;

        [SerializeField]
        private Transform _playerPivot;

        [SerializeField]
        private Transform _playerboardsRoot;

        [SerializeField]
        private Material _ghostWall;

        [SerializeField]
        private Material _ghostTable;

        public UnityEvent SnapToValidPos;

        [SerializeField]
        private LayerMask _GhostLayer;
        [SerializeField]
        private LayerMask _NormalLayer;        

        public GameVolumePlaceholdersManager PlaceholdersManager { get; private set; }

        private List<Transform> _playerBoards = new();
        private List<Renderer> _ghostRenderers;

        private bool _isSnappedOnTable;

        private int _shaderID = Shader.PropertyToID("_UseFlattenHeight");

        public Transform PlayerPivot => _playerPivot;

        private void Awake()
        {
            if (Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            PlaceholdersManager = GetComponentInChildren<GameVolumePlaceholdersManager>();
            _ghostRenderers = _ghost.GetComponentsInChildren<Renderer>().ToList();

            for (var i = 0; i < _playerboardsRoot.childCount; ++i)
            {
                _playerBoards.Add(_playerboardsRoot.GetChild(i));
            }

            HideGhost();

            var transformers = GetComponentsInChildren<GameVolumeTransformer>();
            foreach (var transformer in transformers)
            {
                transformer.updateTransform.AddListener(OnGameVolumeTransformed);
                transformer.endTransform.AddListener(OnGameVolumeReleased);
            }
          
        }

#if UNITY_EDITOR
        private void Update()
        {

            if (Keyboard.current.gKey.isPressed)
            {
                OnGameVolumeTransformed(true);
            }
            else if (Keyboard.current.gKey.wasReleasedThisFrame)
            {
                OnGameVolumeReleased();
            }
        }
#endif
        private void Start()
        {
            GameVolumeManager.Instance.DisplayEffectMesh(false);
            
            if (GameVolumeManager.Instance.TryGetClosestTablePositionInRange(
                    transform.position,
                    GameVolumeManager.Instance.SpawnRules.SnapSurfaceDistance, true,
                    out var _, out var _))
            {
                _isSnappedOnTable = true;
            }
            UpdateLegDisplay();
        }

        private void OnDestroy()
        {
            var transformers = GetComponentsInChildren<GameVolumeTransformer>();
            foreach (var transformer in transformers)
            {
                transformer.updateTransform.RemoveListener(OnGameVolumeTransformed);
                transformer.endTransform.RemoveListener(OnGameVolumeReleased);
            }
        }

        public Transform GetClosestPlayerBoard(Vector3 worldPos)
        {
            var closest = _playerBoards[0];
            var closestDistance = Vector3.Distance(worldPos, _playerBoards[0].position);
            for (var i = 1; i < _playerBoards.Count; ++i)
            {
                var distance = Vector3.Distance(worldPos, _playerBoards[i].position);
                if (distance < closestDistance)
                {
                    closest = _playerBoards[i];
                    closestDistance = distance;
                }
            }

            return closest;
        }

        private void UpdateLegDisplay()
        {
            if (_isSnappedOnTable)
            {
                //disable leg if snapped to table
                _noTableLeg.SetActive(false);
                Shader.SetGlobalFloat(_shaderID, 1);
            }
            else
            {
                //check above scene volume
                if (GameVolumeManager.Instance.IsPositionInSceneVolume(transform.position,true,
                        GameVolumeManager.Instance.SpawnRules.GameVolumeFreeSpaceRadius + GameVolumeManager.Instance.SpawnRules.VolumeAndWallDistanceBuffer))
                {
                    _noTableLeg.SetActive(false);
                    Shader.SetGlobalFloat(_shaderID, 1);
                }
                // check gamevolume height
                else if (GameVolumeManager.Instance.GetHeightFromFloor(transform.position, out var height)
                    && height > GameVolumeManager.Instance.SpawnRules.LegMinHeightDisplay
                    && height < GameVolumeManager.Instance.SpawnRules.LegMaxHeightDisplay)
                {
                    //enable leg
                    _noTableLeg.SetActive(true);

                    //adapt height of leg floor
                    GameVolumeManager.Instance.GetHeightFromFloor(_legFloor.transform.position, out var localHeight);
                    var pivotPos = _noTableLeg.transform.position;
                    var localFloorToPivot = pivotPos.y - _legFloor.transform.position.y;
                    pivotPos.y = localFloorToPivot - localHeight * 0.5f;
                    _noTableLeg.transform.position = pivotPos;
                    Shader.SetGlobalFloat(_shaderID, 0);
                }
                else // too high or too low 
                {
                    _noTableLeg.SetActive(false);
                    Shader.SetGlobalFloat(_shaderID, 1);
                }
            }
        }

        private void OnGameVolumeTransformed()
        {
            OnGameVolumeTransformed(false);
        }
        private void OnGameVolumeTransformed(bool forceInVolume)
        {
            PlaceholdersManager.IsMoving = true;
            PlaceholdersManager.RefreshPlaceholders();

            GameVolumeManager.Instance.DisplayEffectMesh(true);

            if (forceInVolume)
            {
                DisplayGhost(transform.position, true);
            }
            //check close to table
            else if (GameVolumeManager.Instance.TryGetClosestTablePositionInRange(
                    transform.position,
                    GameVolumeManager.Instance.SpawnRules.SnapSurfaceDistance, true,
                    out var bestTablePosition, out var _))
            {
                _isSnappedOnTable = true;
                DisplayGhost(bestTablePosition, false);
            }

            //check walls
            else if (!GameVolumeManager.Instance.IsValidFloorPosition(
                         transform.position
                         , GameVolumeManager.Instance.SpawnRules.GameVolumeFreeSpaceRadius + GameVolumeManager.Instance.SpawnRules.VolumeAndWallDistanceBuffer))
            {
                var validPosition = GameVolumeManager.Instance.GetClosestValidFloorPosition(transform.position);
                _isSnappedOnTable = false;
                DisplayGhost(transform.position, true);
            }
            else
            {
                _isSnappedOnTable = false;
                HideGhost();
            }

            UpdateLegDisplay();
        }

        /// <summary>
        ///  Rotate the game volume around its pivot to get the player facing a specific object
        /// </summary>
        /// <param name="playerIndex"></param>
        /// <param name="playerCount"></param>
        public void RotatePivotForPlayerIndex(int playerIndex, int playerCount)
        {
#if UNITY_STANDALONE_WIN || UNITY_STANDALONE_MAC
            return;
#endif

            Log.Debug($"[GAMEVOLUME] RotatePivotForPlayerId playerIndex:{playerIndex} playerCount:{playerCount}");
            var settings = TabletopConfig.Get().GetBoardSettingsPerPlayerCount(playerCount);

            if (settings.boardRotation == null || playerIndex < 0 || playerIndex >= settings.boardRotation.Count)
            {
                Log.Error("[GAMEVOLUME] Could not find a board settings for playerCount:" + playerCount + " playerIndex:" + playerIndex);
                return;
            }

            var rotation = settings.boardRotation[playerIndex];
            Log.Debug($"[GAMEVOLUME] pivot rotation y:{rotation}");

            _playerPivot.localRotation = Quaternion.Euler(
                _playerPivot.localRotation.eulerAngles.x, rotation, _playerPivot.localRotation.eulerAngles.z);
        }

        private void OnGameVolumeReleased()
        {
            PlaceholdersManager.IsMoving = false;
            PlaceholdersManager.RefreshPlaceholders();

            GameVolumeManager.Instance.DisplayEffectMesh(false);
            
            if (_ghost.activeSelf)
            {
                transform.SetPositionAndRotation(_ghostPivot.transform.position, _ghostPivot.transform.rotation);
                HideGhost();

                SnapToValidPos?.Invoke();
            }
        }

        public void Attach(Transform transform, bool worldPositionStays, bool attachToPlaceholders = false)
        {
            transform.SetParent(attachToPlaceholders ? PlaceholdersManager.transform : _playerPivot, worldPositionStays);
        }

        private void DisplayGhost(Vector3 validPosition, bool inVolume)
        {
            // Iterate through all renderers and swap material
            foreach (var ghostRenderer in _ghostRenderers)
            {
                // get the current (original) materials instances
                // Renderer.materials returns a new copy of the array every time
                var materials = ghostRenderer.materials;

                // in the local array simply replace all elements
                for (var i = 0; i < materials.Length; i++)
                {
                    materials[i] = inVolume ? _ghostWall : _ghostTable;
                }

                // assign back the entire materials array
                ghostRenderer.materials = materials;
            }

            _ghostPivot.transform.position = validPosition;

            Camera.main.cullingMask = inVolume ? _GhostLayer : _NormalLayer;
            _ghost.SetActive(true);
        }

        private void HideGhost()
        {
            _ghostPivot.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            Camera.main.cullingMask = _NormalLayer;
            _ghost.SetActive(false);
        }
    }
}