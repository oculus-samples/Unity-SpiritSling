// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using UnityEngine;

namespace SpiritSling.TableTop
{
    public class PawnAnimator : MonoBehaviour
    {
        /// <summary>
        /// This value is a placeholder indicating that a loot will be used.
        /// Its purpose is to prevent transitions to wrong states before the real loot ID is set.
        /// </summary>
        public const int ACTIVATE_LOOT_PLACEHOLDER = -1;

        private static readonly int IDLE_ID = Animator.StringToHash("RandomIdle");
        private static readonly int ENCOURAGE_PLAYER_ID = Animator.StringToHash("EncouragePlayer");
        private static readonly int REACTION_NUMBER_ID = Animator.StringToHash("ReactionNumber");
        private static readonly int TILE_UP_ID = Animator.StringToHash("TileUp");
        private static readonly int TILE_DOWN_ID = Animator.StringToHash("TileDown");
        private static readonly int ACTIVATION_SLING_ID = Animator.StringToHash("ActivationSling");
        private static readonly int LOSING_HP_ID = Animator.StringToHash("LosingHP");
        private static readonly int HEAL_ID = Animator.StringToHash("Heal");
        private static readonly int HOLD_BY_PLAYER_ID = Animator.StringToHash("HoldByPlayer?");
        private static readonly int HAND_VELOCITY_X_ID = Animator.StringToHash("VelocityHandX");
        private static readonly int HAND_VELOCITY_Z_ID = Animator.StringToHash("VelocityHandZ");
        private static readonly int RELEASED_HIGH_ID = Animator.StringToHash("ReleasedHigh?");
        private static readonly int LANDING_ZONE_ID = Animator.StringToHash("LandingZone?");
        private static readonly int IN_LAVA_ID = Animator.StringToHash("InLava?");
        private static readonly int HIT_ID = Animator.StringToHash("Hit");
        private static readonly int HIT_FLOOR_ID = Animator.StringToHash("HitFloor");
        private static readonly int IMMUNE_ID = Animator.StringToHash("Shielded?");
        private static readonly int DEAD_ID = Animator.StringToHash("Dead?");
        private static readonly int ACTIVATE_LOOT_ID = Animator.StringToHash("ActivateLoot");

        [Header("Objects")]
        [SerializeField]
        private Animator animator;
        [SerializeField]
        private Animator lootAnimator;

        [SerializeField]
        private Renderer[] loots;

        [SerializeField]
        private AnimationClip beingReleasedFar;

        [SerializeField]
        private AnimationClip pushedBack;

        [SerializeField]
        private AnimationClip deployingInAir;

        [SerializeField]
        private AnimationClip goingInAir;

        [Header("Drag pawn")]
        [SerializeField, Min(0), Tooltip("From which height from the tile the pawn is considered as high when released.")]
        private float releasedHighThreshold = 0.15f;

        [SerializeField, Min(0), Tooltip("The dragging speed computed for the animation is limited to this value. In m/s.")]
        private float draggingSpeedLimit = 0.4f;

        [SerializeField, Min(0), Tooltip("The smooth time applied to the dragging animation.")]
        private float draggingSmoothTime = 0.04f;

        [Header("Push back")]
        [SerializeField, Range(0, 0.5f), Tooltip("The max height the pawn will reach when pushed. This height is from a level 0 tile.")]
        private float pushedMaxHeight = 0.2f;

        [SerializeField, Range(0, 0.5f),
         Tooltip("How many seconds to wait after the start of the pushed back animation before starting the move animation in code")]
        private float waitAfterPushedBackDuration = 0.16f;

        [SerializeField, Min(0), Tooltip("The exit time set up in the animator on the transition after the pushed back state")]
        private float pushedBackExitTime = 0.9f;

        [Header("Touch lava")]
        [SerializeField, Min(1), Tooltip("The number of times going in air animation is repeated")]
        private float goingInAirRepetitions = 4;

        [Header("Other")]
        [SerializeField, Tooltip("The height of the water in the board")]
        private float waterHeight = -0.04f;

        [SerializeField, Tooltip("The height at which the kodama should die in water")]
        private float deathInWaterHeight = -0.08f;

        [SerializeField, Min(0), Tooltip("The speed at which the pawn falls after being deployed in the air, in m/s.")]
        private float fallingSpeed = 0.1f;

