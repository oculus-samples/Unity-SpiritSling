// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

namespace SpiritSling
{
    [MetaCodeSample("SpiritSling")]
    [CreateAssetMenu(fileName = "TableTopAiPlayerConfig", menuName = "SpiritSling/TableTop/TableTop AI Player Config")]
    public class TabletopAiPlayerConfig : ScriptableObject
    {
        [Header("General")]
        [SerializeField, Min(0), Tooltip("If the AI need to skip a phase, how many seconds to wait before skipping?")]
        private float delayBeforeSkipPhaseSeconds = 0.5f;

        /// <summary>
        /// If the AI need to skip a phase, how many seconds to wait before skipping?
        /// </summary>
        public float DelayBeforeSkipPhaseSeconds => delayBeforeSkipPhaseSeconds;

        [SerializeField, Min(0), Tooltip("How many seconds to wait when a new phase starts for the AI?" +
            "This delay ensures that the previous phase or turn has completely finished.")]
        private float newPhaseDelaySeconds = 0.5f;

        /// <summary>
        /// How many seconds to wait when a new phase starts for the AI?
        /// This delay ensures that the previous phase or turn has completely finished.
        /// </summary>
        public float NewPhaseDelaySeconds => newPhaseDelaySeconds;

        /// <summary>
        /// Distance in meters above the pawn for bezier curve points
        /// </summary>
        [Header("Move / Summon")]
        [SerializeField, Min(0), Tooltip("How high the pawn moves above its location during pawn curve movement")]
        private float pawnMoveHeigthMeters = 0.16f;

        public float PawnMoveHeigthMeters => pawnMoveHeigthMeters;

        /// <summary>
        /// How many seconds the pawn movement last
        /// </summary>
        [SerializeField, Min(0), Tooltip("How long pawn movement last")]
        private float pawnMoveDurationSeconds = 2f;

        public float PawnMoveDurationSeconds => pawnMoveDurationSeconds;

        [Header("Shoot")]
        [SerializeField, Min(0), Tooltip("How far the slingshot ball will be pulled from its base position when shooting, in meters.")]
        private float shootPullDistanceMeters = 0.15f;

        /// <summary>
        /// How far the slingshot ball will be pulled from its base position when shooting, in meters.
        /// </summary>
        public float ShootPullDistanceMeters => shootPullDistanceMeters;

        [SerializeField, Range(0, 90), Tooltip("The maximum positive angle between the slinghot ball pulled position," +
            "its base position and an horizontal line going through the base position.")]
        private float maxShootAngleDegree = 80f;

        /// <summary>
        /// The maximum positive angle between the slinghot ball pulled position, its base position and an horizontal line going through the base position.
        /// </summary>
        public float MaxShootAngleDegree => maxShootAngleDegree;

        [SerializeField, Min(0.1f), Tooltip("When testing if a target can be shot, the angle between the slinghot ball pulled position," +
            "its base position and an horizontal line going through the base position is decremented by this value between each test.")]
        private float shootAngleStepDegree = 10;

        /// <summary>
        /// When testing if a target can be shot, the angle between the slinghot ball pulled position,
        /// its base position and an horizontal line going through the base position is decremented by this value between each test.
        /// </summary>
        public float ShootAngleStepDegree => shootAngleStepDegree;

        [SerializeField, Min(0), Tooltip("How many seconds the aim should last?")]
        private float aimingDurationSeconds = 1f;

        /// <summary>
        /// How many seconds the aim should last?
        /// </summary>
        public float AimingDurationSeconds => aimingDurationSeconds;

        [SerializeField, Min(0), Tooltip("How many seconds to wait between the aim and the shoot for a real one?")]
        private float delayBeforeRealShootSeconds = 1f;

        /// <summary>
        /// How many seconds to wait between the aim and the shoot for a real one?
        /// </summary>
        public float DelayBeforeRealShootSeconds => delayBeforeRealShootSeconds;

        [Header("AI errors")]
        [SerializeField, Range(0, 1), Tooltip("The probability to entirely skip the lootbox checks during a turn.")]
        private float skipLootboxProbability = 0.2f;

        /// <summary>
        /// The probability to entirely skip the lootbox checks during a turn.
        /// </summary>
        public float SkipLootboxProbability => skipLootboxProbability;

        [SerializeField, Range(0, 1), Tooltip("The probability to skip the attack planning during a turn (and instead doing a defense planning).")]
        private float skipAttackProbability = 0.125f;

        /// <summary>
        /// The probability to skip the attack planning during a turn (and instead doing a defense planning).
        /// </summary>
        public float SkipAttackProbability => skipAttackProbability;

        [SerializeField, Range(0, 1), Tooltip("The probability to skip the defense planning during a turn (and instead doing random move and summon).")]
        private float skipDefenseProbability = 0.25f;

        /// <summary>
        /// The probability to skip the defense planning during a turn (and instead doing random move and summon).
        /// </summary>
        public float SkipDefenseProbability => skipDefenseProbability;

        [SerializeField, Range(0, 1), Tooltip("The probability to use a random angle for a shoot phase instead of computing the right one.")]
        private float randomAngleForAttackProbability = 0.33f;

        /// <summary>
        /// The probability to use a random angle for a shoot phase instead of computing the right one.
        /// </summary>
        public float RandomAngleForAttackProbability => randomAngleForAttackProbability;
    }
}
