// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// Wrapper for the Vector3 struct to pass it as an out value in an enumerator method.
    /// </summary>
    public class Vector3Wrapper
    {
        public Vector3 Value { get; set; }
    }

    /// <summary>
    /// Manage the interactions, like moving a pawn, for an AI player (bot).
    /// </summary>
    [RequireComponent(typeof(TabletopAiPlayer))]
    public class TabletopAiInteractions : MonoBehaviour
    {
        private const string LOG_TAG = "[AI]";
        private const string AI_MISTAKE = "[AI] Do a mistake:";

        private TabletopAiPlayerConfig aiConfig;

        private WaitForSeconds waitBeforeRealShoot;

        public void Initialize(TabletopAiPlayerConfig aiConfig, List<Slingshot> aiSlingshots)
        {
            this.aiConfig = aiConfig;

            // Setup bot's slingshots to be controlled by code (single point grab)
            foreach (var slingshot in aiSlingshots)
            {
                var ballGrabbable = slingshot.SlingBall.ShootController.Grabbable;
                if (!ballGrabbable.TryGetComponent<GrabFreeTransformerV2>(out var tr2))
                {
                    tr2 = ballGrabbable.gameObject.AddComponent<GrabFreeTransformerV2>();
                }
                tr2.Initialize(ballGrabbable);
                ballGrabbable.InjectOptionalOneGrabTransformer(tr2);
            }

            waitBeforeRealShoot = new WaitForSeconds(aiConfig.DelayBeforeRealShootSeconds);
        }

        private IEnumerator MovePawn(Pawn toMove, HexCell destination)
        {
            Vector3 fromPos = toMove.transform.position;
            Vector3 toPos = TabletopGameManager.Instance.GridRenderer.GetCell(destination).transform.position;

            Vector3 p1 = fromPos;
            Vector3 p2 = fromPos + new Vector3(0, aiConfig.PawnMoveHeigthMeters, 0);
            Vector3 p3 = toPos + new Vector3(0, aiConfig.PawnMoveHeigthMeters, 0);
            Vector3 p4 = toPos + new Vector3(0, 0.01f, 0);


            Log.Info($"{LOG_TAG} Move {toMove.name} from {(toMove.IsOnGrid ? toMove.CurrentCell : "player board")} to {destination}");
            var stateMachine = toMove.StateMachine;

            stateMachine.ChangeState(stateMachine.dragState);
            var t = 0f;
            while (t < 1f)
            {
                toMove.transform.position = GetBezierPosition(p1, p2, p3, p4, t);
                t += Time.deltaTime / aiConfig.PawnMoveDurationSeconds;
                yield return null;
            }

            stateMachine.ChangeState(stateMachine.dropState);
        }

        private Vector3 GetBezierPosition(Vector3 p1, Vector3 p2, Vector3 p3, Vector3 p4, float t)
        {
            var u = 1 - t;
            var tt = t * t;
            var uu = u * u;
            var uuu = uu * u;
            var ttt = tt * t;

            var p = uuu * p1;  // (1-t)^3 * P1
            p += 3 * uu * t * p2;  // 3 * (1-t)^2 * t * P2
            p += 3 * u * tt * p3;  // 3 * (1-t) * t^2 * P3
            p += ttt * p4;         // t^3 * P4

            return p;
        }

        public IEnumerator MoveKodama(Kodama toMove, HexCell destination)
        {
            yield return MovePawn(toMove, destination);
        }

        /// <summary>
        /// Summon a slingshot from the player board or move an already summoned one.
        /// </summary>
        public IEnumerator MoveSlingshot(Slingshot toSummon, HexCell destination)
        {
            yield return MovePawn(toSummon, destination);
        }

        private IEnumerator Aim(Slingshot slingshot, Transform target, Vector3 ballPosition)
        {
            var slingshotTransform = slingshot.transform;
            var slingBallTransform = slingshot.SlingBall.transform;
            var ballGrabbable = slingshot.SlingBall.ShootController.Grabbable;

            var lookPosition = target.position - slingshotTransform.position;
            lookPosition.y = 0;
            var lookQuaternion = Quaternion.LookRotation(lookPosition);

            var slingshotStartRotation = slingshotTransform.rotation;
            var slingBallStartPosition = slingBallTransform.localPosition;

            // Grab ball
            var pose = new Pose(slingshotTransform.position, Quaternion.identity);
            ballGrabbable.ProcessPointerEvent(new PointerEvent(0, PointerEventType.Select, pose));

            // Rotate slingshot (only in Y axis) and move ball
            var elapsedTime = 0f;
            while (elapsedTime < aiConfig.AimingDurationSeconds)
            {
                var completion = elapsedTime / aiConfig.AimingDurationSeconds;
                slingshotTransform.rotation = Quaternion.Lerp(slingshotStartRotation, lookQuaternion, completion);
                slingBallTransform.localPosition = Vector3.Lerp(slingBallStartPosition, ballPosition, completion);
                yield return null;
                elapsedTime += Time.deltaTime;
            }
            slingshotTransform.rotation = lookQuaternion;
            slingBallTransform.localPosition = ballPosition;
        }

        /// <summary>
        /// Return true if the target can be shot with the out local ball position.
        /// </summary>
        public IEnumerator FindShootBallPosition(Slingshot shooter, HexCellRenderer target, bool includeCliff, Vector3Wrapper ballPosition)
        {
            var shooterTransform = shooter.transform;
            var slingBall = shooter.SlingBall;
            var initialShooterRotation = shooterTransform.rotation;
            var initialBallPosition = slingBall.transform.position;

            // Rotate slingshot (only in Y axis)
            var lookPosition = target.transform.position - shooterTransform.position;
            lookPosition.y = 0;
            shooterTransform.rotation = Quaternion.LookRotation(lookPosition);

            if (Random.value < aiConfig.RandomAngleForAttackProbability)
            {
                Log.Debug($"{AI_MISTAKE} random angle to target {target.Cell} with slingshot on {shooter.CurrentCell}");
                var degree = Random.Range(-aiConfig.MaxShootAngleDegree, aiConfig.MaxShootAngleDegree);
                ballPosition.Value = ComputeBallPosition(degree);
            }
            else
            {
                for (var degree = aiConfig.MaxShootAngleDegree; degree >= -aiConfig.MaxShootAngleDegree; degree -= aiConfig.ShootAngleStepDegree)
                {
                    ballPosition.Value = ComputeBallPosition(degree);

                    // Move ball and check which tile it is aiming
                    slingBall.transform.localPosition = ballPosition.Value;
                    var aimedTile = slingBall.ShootController.GetAimedCell(includeCliff);

                    // If a valid shoot position has been found
                    if (aimedTile == target.Cell)
                    {
                        break;
                    }
                    ballPosition.Value = Vector3.zero;
                }
            }

            // Reset visual elements
            shooterTransform.rotation = initialShooterRotation;
            slingBall.transform.position = initialBallPosition;

            // Dispatch computations over frames to avoid freezes
            yield return null;
        }

        private Vector3 ComputeBallPosition(float degree)
        {
            var angleInRadians = degree * Mathf.Deg2Rad;
            var y = aiConfig.ShootPullDistanceMeters * Mathf.Sin(angleInRadians);
            var z = aiConfig.ShootPullDistanceMeters * Mathf.Cos(angleInRadians);
            return new Vector3(0, -y, -z);
        }

        public IEnumerator Shoot(Slingshot shooter, Transform target, Vector3 ballPosition)
        {
            Log.Debug($"{LOG_TAG} Shoot with slingshot on {shooter.CurrentCell}");

            yield return Aim(shooter, target, ballPosition);
            yield return waitBeforeRealShoot;

            // Release ball
            var ballGrabbable = shooter.SlingBall.ShootController.Grabbable;
            ballGrabbable.ProcessPointerEvent(new PointerEvent(0, PointerEventType.Unselect, new Pose()));

            // No matter what value is set to pose.position above, the call to ProcessPointerEvent moves the ball at the bottom of the slingshot
            // So we set the position again to have correct computations in the shoot controller
            shooter.SlingBall.transform.localPosition = ballPosition;
        }
    }
}