        [SerializeField, Min(0), Tooltip("The speed at which the pawn moves horizontally after being deployed in the air, in m/s.")]
        private float horizontalSpeed = 0.1f;

        [SerializeField, Tooltip(
             "Normalized curve for the falling movement. Time and values must be in the [0-1] range.\n" +
             "A value of 0 means the highest position and a value of 1 is the floor.")]
        private AnimationCurve fallingCurve = AnimationCurve.Linear(0, 0, 1, 1);

        [Header("Lootboxes")]
        [SerializeField]
        private Material normalLootboxMaterial;

        [SerializeField]
        private Material strongLootboxMaterial;

        public float ReleasedHighThreshold => releasedHighThreshold;

        public float PushedMaxHeight => pushedMaxHeight;

        public float WaterHeight => waterHeight;

        public float DeathInWaterHeight => deathInWaterHeight;

        /// <summary>
        /// The speed at which the pawn falls after being deployed in the air, in m/s.
        /// </summary>
        public float FallingSpeed => fallingSpeed;

        /// <summary>
        /// The speed at which the pawn moves horizontally after being deployed in the air, in m/s.
        /// </summary>
        public float HorizontalSpeed => horizontalSpeed;

        public AnimationCurve FallingCurve => fallingCurve;

        /// <summary>
        /// The duration of the move animation for the push back.
        /// </summary>
        public float MovePushBackDuration => -waitAfterPushedBackDuration + pushedBack.length * pushedBackExitTime + deployingInAir.length;

        /// <summary>
        /// The duration of the move animation for going in air.
        /// </summary>
        public float GoingInAirDuration => goingInAir.length * goingInAirRepetitions;

        /// <summary>
        /// Used to wait after the start of the being released far animation before starting the move up animation in code.
        /// </summary>
        public WaitForSeconds WaitAfterBeingReleasedFar { get; private set; }

        /// <summary>
        /// Used to wait after the start of the pushed back animation before starting the move animation in code.
        /// </summary>
        public WaitForSeconds WaitAfterPushedBack { get; private set; }

        private bool immune;

        private bool inLava;

        private bool goingInAirStarted;

        private bool deathAnimationEnded;

        private bool moveAnimationEnded;

        private bool holdByPlayer;

        private bool releasedHigh;

        private Vector3 velocity;

        private Vector3 previousPosition;

        private int currentIdle;

        private bool isActivatingStrongLoot;

        public bool IsActivatingLoot { get; private set; }

        public Animator Animator => animator;

        private void Awake()
        {
            WaitAfterBeingReleasedFar = new WaitForSeconds(beingReleasedFar.length);
            WaitAfterPushedBack = new WaitForSeconds(waitAfterPushedBackDuration);
        }

        private void LateUpdate()
        {
            if (!holdByPlayer)
            {
                return;
            }

            // Computes the smoothed velocity of the pawn
            velocity = Vector3.SmoothDamp(velocity, (transform.position - previousPosition) / Time.deltaTime, ref velocity, draggingSmoothTime);
            previousPosition = transform.position;

            HandVelocityX = Vector3.Dot(transform.right, velocity) * -1;
            HandVelocityZ = Vector3.Dot(transform.forward, velocity) * -1;
        }

        public bool EncouragePlayer
        {
            set => animator.SetBool(ENCOURAGE_PLAYER_ID, value);
        }

        public bool HoldByPlayer
        {
            set
            {
                holdByPlayer = value;
                animator.SetBool(HOLD_BY_PLAYER_ID, value);

                if (holdByPlayer)
                {
                    moveAnimationEnded = false;
                }
            }
        }

        public bool ReleasedHigh
        {
            set
            {
                releasedHigh = value;
                animator.SetBool(RELEASED_HIGH_ID, value);
            }
            get => releasedHigh;
        }

        public bool LandingZone
        {
            set => animator.SetBool(LANDING_ZONE_ID, value);
        }

        public bool InLava
        {
            set
            {
                inLava = value;

                if (inLava)
                {
                    ResetIdle();
                    ResetHeal();
                    goingInAirStarted = false;
                }

                animator.SetBool(IN_LAVA_ID, inLava);
            }
            get => inLava;
        }

        public bool Immune
        {
            set
            {
                immune = value;
                animator.SetBool(IMMUNE_ID, immune);
            }
            get => immune;
        }

