// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

namespace SpiritSling.TableTop
{
    [MetaCodeSample("SpiritSling")]
    public class SetupTabletopPositionState : TabletopMenuBaseState
    {
        [SerializeField]
        private GameVolumeSpawner _gameVolumeSpawner;

        [SerializeField]
        private CustomButton _validateButton;

        private bool _validated;

        private CanvasFollowPlayer _follow;
        public override void Awake()
        {
            _validateButton.onClick.AddListener(OnPositionValidated);

            base.Awake();
        }

        public override void Enter()
        {
            _validated = false;
            if (MenuStateMachine.UnifiedGameVolume == null)
            {
                _gameVolumeSpawner.GameVolumeSpawned.AddListener(OnGameVolumeSpawned);
                _gameVolumeSpawner.Spawn();
            }
        }

        private void OnGameVolumeSpawned(GameObject gameVolume)
        {
            AttachBoardToGameVolume();

            MenuStateMachine.UnifiedGameVolume = gameVolume;

            m_uiAnimation.Init(MenuStateMachine.UIConfig);
            FadeIn();

            // Display menu related visuals
            gameVolume.GetComponent<GameVolume>().PlaceholdersManager.EnablePlaceholders(true);

            if (_validated)
            {
                OnPositionValidated();
            }
            else
            {
                AddTransformerListeners(gameVolume);
            }
        }


        public void OnPositionValidated()
        {
            _validated = true;

            if (MenuStateMachine.UnifiedGameVolume != null)
            {
                ChangeToNextState(true);
            }
        }

        public override void Exit()
        {
            base.Exit();
            _gameVolumeSpawner.GameVolumeSpawned.RemoveListener(OnGameVolumeSpawned);
        }

        protected void AttachBoardToGameVolume()
        {
            // Attaching menus to the center of the game board using the same rotation as the placeholder playerboard
            GameVolume.Instance.Attach(MenuStateMachine.transform, true, true);
            MenuStateMachine.transform.SetLocalPositionAndRotation(new Vector3(0, 0, 0), Quaternion.identity);

            MenuStateMachine.CanvasRoot.transform.SetLocalPositionAndRotation(new Vector3(0, .5f, 0), Quaternion.identity);
            _follow = MenuStateMachine.GetComponentInChildren<CanvasFollowPlayer>(true);
            _follow.ApplyOnUpdate = false;
        }
    }
}
