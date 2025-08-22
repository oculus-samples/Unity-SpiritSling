// Copyright (c) Meta Platforms, Inc. and affiliates.

using Fusion;
using Meta.XR.Samples;
using Oculus.Interaction;
using UnityEngine;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// Manages the behavior of the Slingball object in a tabletop game, 
    /// including its grab functionality and phase-specific interactions.
    /// </summary>
    [MetaCodeSample("SpiritSling")]
    public class SlingBall : NetworkBehaviour
    {
        public delegate void OnShotSequenceDoneHandler(Slingshot slingshot, HexCell targetShotCell, bool hitCliff);

        public static OnShotSequenceDoneHandler OnShotSequenceDone;

        #region Fields

        /// <summary>
        /// The game phase during which the Slingball can be moved.
        /// </summary>
        [SerializeField]
        private TableTopPhase allowedPhase = TableTopPhase.Shoot;

        /// <summary>
        /// The ball renderer
        /// </summary>
        [SerializeField]
        private MeshRenderer ballVisual;

        /// <summary>
        /// Reference to the shoot controller component
        /// </summary>
        [SerializeField]
        private SlingBallShootController shootController;

        public SlingBallShootController ShootController => shootController;

        /// <summary>
        /// Display a visual where the ball hits a collider
        /// </summary>
        [SerializeField]
        private Transform targetHitVisual;

        /// <summary>
        /// Determines whether the Slingball can be grabbed based on its current state,
        /// the state authority of the networked object and whether it is controlled by a human or a bot.
        /// </summary>
        public bool AllowGrab
        {
            get => Object.HasStateAuthority && Slingshot != null && Slingshot.Owner != null && (grabbable.gameObject.activeSelf || !Slingshot.Owner.IsHuman);
            set => grabbable.gameObject.SetActive(value);
        }

        /// <summary>
        /// The Grabbable component that enables VR interaction with the Slingball.
        /// </summary>
        private Grabbable grabbable;

        /// <summary>
        /// The parent slingshot
        /// </summary>
        public Slingshot Slingshot { get; private set; }

        private TabletopConfig.SlingSettingsPerHeight settings;
        private HexCell ballCellTarget;
        private HexCell previousBallCellTarget;
        private bool firstHighlight;
        private bool ballCellHitCliff;
        private bool previousBallCellHitCliff;

        #endregion

        #region VR drag detection

        /// <summary>
        /// Unsubscribes from game phase change events when the Slingball object is destroyed.
        /// </summary>
        private void OnDestroy()
        {
            TabletopGameEvents.OnGamePhaseChanged -= OnGamePhaseChanged;
            TabletopGameEvents.OnShootPhaseEnd -= OnShootPhaseEnd;
            TabletopGameEvents.OnPawnDragStart -= OnStartDrag;
            TabletopGameEvents.OnPawnDragCanceled -= OnCancelDrag;
        }

        /// <summary>
        /// Initializes the Slingball by retrieving its components and setting up event subscriptions.
        /// </summary>
        private void Start()
        {
            grabbable = GetComponentInChildren<Grabbable>();
            Slingshot = GetComponentInParent<Slingshot>();
            shootController = GetComponent<SlingBallShootController>();
            shootController.onBallStartDrag.AddListener(OnBallStartDrag);
            shootController.onBallDragging.AddListener(OnBallDragging);
            shootController.onBallLaunched.AddListener(OnBallLaunched);
            shootController.onShotActionDone.AddListener(OnShotActionDone);
            shootController.onBallDragCancelled.AddListener(OnBallDragCanceled);

            TabletopGameEvents.OnGamePhaseChanged += OnGamePhaseChanged;
            TabletopGameEvents.OnShootPhaseEnd += OnShootPhaseEnd;
            TabletopGameEvents.OnPawnDragStart += OnStartDrag;
            TabletopGameEvents.OnPawnDragCanceled += OnCancelDrag;

            AllowGrab = false;
        }

        private void OnShootPhaseEnd()
        {
            ClearDrag();
            AllowGrab = false;
        }

        private void OnBallStartDrag()
        {
            if (!AllowGrab)
                return;

            previousBallCellTarget = null;
            ballCellTarget = null;
            firstHighlight = true;
            PawnMovement.DraggedObject = gameObject;
            TabletopGameEvents.OnPawnDragStart?.Invoke();
        }

        private void OnStartDrag()
        {
            // Doesn't allow grabbing two objects at the same time
            if (PawnMovement.DraggedObject != gameObject)
            {
                AllowGrab = false;
            }
        }

        private void OnCancelDrag()
        {
            RefreshGrab();
        }

        private void OnBallDragCanceled()
        {
            ClearDrag();
            TabletopGameEvents.OnPawnDragCanceled?.Invoke();
        }

        private void OnBallLaunched()
        {
            AllowGrab = false;
            ClearDrag();
        }

        private void ClearDrag()
        {
            PawnMovement.DraggedObject = null;
            TabletopGameEvents.OnPawnDragEnd?.Invoke(null);

            if (TabletopGameManager.Instance != null && Object.HasStateAuthority)
                TabletopGameManager.Instance.RPC_ClearHighlights();
        }

        private void OnShotActionDone()
        {
            OnShotSequenceDone?.Invoke(Slingshot, ballCellTarget, ballCellHitCliff);
        }

        public HexCell GetTargetCell()
        {
            return ballCellTarget;
        }

        /// <summary>
        /// Called every update during the ball drag phase
        /// </summary>
        private void OnBallDragging()
        {
            if (Slingshot.CurrentCellRenderer == null)
                return;

            if (!AllowGrab)
            {
                shootController.CancelDrag();
                return;
            }

            if (TabletopGameManager.Instance != null)
            {
                var collisionPos = shootController.GetTargetCollisionPoint();
                var targetCollider = shootController.GetCollisionInfo().collider;
                HexCell foundTile = null;
                ballCellHitCliff = false;

                //did we hit a tile or its cliff ?
                if (targetCollider != null && targetCollider.GetComponentInParent<HexCellRenderer>() != null)
                {
                    HexCellRenderer foundTileRenderer = targetCollider.GetComponentInParent<HexCellRenderer>();
                    if (foundTileRenderer != null)
                    {
                        foundTile = foundTileRenderer.Cell;
                        ballCellHitCliff = foundTileRenderer.IsAimingAtCliff(collisionPos);
                    }
                }

                if (targetHitVisual != null)
                {
                    targetHitVisual.position = collisionPos;
                }

                //did we hit a pawn/loot ?
                if (foundTile == null)
                {
                    foundTile = TabletopGameManager.Instance.GridRenderer.FindClosestCell(collisionPos, TabletopConfig.Get().gridConfig.cellSize);
                }

                ballCellTarget = TabletopGameManager.Instance.GridRenderer.HighlightShootableCells(
                    Slingshot.CurrentCell,
                    foundTile, settings.range, HexCellRenderer.CellState.Range, ballCellHitCliff);

                if (firstHighlight || ballCellTarget != previousBallCellTarget || ballCellHitCliff != previousBallCellHitCliff)
                {
                    previousBallCellHitCliff = ballCellHitCliff;
                    previousBallCellTarget = ballCellTarget;
                    firstHighlight = false;
                    TabletopGameManager.Instance.RPC_HighlightShootChanged(Slingshot.CurrentCell.Position,
                        foundTile != null ? foundTile.Position : HexGridRenderer.OutOfBoardPosition,
                        settings.range, (byte)(ballCellHitCliff ? 1 : 0));
                }
            }

            var ballToSling = (Slingshot.transform.position - ballVisual.transform.position).SetY(0);

            // rotate entire slingshot toward the trajectory
            Slingshot.transform.rotation = Quaternion.LookRotation(ballToSling.normalized, Vector3.up);
        }

        private void OnDrawGizmos()
        {
            if (Slingshot == null || Slingshot.CurrentCellRenderer == null)
                return;

            //Vector3 rangePos = shootController.GetPointAtY(slingshot.CurrentCellRenderer.transform.position.y);
            var rangePos = shootController.GetTargetCollisionPoint();

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(rangePos, .01f);
        }

        /// <summary>
        /// Handles game phase changes by updating the grab status of the Slingball.
        /// </summary>
        /// <param name="player">Reference to the player whose game phase has changed.</param>
        /// <param name="phase">The new game phase.</param>
        private void OnGamePhaseChanged(BaseTabletopPlayer player, TableTopPhase phase)
        {
            RefreshGrab();
        }

        public void RefreshGrab()
        {
            AllowGrab = CheckAllowGrab();

            //Debug.Log($"Ball {name} OnGamePhaseChanged phase:" + phase + " AllowGrab:" + AllowGrab);

            if (AllowGrab && Slingshot != null && Slingshot.CurrentCell != null)
            {
                settings = TabletopConfig.Get().GetSlingShotSettingsForHeight(Slingshot.CurrentCell.Height);
                shootController.MaxVelocity = settings.maxVelocity;
                shootController.MinVelocity = TabletopConfig.Get().slingBallMinDistance;
                shootController.MaxPullDistance = TabletopConfig.Get().slingBallMaxDistance;
                shootController.CancelDistance = shootController.MaxPullDistance + TabletopConfig.Get().slingBallToleranceDistance;
                shootController.RestoreBallDelay = TabletopConfig.Get().slingBallRestoreDelay;
                shootController.ValidPullDistanceColor = TabletopConfig.Get().slingBallValidPullDistanceColor;
                shootController.InvalidPullDistanceColor = TabletopConfig.Get().slingBallInvalidPullDistanceColor;
                shootController.SlingBallSpeed = TabletopConfig.Get().slingBallSpeed;
                shootController.ResetBall();
            }
        }

        /// <summary>
        /// Checks if the Slingball is allowed to be grabbed based on the current game phase 
        /// and the player's authority.
        /// </summary>
        /// <returns>True if the Slingball can be grabbed; otherwise, false.</returns>
        private bool CheckAllowGrab()
        {
            var localPlayer = BaseTabletopPlayer.LocalPlayer;
            var currentPlayer = BaseTabletopPlayer.GetByPlayerIndex(TabletopGameManager.Instance.CurrentPlayerIndex);

            if (currentPlayer == null)
            {
                return false;
            }

            var currentPhase = (TableTopPhase)TabletopGameManager.Instance.Phase;

            return currentPhase == allowedPhase &&
                   Object.HasStateAuthority &&
                   localPlayer.PlayerId == currentPlayer.PlayerId &&
                   Slingshot.OwnerId == localPlayer.PlayerId &&
                   Slingshot.IsOnGrid &&
                   (PawnMovement.DraggedObject == gameObject || PawnMovement.DraggedObject == null);
        }

        #endregion
    }
}