        public bool Dead
        {
            set
            {
                deathAnimationEnded = false;
                animator.SetBool(DEAD_ID, value);
            }
        }

        public int Idle
        {
            set
            {
                currentIdle = value;
                animator.SetInteger(IDLE_ID, value);
            }
            get => currentIdle;
        }

        public int ReactionNumber
        {
            set => animator.SetInteger(REACTION_NUMBER_ID, value);
        }

        public void ResetIdle()
        {
            Idle = 0;
        }

        public void ResetReactionNumber()
        {
            animator.SetInteger(REACTION_NUMBER_ID, 0);
        }

        public float HandVelocityX
        {
            set => SetHandVelocity(HAND_VELOCITY_X_ID, value);
        }

        public float HandVelocityZ
        {
            set => SetHandVelocity(HAND_VELOCITY_Z_ID, value);
        }

        private void SetHandVelocity(int id, float value)
        {
            value = MathUtils.ClampedRemap(value, -draggingSpeedLimit, draggingSpeedLimit, -1, 1);
            animator.SetFloat(id, value);
        }

        public void ActivateLoot(int lootId, bool isStrongLoot = false)
        {
            if (lootId != ACTIVATE_LOOT_PLACEHOLDER)
            {
                ResetIdle();
                IsActivatingLoot = true;
                isActivatingStrongLoot = isStrongLoot;
            }
            animator.SetInteger(ACTIVATE_LOOT_ID, lootId);

        }

        public void AddLoot(int activateLootId)
        {
            if (IsActivatingLoot)
            {
                Log.Debug("[LOOT] AddLoot");
                if (activateLootId is > 0 and <= 4)
                {
                    loots[activateLootId - 1].sharedMaterial = isActivatingStrongLoot ? strongLootboxMaterial : normalLootboxMaterial;
                    loots[activateLootId - 1].gameObject.SetActive(true);
                }
                lootAnimator.SetInteger(ACTIVATE_LOOT_ID, activateLootId);
            }
        }

        public void ResetActivateLoot()
        {
            Log.Debug("[LOOT] ResetActivateLoot");
            animator.SetInteger(ACTIVATE_LOOT_ID, 0);
            lootAnimator.SetInteger(ACTIVATE_LOOT_ID, 0);
            for (var i = 0; i < 4; ++i)
                loots[i].gameObject.SetActive(false);
            IsActivatingLoot = false;
        }

        public void TileMoving(bool movingUp)
        {
            if (!IsActivatingLoot && !Immune)
            {
                ResetIdle();
                animator.SetTrigger(movingUp ? TILE_UP_ID : TILE_DOWN_ID);
            }
        }

        public void ActivateSling()
        {
            animator.SetTrigger(ACTIVATION_SLING_ID);
        }

        public void StaticDamage()
        {
            ResetIdle();
            animator.SetTrigger(LOSING_HP_ID);
        }

        public void Heal()
        {
            ResetIdle();
            animator.SetTrigger(HEAL_ID);
        }

        public void ResetHeal()
        {
            animator.ResetTrigger(HEAL_ID);
        }

        public void Hit()
        {
            ResetIdle();
            animator.SetTrigger(HIT_ID);
        }

        public void HitFloor()
        {
            animator.SetTrigger(HIT_FLOOR_ID);
        }

        public void ResetHitFloor()
        {
            animator.ResetTrigger(HIT_FLOOR_ID);
        }

        /// <summary>
        /// Called by animation event.
        /// </summary>
        public void EndFallInLavaAnimation()
        {
            goingInAirStarted = true;
        }

        /// <summary>
        /// Called by animation event.
        /// </summary>
        public void EndDeathAnimation()
        {
            deathAnimationEnded = true;
        }

        /// <summary>
        /// Called by animation event.
        /// </summary>
        public void EndMoveAnimation()
        {
            moveAnimationEnded = true;
        }

        public IEnumerator WaitForGoingInAir()
        {
            while (!goingInAirStarted)
            {
                yield return null;
            }
        }

        public IEnumerator WaitForEndDeathAnimation()
        {
            while (!deathAnimationEnded)
            {
                yield return null;
            }
        }

        public IEnumerator WaitForEndMoveAnimation()
        {
            while (!moveAnimationEnded)
            {
                yield return null;
            }
        }
    }
}