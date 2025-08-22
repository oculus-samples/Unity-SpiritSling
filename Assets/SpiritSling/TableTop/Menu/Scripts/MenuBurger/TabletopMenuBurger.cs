// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using Oculus.Interaction.Input;
using UnityEditor;
using UnityEngine;

namespace SpiritSling.TableTop
{
    [MetaCodeSample("SpiritSling")]
    [SelectionBase]
    public class TabletopMenuBurger : MonoBehaviour
    {
        [SerializeField]
        private GameObject root;

        [SerializeField]
        private CustomButton resumeButton;

        [SerializeField]
        private MenuBurgerPlayerButton[] muteButtons;

        [SerializeField]
        private GameObject quitButtonBackground;

        [SerializeField]
        private CustomButton quitButton;

        public bool IsOpened { get; private set; }

        private MenuBurgerAnchor[] _allAnchors;
        private float _waitBeforeClose; // Avoid missclicks on Resume

        private void Awake()
        {
            root.SetActive(false);

            resumeButton.onClick.AddListener(OnClickResume);
            quitButton.onClick.AddListener(OnClickQuit);

            TabletopGameEvents.OnPlayerJoin += OnPlayerJoinOrLeave;
            TabletopGameEvents.OnPlayerLeave += OnPlayerJoinOrLeave;
            TabletopGameEvents.OnMainMenuEnter += OnMainMenuEnter;
            TabletopGameEvents.OnPawnDragStart += OnPawnStartDrag;
        }

        private void Start()
        {
            _allAnchors = FindObjectsByType<MenuBurgerAnchor>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        }

        private void OnDestroy()
        {
            TabletopGameEvents.OnPlayerJoin -= OnPlayerJoinOrLeave;
            TabletopGameEvents.OnPlayerLeave -= OnPlayerJoinOrLeave;
            TabletopGameEvents.OnMainMenuEnter -= OnMainMenuEnter;
            TabletopGameEvents.OnPawnDragStart -= OnPawnStartDrag;
        }

        private void OnClickResume()
        {
            Close();
        }

        private void OnClickQuit()
        {
            if (_waitBeforeClose > 0)
            {
                return;
            }
            Close();
            TabletopGameManager.Instance.BackToMenu();
        }

        private void Update()
        {
            if (_waitBeforeClose > 0) _waitBeforeClose -= Time.deltaTime;

            // Check if we need to open or close the menu
            if (IsOpened)
            {
                CheckForClose();
            }
            else
            {
                CheckForOpen();
            }
        }

        private void AttachToDominantHand()
        {
            if (DesktopModeEnabler.IsDesktopMode)
            {
                transform.SetParent(GameVolume.Instance.transform);
                transform.SetLocalPositionAndRotation(new Vector3(0, 0.5f, 0), Quaternion.identity);
            }
            else
            {
                var isRightHandDominant = HandVisualReferences.Instance.GetHandVisual(Handedness.Right).Hand.IsDominantHand;

                // Not great obviously but straight to the point
                MenuBurgerAnchor anchor = null;
                foreach (var a in _allAnchors)
                {
                    // The dominant hand is not the hand to which attach the burger menu
                    if (a.handedness == (isRightHandDominant ? OVRPlugin.Handedness.LeftHanded : OVRPlugin.Handedness.RightHanded))
                    {
                        anchor = a;
                        break;
                    }
                }

                if (anchor != null)
                {
                    transform.SetParent(anchor.transform, false);
                    transform.SetLocalPositionAndRotation(anchor.shift, anchor.shiftRotation);
                }
            }
        }

        private void CheckForOpen()
        {
            // Have we passed the menu?
            // Simple way to check: there should be at least us, as a player, in the list!
            // Prevent opening the menu if the local player is currently dragging a pawn or sling ball
            if (BaseTabletopPlayer.TabletopPlayers.Count > 0 && IsBurgerMenuActionPerformed() && !IsLocalPlayerDraggingPawn())
            {
                Open();
            }
        }

        private void CheckForClose()
        {
            if (IsBurgerMenuActionPerformed())
            {
                Close();
            }
        }

        private bool IsBurgerMenuActionPerformed()
        {
            // Hand gesture
            var actionPerformed = OVRInput.GetDown(OVRInput.Button.Start);

#if UNITY_EDITOR
            // Debug keyboard
            actionPerformed |= DesktopModeEnabler.IsDesktopMode && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame;
#endif

            return actionPerformed;
        }

        /// <summary>
        /// Return true if the local player is currently dragging any of his pawn.
        /// </summary>
        private bool IsLocalPlayerDraggingPawn()
        {
            if (PawnMovement.DraggedObject == null)
            {
                return false;
            }

            if (!PawnMovement.DraggedObject.TryGetComponent(out Pawn pawn) && PawnMovement.DraggedObject.TryGetComponent<SlingBall>(out var slingBall))
            {
                pawn = slingBall.Slingshot;
            }

            return pawn != null && pawn.OwnerId == BaseTabletopPlayer.LocalPlayer.PlayerId;
        }

        private void Open()
        {
            IsOpened = true;
            _waitBeforeClose = 0.5f;

            AttachToDominantHand();
            RefreshPlayers();

            quitButtonBackground.SetActive(TabletopGameManager.Instance);
            quitButton.gameObject.SetActive(TabletopGameManager.Instance);

            root.SetActive(true);
        }

        private void Close(bool forceClose = false)
        {
            if (!IsOpened || (!forceClose && _waitBeforeClose > 0))
            {
                return;
            }

            root.SetActive(false);
            IsOpened = false;
            _waitBeforeClose = 0;
        }

        private void RefreshPlayers()
        {
            if (!IsOpened)
            {
                return;
            }

            // Update buttons states
            for (byte i = 0; i < muteButtons.Length; i++)
            {
                var player = BaseTabletopPlayer.GetByPlayerIndex(i);
                muteButtons[i].gameObject.SetActive(player != null);
                muteButtons[i].Player = player;
            }
        }

        private void OnPlayerJoinOrLeave(BaseTabletopPlayer _)
        {
            RefreshPlayers();
        }

        private void OnMainMenuEnter()
        {
            Close(true);
        }

        private void OnPawnStartDrag()
        {
            // If the local player starts dragging a pawn or sling ball, close the burger menu if opened
            if (IsLocalPlayerDraggingPawn())
            {
                Close(true);
            }
        }
    }
}
