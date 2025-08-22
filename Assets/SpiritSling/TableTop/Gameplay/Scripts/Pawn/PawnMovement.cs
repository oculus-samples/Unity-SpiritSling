// Copyright (c) Meta Platforms, Inc. and affiliates.

using Fusion;
using Meta.XR.Samples;
using Oculus.Interaction;
using UnityEngine;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// Handles the movement and interaction logic for pawns within the game, including VR drag detection and state management.
    /// </summary>
    [MetaCodeSample("SpiritSling")]
    public class PawnMovement : NetworkBehaviour
    {
        #region Fields

        /// <summary>
        /// The state machine managing the pawn's states.
        /// </summary>
        [SerializeField]
        protected PawnStateMachine stateMachine;

        /// <summary>
        /// The phase during which the pawn is allowed to be moved.
        /// </summary>
        [SerializeField]
        protected TableTopPhase allowedPhase = TableTopPhase.Move;

        /// <summary>
        /// The Grabbable component attached to this pawn for VR interaction.
        /// </summary>
        [SerializeField]
        protected Grabbable grabbable;

        /// <summary>
        /// Vertical VFX for placement
        /// </summary>
        [SerializeField]
        private GameObject columnVfx;

        /// <summary>
        /// Determines if the pawn can be grabbed based on the state of the collider and the authority.
        /// </summary>
        public bool AllowGrab
        {
            get => grabbable.gameObject.activeSelf && stateMachine.pawn.OwnerId == Object.StateAuthority.PlayerId;
            set
            {
                // The collider's enabled state controls whether the pawn can be grabbed.
                grabbable.gameObject.SetActive(value);
            }
        }

        /// <summary>
        /// The cell where the pawn can be dropped.
        /// </summary>
        public HexCell DroppableCell { get; set; }

        /// <summary>
        /// The initial position of the pawn, used for resetting or lerping back.
        /// </summary>
        public Vector3 InitialPosition { get; set; }

        public static GameObject DraggedObject { get; set; }

        public static bool IsDraggingPawn => DraggedObject != null;

        #endregion

        #region VR drag detection

        /// <summary>
        /// Cleans up event subscriptions when the pawn is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            grabbable.WhenPointerEventRaised -= OnPointerEventRaised;
            TabletopGameEvents.OnGamePhaseChanged -= OnGamePhaseChanged;
            TabletopGameEvents.OnPawnDragStart -= OnPawnDragStart;
            TabletopGameEvents.OnPawnDragEnd -= OnPawnDragEnd;

            Pawn.OnMove -= OnMoveSucceed;
        }

        /// <summary>
        /// Initializes the pawn, sets up event handlers, and disables grabbing initially.
        /// </summary>
        protected virtual void Awake()
        {
            grabbable.WhenPointerEventRaised += OnPointerEventRaised;

            if (columnVfx) columnVfx.SetActive(false);

            AllowGrab = false;

            TabletopGameEvents.OnGamePhaseChanged += OnGamePhaseChanged;
            TabletopGameEvents.OnPawnDragStart += OnPawnDragStart;
            TabletopGameEvents.OnPawnDragEnd += OnPawnDragEnd;

            Pawn.OnMove += OnMoveSucceed;
        }

        /// <summary>
        /// Unity's Start method, called on the frame when a script is enabled just before any of the Update methods are called for the first time.
        /// Initializes the pawn's state and sets the instance name if it has state authority.
        /// </summary>
        private void Start()
        {
            stateMachine.ChangeState(stateMachine.idleState);
        }

        protected virtual void Update()
        {
            if (columnVfx && columnVfx.activeInHierarchy)
            {
                columnVfx.transform.rotation = Quaternion.identity; // Keep it straight
            }
        }

        protected void OnPawnDragStart() => OnPawnDragChanged();

        protected void OnPawnDragEnd(Pawn pawn) => OnPawnDragChanged();

        protected void OnPawnDragChanged()
        {
            AllowGrab = CheckAllowGrab() && (DraggedObject == gameObject || DraggedObject == null);
        }

        private void OnMoveSucceed(PawnMovement _)
        {
            // If the pawn is successfully being moved, stop allowing the grab.
            // Grabbing in this situation unsynchronized the pawn visual position with its "backend" position on the board.
            AllowGrab = false;
        }

        /// <summary>
        /// Called when the game phase changes. Updates the ability to grab the pawn and handles state transitions.
        /// </summary>
        /// <param name="player">Reference to the player associated with the phase change.</param>
        /// <param name="phase">The new phase of the game.</param>
        protected void OnGamePhaseChanged(BaseTabletopPlayer player, TableTopPhase phase)
        {
            DraggedObject = null;
            AllowGrab = CheckAllowGrab();

            // Log.Debug($"Pawn {name} OnGamePhaseChanged phase:" + phase + " AllowGrab:" + AllowGrab);

            // Cancel the drag if the game turn phase changes while the pawn is being dragged.
            if (stateMachine.CurrentState == stateMachine.dragState)
            {
                CancelDrag();
            }
        }

        /// <summary>
        /// Checks whether grabbing the pawn is allowed based on the current game phase and player authority.
        /// </summary>
        /// <param name="playerRef">Reference to the player.</param>
        /// <param name="phase">The current phase of the game.</param>
        /// <returns>True if grabbing is allowed, false otherwise.</returns>
        protected virtual bool CheckAllowGrab()
        {
            if (TabletopGameManager.Instance == null)
            {
                Log.Error("TabletopGameManager.Instance is null");
                return false;
            }

            if (stateMachine == null || stateMachine.pawn == null)
            {
                Log.Error("stateMachine or stateMachine.pawn is null");
                return false;
            }

            if (BaseTabletopPlayer.LocalPlayer == null)
                return false;

            var localPlayer = BaseTabletopPlayer.LocalPlayer;
            var ownerId = stateMachine.pawn.OwnerId;
            var currentPlayer = BaseTabletopPlayer.GetByPlayerIndex(TabletopGameManager.Instance.CurrentPlayerIndex);

            if (currentPlayer == null)
            {
                return false;
            }

            var currentPhase = (TableTopPhase)TabletopGameManager.Instance.Phase;

            return currentPhase == allowedPhase &&
                   ownerId == currentPlayer.PlayerId &&
                   ownerId == localPlayer.PlayerId;
        }

        private void CancelDrag()
        {
            grabbable.ProcessPointerEvent(new PointerEvent(0, PointerEventType.Cancel, new Pose()));
            DroppableCell = null;
            AllowGrab = false;
            stateMachine.ChangeState(stateMachine.dropState);
        }

        /// <summary>
        /// Handles VR pointer events related to grabbing and interacting with the pawn.
        /// </summary>
        /// <param name="pointerEvent">The pointer event that occurred.</param>
        protected void OnPointerEventRaised(PointerEvent pointerEvent)
        {
            //Debug.Log("OnPointerEventRaised pointerEvent:" + pointerEvent.Type + " AllowGrab:" + AllowGrab + " killed:" + stateMachine.pawn.IsKilled + " CurrentState:" + stateMachine.CurrentState);
            if (stateMachine.pawn.IsKilled)
                return;

            if (grabbable == null || !AllowGrab)
                return;

            // Change the state based on the type of pointer event and the current state.
            if (pointerEvent.Type == PointerEventType.Select && grabbable.SelectingPointsCount == 1)
            {
                stateMachine.ChangeState(stateMachine.dragState);
            }
            else if (pointerEvent.Type == PointerEventType.Hover && stateMachine.CurrentState == stateMachine.idleState)
            {
                stateMachine.ChangeState(stateMachine.hoverState);
            }
            else if (pointerEvent.Type == PointerEventType.Unselect || pointerEvent.Type == PointerEventType.Cancel)
            {
                stateMachine.ChangeState(stateMachine.dropState);
            }
        }

        /// <summary>
        /// Enable or disable cell detection VFX
        /// </summary>
        /// <param name="yes"></param>
        public void ShowCellDetectionVFX(bool yes)
        {
            if (columnVfx) columnVfx.SetActive(yes);
        }

        #endregion
    }
}
