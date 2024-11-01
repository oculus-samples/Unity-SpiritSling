// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using UnityEngine;

namespace SpiritSling.TableTop
{
    public class GameVolumePlaceholdersManager : MonoBehaviour
    {
        [SerializeField]
        private GameObject _placeholderPlayerboardsRoot;

        [SerializeField]
        private Transform _placementAvatarsRoot;

        /// <summary>
        /// The neutral player board placeholder and all the variants.
        /// </summary>
        [SerializeField]
        private GameObject[] _placeholderPlayerBoards;

        [SerializeField]
        private GameObject _placementAvatarPrefab;

        [SerializeField]
        private string[] _placementAvatarAssets;

        private GameObject[] _placementAvatars = new GameObject[3];

        public bool IsMoving { get; set; }

        private bool _arePlaceholdersEnabled;
        private bool[] _fakeAvatarsToActivate;
        private int _maxFakeAvatarToActivate;

        private void Awake()
        {
            _fakeAvatarsToActivate = new[] { true, true, true };
            _maxFakeAvatarToActivate = _fakeAvatarsToActivate.Length;

            TabletopGameEvents.OnLobbyEnter += OnLobbyEnter;
            TabletopGameEvents.OnFirstMenuEnter += OnMainMenuEnter;
            TabletopGameEvents.OnMainMenuEnter += OnMainMenuEnter;
            TabletopGameEvents.OnPlayerJoin += OnPlayerJoin;
            TabletopGameEvents.OnPlayerLeave += OnPlayerLeave;
            TabletopGameEvents.GameStart += DestroyPlacementAvatars;
            TabletopGameEvents.OnGameBoardReady += OnGameBoardReady;

            SpawnPlacementAvatars();
        }

        private void Start()
        {
            ActivateOnlyNeutralPlayerBoard();
        }

        private void OnDestroy()
        {
            TabletopGameEvents.OnLobbyEnter -= OnLobbyEnter;
            TabletopGameEvents.OnFirstMenuEnter -= OnMainMenuEnter;
            TabletopGameEvents.OnMainMenuEnter -= OnMainMenuEnter;
            TabletopGameEvents.OnPlayerJoin -= OnPlayerJoin;
            TabletopGameEvents.OnPlayerLeave -= OnPlayerLeave;
            TabletopGameEvents.GameStart -= DestroyPlacementAvatars;
            TabletopGameEvents.OnGameBoardReady -= OnGameBoardReady;
        }

        private void OnLobbyEnter()
        {
            SpawnPlacementAvatars();

            _maxFakeAvatarToActivate = ConnectionManager.Instance.Runner.SessionInfo.PlayerCount - 1;

            if (_maxFakeAvatarToActivate == 2)
            {
                _fakeAvatarsToActivate[0] = false;
                _fakeAvatarsToActivate[1] = true;
                _fakeAvatarsToActivate[2] = true;
            }
            else
            {
                for (var i = 0; i < _fakeAvatarsToActivate.Length; i++)
                {
                    _fakeAvatarsToActivate[i] = i < _maxFakeAvatarToActivate;
                }
            }

            _placeholderPlayerBoards[0].SetActive(false);
            EnablePlaceholders(true);
        }

        private void OnMainMenuEnter()
        {
            SpawnPlacementAvatars();

            _maxFakeAvatarToActivate = _fakeAvatarsToActivate.Length;
            for (var i = 0; i < _maxFakeAvatarToActivate; i++)
            {
                _fakeAvatarsToActivate[i] = true;
            }
            _placementAvatarsRoot.localEulerAngles = Vector3.zero;

            EnablePlaceholders(true);
            ActivateOnlyNeutralPlayerBoard();
        }

        private void OnPlayerJoin(BaseTabletopPlayer player)
        {
            OnPlayerJoinOrLeave(null);
        }

        private void OnPlayerLeave(BaseTabletopPlayer player)
        {
            OnPlayerJoinOrLeave(player);
        }

