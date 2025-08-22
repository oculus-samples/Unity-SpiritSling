// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using System;
using Fusion;
using Oculus.Interaction;
using UnityEngine;
using UnityEngine.Events;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// Controls the behavior of the bowling ball, including aiming, launching, and resetting.
    /// </summary>
    [MetaCodeSample("SpiritSling")]
    public class SlingBallShootController : NetworkBehaviour
    {
        public enum States { Idle, Dragging, Launched, Hit }

        private States currentState;

        /// <summary>
        /// Public properties for the slingshot settings
        /// </summary>
        public float MinVelocity { get; set; }

        public float MaxPullDistance { get; set; }
        public float CancelDistance { get; set; }
        public Color ValidPullDistanceColor { get; set; }
        public Color InvalidPullDistanceColor { get; set; }
        public float RestoreBallDelay { get; set; }
        public float SlingBallSpeed { get; set; }

        /// <summary>
        /// Reference to the transform for the aiming arrow.
        /// </summary>
        [SerializeField]
        private Transform aimArrowTr;

        [SerializeField]
        private Transform ballVisualTr;

        /// <summary>
        /// Component for calculating the ball's trajectory.
        /// </summary>
        [SerializeField]
        private Trajectory trajectory;

        [SerializeField]
        private TrajectoryRenderer trajectoryRender;

        /// <summary>
        /// Trail renderer for visualizing the ball's trail.
        /// </summary>
        [SerializeField]
        private TrailRenderer trailRenderer;

        [SerializeField]
        private ParticleSystem ImpactVFX;

        [Header("Audio")]
        [SerializeField]
        private AudioSource _shootGrabMoving;

        [SerializeField]
        private AudioClip[] _shootReleaseAudioClips;

        [SerializeField]
        private AudioClip[] _shootCancelAudioClips;

        [SerializeField, Min(0), Tooltip("The speed threshold above which the grab audio is played.")]
        private float audioGrabSpeedThreshold = 0.01f;

        [SerializeField, Min(0), Tooltip("The smooth time applied to the velocity computation used to play or not the grab audio.")]
        private float audioGrabSmoothTime = 0.04f;

        /// <summary>
        /// Local events
        /// </summary>
        [Serializable]
        public class FloatUnityEvent : UnityEvent<float>
        {
        }

        [Header("Events")]
        public UnityEvent onBallLaunched = new();

        public UnityEvent onBallHit = new();
        public UnityEvent onBallStartDrag = new();
        public UnityEvent onBallDragging = new();
        public FloatUnityEvent onBallDraggingOffLimit = new();
        public UnityEvent onBallDraggingOffLimitBreak = new();
        public UnityEvent onBallDragCancelled = new();
        public UnityEvent onBallRestored = new();
        public UnityEvent onShotActionDone = new();

        /// <summary>
        /// Line renderer for visualizing the pull line.
        /// </summary>
        private LineRenderer lineRenderer;

        /// <summary>
        /// Initial position when the ball is pulled back.
        /// </summary>
        private Vector3 pullInitialPosition;

        // /// <summary> Flag indicating whether the ball is currently being grabbed. </summary>
        // private bool isGrabbing;

        // /// <summary>
        // /// Flag indicating whether the ball has been launched.
        // /// </summary>
        // private bool isLaunched;
        private bool isCancelled;
        private Vector3 nextBallTarget;
        private int nextBallTargetIndex;

        /// <summary>
        /// Component for detecting grab interactions.
        /// </summary>
        public Grabbable Grabbable { get; private set; }

        public float MaxVelocity { get; set; }

        private Vector3 ballVelocity;
        private Vector3 ballPreviousPosition;
        private Vector3 impactPosition;

        public Vector3 GetTargetCollisionPoint() => trajectoryRender.GetCollisionPoint();
        public RaycastHit GetCollisionInfo() => trajectoryRender.GetCollisionInfo();

        public Vector3 GetPointAtY(float y)
        {
            return trajectory.GetPointAtY(y);
        }

        /// <summary>
        /// Initializes components and subscribes to events.
        /// </summary>
        private void Awake()
        {
            // Initialize the line renderer
            lineRenderer = GetComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.enabled = false;

            // Initialize the trail renderer
            if (trailRenderer)
                trailRenderer.enabled = false;

            // Get the Grabbable component and subscribe to its event
            Grabbable = GetComponentInChildren<Grabbable>(true);
            Grabbable.WhenPointerEventRaised += GrabbableOnWhenPointerEventRaised;

            ResetBall();

            onBallHit.AddListener(OnBallHit);
        }

        /// <summary>
        /// Event handler for when the ball is grabbed.
        /// </summary>
        /// <param name="pointerEvent">The pointer event data.</param>
        private void GrabbableOnWhenPointerEventRaised(PointerEvent pointerEvent)
        {
            if (pointerEvent.Type == PointerEventType.Select)
            {
                ballPreviousPosition = ballVisualTr.position;
                pullInitialPosition = transform.parent.position; // Record the initial position of the pull

                currentState = States.Dragging;
                isCancelled = false;
                lineRenderer.enabled = true;
                aimArrowTr.gameObject.SetActive(true);
                trajectory.gameObject.SetActive(true);

                onBallStartDrag?.Invoke();
            }

            if (pointerEvent.Type is PointerEventType.Unselect or PointerEventType.Cancel && isCancelled)
            {
                ResetBall();
            }
        }

        /// <summary>
        /// Updates the state of the ball every frame.
        /// </summary>
        private void Update()
        {
            if (Grabbable.SelectingPointsCount > 0 && currentState == States.Dragging)
            {
                // If the ball is being grabbed

                // Update visuals and trajectory calculations
                UpdateBallVisual(); //clamp the ball to max

                //UpdateLineRenderer();//keep for debug ?
                UpdateAimRotation();
                UpdateTrajectory();

                onBallDragging?.Invoke();

                OffLimitDrag();

                if (GetDistance() > CancelDistance)
                {
                    onBallDraggingOffLimitBreak?.Invoke();
                    CancelDrag();
                }
            }
            else if (currentState == States.Dragging)
            {
                if (IsVelocityTooLow())
                {
                    CancelDrag();
                }
                else
                {
                    // If the ball was grabbed but not yet launched
                    LaunchBall();
                }
            }
            else if (currentState is States.Launched or States.Hit)
            {
                var v1 = (nextBallTarget - transform.position).normalized;

                var isBallMovingTowardsTarget = Vector3.Dot(v1, trajectory.GetDirection()) <= 0f;
                var isLastTarget = nextBallTargetIndex == trajectoryRender.GetPointCount()-1;

                if (currentState == States.Launched && isBallMovingTowardsTarget)
                {
                    //Debug.Log($"{currentState}--{nextBallTargetIndex}/{trajectoryRender.GetPointCount()}");

                    if (isLastTarget)
                    {
                        impactPosition = transform.localPosition;
                        onBallHit?.Invoke();
                        currentState = States.Hit;
                        ResetBall(true);
                        return;
                    }

                    nextBallTargetIndex++;
                    nextBallTarget = trajectoryRender.GetPointIndex(nextBallTargetIndex);
                }

                transform.position = Vector3.MoveTowards(
                transform.position,
                nextBallTarget, SlingBallSpeed * Time.deltaTime);

                if (transform.localPosition.y < -0.2f)
                {
                    ResetBall(true);
                }
            }
        }

        private void LateUpdate()
        {
            // If the ball is being grabbed (only computed on the state authority)
            if (Grabbable.SelectingPointsCount > 0 && currentState == States.Dragging)
            {
                // Computes the smoothed velocity of the ball
                var target = (ballVisualTr.position - ballPreviousPosition) / Time.deltaTime;
                ballVelocity = Vector3.SmoothDamp(ballVelocity, target, ref ballVelocity, audioGrabSmoothTime);
                ballPreviousPosition = ballVisualTr.position;

                RPC_UpdateGrabMovingSound(ballVelocity.sqrMagnitude);
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_UpdateGrabMovingSound(float squareSpeed)
        {
            // Checks if we are under the limit to play the grab audio
            if (squareSpeed < audioGrabSpeedThreshold * audioGrabSpeedThreshold)
            {
                if (_shootGrabMoving.isPlaying)
                {
                    _shootGrabMoving.Stop();
                }
            }
            else
            {
                if (!_shootGrabMoving.isPlaying)
                {
                    _shootGrabMoving.Play();
                }
            }
        }

        private bool IsVelocityTooLow() => GetVelocity() < MinVelocity;

        public void CancelDrag(bool forceCancel = false)
        {
            //Debug.Log("CancelDrag currentState:" + currentState);
            if (isCancelled || (currentState != States.Dragging && !forceCancel))
            {
                return;
            }

            // If there is a real cancel of the shot
            if (currentState == States.Dragging)
            {
                onBallDragCancelled?.Invoke();
                RPC_PlayCancelSound();
            }

            isCancelled = true;
            Grabbable.ProcessPointerEvent(new PointerEvent(0, PointerEventType.Cancel, new Pose()));
            ResetBall();
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_PlayCancelSound()
        {
            _shootGrabMoving.Stop();
            AudioManager.Instance.PlayRandom(_shootCancelAudioClips, AudioMixerGroups.SFX_Shoot, transform);
        }

        /// <summary>
        /// Updates the trajectory calculations.
        /// </summary>
        private void UpdateTrajectory()
        {
            trajectory.Velocity = GetVelocity();
            trajectory.CalculateMatrixFromCurrentTransform();
            trajectory.CalculateAngleByForwardVector();

            trajectory.gameObject.SetActive(IsVelocityTooLow() == false);
        }

        float lastDist;

        private void OffLimitDrag()
        {
            var currentDistance = GetDistance();
            if (currentDistance > MaxPullDistance && currentDistance < CancelDistance)
            {
                var a = 1 - ((CancelDistance - currentDistance) / (CancelDistance - MaxPullDistance));

                //Debug.Log("<color=blue>" + name + ": OffLimitDrag Blend: " + a + "</color>");
                onBallDraggingOffLimit?.Invoke(a);
            }
            else if (!Mathf.Approximately(lastDist, currentDistance))
            {
                onBallDraggingOffLimit?.Invoke(0);
            }

            lastDist = currentDistance;
        }

        private void UpdateBallVisual()
        {
            ballVisualTr.position = pullInitialPosition - GetDirectionNormalized() * GetClampedDistance();
        }

        /// <summary>
        /// Updates the aim arrow's rotation based on the pull direction.
        /// </summary>
        private void UpdateAimRotation()
        {
            var direction = GetDirection();
            if (direction != Vector3.zero)
            {
                aimArrowTr.rotation = Quaternion.LookRotation(-direction); // Rotate aim arrow
                trajectory.transform.rotation = Quaternion.LookRotation(direction); // Rotate trajectory indicator
            }
        }

        /// <summary>
        /// Launches the ball with calculated force.
        /// </summary>
        private void LaunchBall()
        {
            onBallLaunched?.Invoke();
            RPC_PlayShootSound();

            if (trailRenderer)
                trailRenderer.enabled = true;
            lineRenderer.enabled = false;
            trajectory.gameObject.SetActive(false);

            transform.position = ballVisualTr.transform.position;
            ballVisualTr.transform.localPosition = Vector3.zero;

            currentState = States.Launched;
            nextBallTargetIndex = 0;
            nextBallTarget = trajectoryRender.GetPointIndex(nextBallTargetIndex);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_PlayShootSound()
        {
            _shootGrabMoving.Stop();
            AudioManager.Instance.PlayRandom(_shootReleaseAudioClips, AudioMixerGroups.SFX_Shoot, transform);
        }

        /// <summary>
        /// Resets the ball's state after launch.
        /// </summary>
        public void ResetBall(bool throwCompletionEvent = false)
        {
            //Debug.Log("ResetBall throwCompletionEvent:" + throwCompletionEvent + " isCancelled:" + isCancelled);
            currentState = States.Idle;
            lineRenderer.enabled = false;
            aimArrowTr.gameObject.SetActive(false);
            trajectory.gameObject.SetActive(false);

            if (trailRenderer)
                trailRenderer.enabled = false;
            transform.rotation = Quaternion.identity; // Reset rotation
            transform.localPosition = Vector3.zero; // Reset position
            ballVisualTr.localPosition = Vector3.zero;

            currentState = States.Idle;

            onBallRestored.Invoke();

            if (throwCompletionEvent)
                onShotActionDone?.Invoke();
        }

        public HexCell GetAimedCell(bool includeCliff)
        {
            pullInitialPosition = transform.parent.position;

            var direction = GetDirection();
            if (direction != Vector3.zero)
            {
                trajectory.transform.rotation = Quaternion.LookRotation(direction);
            }

            trajectory.Velocity = GetVelocity();
            trajectory.CalculateMatrixFromCurrentTransform();
            trajectory.CalculateAngleByForwardVector();

            _ = trajectoryRender.GetPoints();

            var collisionPos = GetTargetCollisionPoint();
            var targetCollider = GetCollisionInfo().collider;
            HexCell foundTile = null;

            //did we hit a tile or its cliff ?
            if (targetCollider != null)
            {
                var foundTileRenderer = targetCollider.GetComponentInParent<HexCellRenderer>();
                if (foundTileRenderer != null)
                {
                    foundTile = foundTileRenderer.Cell;
                    if (foundTileRenderer.IsAimingAtCliff(collisionPos) && !includeCliff)
                    {
                        return null;
                    }
                }
            }

            //did we hit a pawn/loot ?
            if (foundTile == null)
            {
                foundTile = TabletopGameManager.Instance.GridRenderer.FindClosestCell(collisionPos, TabletopConfig.Get().gridConfig.cellSize);
            }

            return foundTile;
        }

        /// <summary>
        /// Cleanup event listeners on destruction.
        /// </summary>
        private void OnDestroy()
        {
            onBallLaunched.RemoveAllListeners();
            onBallRestored.RemoveAllListeners();
            onShotActionDone.RemoveAllListeners();
        }

        /// <summary>
        /// Calculates the direction vector from the initial pull position to the current position.
        /// </summary>
        /// <returns>The direction vector.</returns>
        private Vector3 GetDirection() => (pullInitialPosition - transform.position);

        /// <summary>
        /// Calculates the normalized direction vector from the initial pull position to the current position.
        /// </summary>
        /// <returns>The normalized direction vector.</returns>
        private Vector3 GetDirectionNormalized() => GetDirection().normalized;

        /// <summary>
        /// Calculates the distance from the initial pull position to the current position.
        /// </summary>
        /// <returns>The distance.</returns>
        private float GetDistance() => GetDirection().magnitude;

        /// <summary>
        /// Calculates the clamped distance to ensure it does not exceed the maximum pull distance.
        /// </summary>
        /// <returns>The clamped distance.</returns>
        private float GetClampedDistance() => Mathf.Clamp(GetDistance(), 0, MaxPullDistance);

        /// <summary>
        /// Calculates the launch velocity based on the clamped distance and velocity multiplier.
        /// </summary>
        /// <returns>The launch velocity.</returns>
        private float GetVelocity()
        {
            var value01 = Mathf.Clamp01(GetClampedDistance() / MaxPullDistance);
            var vel = value01 * MaxVelocity;
            return vel;
        }

        /// <summary>
        /// Calculates the force vector for launching the ball.
        /// </summary>
        /// <returns>The force vector.</returns>
        private Vector3 GetForce() => GetDirectionNormalized() * GetVelocity();


        #region VFX

        private void OnBallHit()
        {
            RPC_PlayImpactVFX(impactPosition);
        }

        [Rpc(RpcSources.All, RpcTargets.All)]
        private void RPC_PlayImpactVFX(Vector3 impactPosition)
        {
            ImpactVFX.transform.localPosition = impactPosition;
            ImpactVFX.Play();
        }

        #endregion
    }
}
