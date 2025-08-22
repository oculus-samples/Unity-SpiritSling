// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using System.Threading.Tasks;
using Meta.XR.MRUtilityKit;
using TMPro;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace SpiritSling.TableTop
{
    [MetaCodeSample("SpiritSling")]
    public class RoomRequirementsState : TabletopMenuBaseState
    {
        [SerializeField]
        protected CustomButton fallbackBtn;

        [SerializeField]
        protected CustomButton roomCaptureBtn;

        private TMPro.TMP_Text btnText;
        private bool _dirty;
        private bool _isSceneDataPermissionGranted;
        private bool _hasRoom;
        private bool _areRoomConstraintsFullfilled;

        private bool _requireQuestPopup;

        public override void Awake()
        {
            base.Awake();
            btnText = roomCaptureBtn.GetComponentInChildren<TMP_Text>();
            
            fallbackBtn.onClick.AddListener(OnFallbackClicked);
            roomCaptureBtn.onClick.AddListener(OnRoomCaptureClicked);
        }

        public override void Enter()
        {
            _dirty = true;
        }

        public override async void Update()
        {
            if (_dirty)
            {
                _dirty = false;

                if (!_isSceneDataPermissionGranted
                    || !_hasRoom
                    || !_areRoomConstraintsFullfilled)
                {
                    if (DesktopModeEnabler.IsDesktopMode)
                    {
                        LoadFallback();
                        ChangeToNextState();
                    }
                    else
                    {
                        var success = await TryToLoadRoom();
                        if (!success)
                        {
                            if (!_isSceneDataPermissionGranted)
                            {
                                btnText.text = "ALLOW ACCESS";
                            }
                            else
                            {
                                btnText.text = "START SCAN";
                            }
                            
                            m_uiAnimation.Init(MenuStateMachine.UIConfig);
                            FadeIn();
                        }
                        else
                        {
                            ChangeToNextState();
                        }
                    }
                }
                else //all good, proceed
                {
                    ChangeToNextState();
                }
            }
        }

        private void OnFallbackClicked()
        {
            LoadFallback();
            ChangeToNextState();
        }

        private void OnRoomCaptureClicked()
        {
            _requireQuestPopup = true;
            FadeOut();
        }

        protected override void OnFadeOutComplete()
        {
            if (_requireQuestPopup)
            {
                _requireQuestPopup = false;
                if (!_isSceneDataPermissionGranted)
                {
                    AskForPermission();
                }
                else
                {
                    Task.Run(async () => await OpenRoomCapture());
                }
            }
        }

        private async Task<bool> TryToLoadRoom()
        {
            Log.Info("[RoomReq] TryToLoadRoom");

            var res = await MRUK.Instance.LoadSceneFromDevice(false); //requires to be called on main thread

            if (res is MRUK.LoadDeviceResult.NoScenePermission or MRUK.LoadDeviceResult.FailurePermissionInsufficient)
            {
                Log.Info("[RoomReq] NoScenePermission");
                _isSceneDataPermissionGranted = false;
                _hasRoom = false;
                return false;
            }

            if (res == MRUK.LoadDeviceResult.NoRoomsFound)
            {
                Log.Info("[RoomReq] NoRoomsFound");
                _isSceneDataPermissionGranted = true;
                _hasRoom = false;
                return false;
            }

            if (res == MRUK.LoadDeviceResult.Success)
            {
                Log.Info("[RoomReq] Success");
                _isSceneDataPermissionGranted = true;
                _hasRoom = true;
                var success = await ValidateRoomConstraints();
                return success;
            }

            Log.Error("[RoomReq] " + res);
            return false;
        }

        void AskForPermission()
        {
            Log.Info("[RoomReq] AskForPermission");
#if UNITY_ANDROID && !UNITY_EDITOR
            var callbacks = new PermissionCallbacks();
            callbacks.PermissionDenied += permissionId =>
            {
                Log.Warning("[RoomReq] User denied permissions to use scene data");
                _dirty = true;
            };
            callbacks.PermissionGranted += permissionId =>
            {
                Log.Info("[RoomReq] User granted permissions to use scene data");
                _dirty = true;
            };
            Permission.RequestUserPermission(OVRPermissionsRequester.ScenePermission, callbacks);
#endif
        }

        async Task OpenRoomCapture()
        {
            Log.Info("[RoomReq] OpenRoomCapture");
            await OVRScene.RequestSpaceSetup();
            _dirty = true;
        }

        private async Task<bool> ValidateRoomConstraints()
        {
            Log.Info("[RoomReq] ValidateRoomConstraints");

            // wait for scene initialized
            while (!MRUK.Instance.IsInitialized)
            {
                await Task.Yield();
            }

            Log.Info("[RoomReq] checking constraints");

            if (!CheckConstraints())
            {
                Log.Warning("[RoomReq] constraints not ok");
                _areRoomConstraintsFullfilled = false;
                _dirty = true;
            }
            else
            {
                Log.Info("[RoomReq] All good - changing to next state");
                _areRoomConstraintsFullfilled = true;
            }

            return _areRoomConstraintsFullfilled;
        }

        void LoadFallback()
        {
            MRUK.Instance.LoadSceneFromPrefab(
                MRUK.Instance.SceneSettings.RoomPrefabs[Random.Range(0, MRUK.Instance.SceneSettings.RoomPrefabs.Length)]);
        }

        bool CheckConstraints()
        {
            if (GameVolumeManager.Instance.TryGetClosestTablePositionInRange(Vector3.zero, -1, false, out _, out _))
                return true;

            if (GameVolumeManager.Instance.ValidFloorSpawnPositions.Count > 0)
                return true;

            return false;
        }
    }
}