        /// <summary>
        /// Refresh for player's board and all fake avatars.
        /// </summary>
        private void OnPlayerJoinOrLeave(BaseTabletopPlayer leaver)
        {
            var humanPlayerCount = 0;
            foreach (var player in BaseTabletopPlayer.TabletopPlayers)
            {
                if (player.IsHuman)
                {
                    humanPlayerCount++;
                }
            }

            if (BaseTabletopPlayer.LocalPlayer != leaver)
            {
                int maxPlayerCount = ConnectionManager.Instance.Runner.SessionInfo.PlayerCount;
                
                var boardsRotations = TabletopConfig.Get().GetBoardSettingsPerPlayerCount(maxPlayerCount).boardRotation;

                // Rotates all placeholders in order to face the corresponding placeholder board for the local player
                if (TabletopGameManager.Instance == null) // in lobby only
                    SetLocalYRotation(_placeholderPlayerboardsRoot.transform, boardsRotations[BaseTabletopPlayer.LocalPlayer.Index]);

                _placeholderPlayerBoards[0].SetActive(false);
                for (var i = 1; i < _placeholderPlayerBoards.Length; i++)
                {
                    var active = i <= humanPlayerCount;
                    if (active)
                    {
                        // Rotates the placeholder board in front of its player
                        if (TabletopGameManager.Instance == null) // in lobby only
                            SetLocalYRotation(_placeholderPlayerBoards[i].transform, -boardsRotations[i - 1]);
                    }
                    _placeholderPlayerBoards[i].SetActive(active);
                }
            }

            if (TabletopGameManager.Instance == null) // in lobby only
            {
               _maxFakeAvatarToActivate = ConnectionManager.Instance.Runner.SessionInfo.PlayerCount - 1;
                switch (_maxFakeAvatarToActivate)
                {
                    case 0:
                        _fakeAvatarsToActivate[0] = false;
                        _fakeAvatarsToActivate[2] = false;
                        _fakeAvatarsToActivate[1] = false;
                        break;

                    case 1:
                        _fakeAvatarsToActivate[0] = humanPlayerCount < 2;
                        _fakeAvatarsToActivate[2] = false;
                        _fakeAvatarsToActivate[1] = false;
                        break;

                    case 2:
                        _fakeAvatarsToActivate[0] = false;
                        _fakeAvatarsToActivate[2] = humanPlayerCount < 2;
                        _fakeAvatarsToActivate[1] = humanPlayerCount < 3;
                        break;

                    case 3:
                        _fakeAvatarsToActivate[2] = humanPlayerCount < 2;
                        _fakeAvatarsToActivate[0] = humanPlayerCount < 3;
                        _fakeAvatarsToActivate[1] = humanPlayerCount < 4;
                        break;
                }

                if (BaseTabletopPlayer.LocalPlayer != null && _maxFakeAvatarToActivate > 1)
                {
                    var y = BaseTabletopPlayer.LocalPlayer.Index * 90f;
                    _placementAvatarsRoot.localEulerAngles = new Vector3(0, y, 0);
                }  
                
                RefreshPlaceholders();
            }
        }

        private void OnGameBoardReady()
        {
            EnablePlaceholders(false);
        }

        private void SpawnPlacementAvatars()
        {
            for (var i = 0; i < _placementAvatars.Length; ++i)
            {
                if (_placementAvatars[i] == null)
                {
#if USE_AVATAR_POOL
                    _placementAvatars[i] = PoolManager.Instance.GetPoolObject(_placementAvatarPrefab);
                    _placementAvatars[i].transform.SetParent(_placementAvatarsRoot.GetChild(i), false);
#else
                    _placementAvatars[i] = Instantiate(_placementAvatarPrefab, _placementAvatarsRoot.GetChild(i));
#endif
                    _placementAvatars[i].GetComponentInChildren<SampleAvatarEntity>().SetAvatarAsset(_placementAvatarAssets[i]);
                }
            }
        }

        private void DestroyPlacementAvatars()
        {
            for (var i = 0; i < _placementAvatars.Length; ++i)
            {
                if (_placementAvatars[i] != null)
                {
#if USE_AVATAR_POOL
                    _placementAvatars[i].GetComponent<PoolObject>().SendBackToPool();
#else
                    Destroy(_placementAvatars[i]);
#endif
                    _placementAvatars[i] = null;
                }
            }
        }

        private void ActivateOnlyNeutralPlayerBoard()
        {
            // Resets rotation
            SetLocalYRotation(_placeholderPlayerboardsRoot.transform, 0);

            for (var i = 0; i < _placeholderPlayerBoards.Length; i++)
            {
                _placeholderPlayerBoards[i].SetActive(i == 0);
            }
        }

        private void SetLocalYRotation(Transform target, float yRotation)
        {
            var angle = target.localEulerAngles;
            angle.y = yRotation;
            target.localEulerAngles = angle;
        }

        public void EnablePlaceholders(bool visible)
        {
            Log.Debug("EnableMenuPlaceholders " + visible);

            _arePlaceholdersEnabled = visible;
            RefreshPlaceholders();
        }

        public void RefreshPlaceholders()
        {
            _placeholderPlayerboardsRoot.SetActive(_arePlaceholdersEnabled);

            //launch coroutine from an always active gameobject
            GameVolumeManager.Instance.StartCoroutine(InternalRefreshAvatar());
        }

        private IEnumerator InternalRefreshAvatar()
        {
            var showAvatars = _arePlaceholdersEnabled && IsMoving;
            for (var i = 0; i < _fakeAvatarsToActivate.Length; i++)
            {
                if (_placementAvatars[i] != null)
                {
                    _placementAvatars[i].SetActive(_fakeAvatarsToActivate[i] && showAvatars);
                    yield return null;
                }
            }
        }
    }
}