// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using System;
using System.Collections;
using Fusion;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.VFX;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// Represents a movable and networked entity on the grid in the tabletop game.
    /// This is an abstract base class for all pawn-like objects.
    /// </summary>
    [MetaCodeSample("SpiritSling")]
    public abstract class Pawn : NetworkBehaviour
    {

        #region Events

        /// <summary>
        /// Delegate type for handling successful pawn movements.
        /// </summary>
        /// <param name="pawn">The moving pawn.</param>
        public delegate void OnMoveHandler(PawnMovement pawn);

        /// <summary>
        /// Event triggered when a pawn successfully moves.
        /// </summary>
        public static OnMoveHandler OnMove;

        /// <summary>
        /// Event triggered when a pawn successfully moves and its animation ends.
        /// </summary>
        public static Action OnMoveAnimationEnd;

        /// <summary>
        /// Event triggered when a pawn fails to move
        /// </summary>
        public static OnMoveHandler OnMoveFailed;

        #endregion

        /// <summary>
        /// The state machine managing the pawn's states.
        /// </summary>
        [SerializeField]
        private PawnStateMachine stateMachine;

        [SerializeField]
        protected GameObject visual;

        [SerializeField]
        protected GameObject smokeTrailVisual;

        [SerializeField]
        internal PawnVisualController pawnVisualController;

        [Header("Audio")]
        [SerializeField]
        private AudioMixerGroups defaultAudioGroup;

        [SerializeField]
        private AudioMixerGroups interactionsAudioGroup;

        [SerializeField]
        private AudioMixerGroups waterAudioGroup;

        [SerializeField]
        private AudioClip[] dieAudioClips;

        [SerializeField]
        private AudioClip[] addHpAudioClips;

        [SerializeField]
        private AudioClip[] looseHpAudioClips;

        [SerializeField]
        protected AudioClip[] fallAudioClips;

        [SerializeField]
        private AudioClip[] fallInWaterAudioClips;

        [SerializeField]
        private AudioClip[] moveHighlightClips;

        [SerializeField]
        protected AudioClip[] grabAudioClips;

        [SerializeField]
        private AudioClip[] moveImpactAudioClips;

        [SerializeField]
        private AudioClip[] shootPawnAudioClips;

        [SerializeField]
        private AudioClip[] pushbackAudioClips;

        [SerializeField]
        protected AudioClip[] tileGoingUpAudioClips;

        [SerializeField]
        private AudioClip moveRangeAudioClip;

        [SerializeField]
        private AudioClip moveReleaseDisappearAudioClip;

        [SerializeField]
        private AudioClip moveReleaseReappearAudioClip;

        [Header("Win disappear animation")]
        [SerializeField, Min(0), Tooltip("Time to wait before scaling down the pawn from the disappear animation start.")]
        protected float winDisappearDelayBeforeScale = 0.2f;

        [SerializeField, Min(0)]
        protected float winDisappearScaleDuration = 0.5f;

        /// <summary>
        /// Gets the state machine for the pawn.
        /// </summary>
        public PawnStateMachine StateMachine => stateMachine;

        /// <summary>
        /// The health points of the pawn, which are networked and trigger a change handler.
        /// </summary>
        [Networked]
        public int HealthPoints
        {
            get;
            protected set;
        }

        /// <summary>
        /// The networked position of the pawn on the grid.
        /// </summary>
        [Networked]
        public Vector3Int Position { get; set; }

        /// <summary>
        /// Player owning the pawn (it may not be the authority!)
        /// </summary>
        [Networked]
        public int OwnerId { get; set; }

        protected bool _isOnGrid;

        /// <summary>Â²
        /// Indicates whether the pawn is currently on the grid.
        /// </summary>
        public virtual bool IsOnGrid
        {
            get => _isOnGrid;
            protected set => _isOnGrid = value;
        }

        /// <summary>
        /// The renderer for the hex cell that the pawn is currently occupying.
        /// </summary>
        public HexCellRenderer CurrentCellRenderer { get; set; }

        /// <summary>
        /// The current hex cell that the pawn occupies.
        /// </summary>
        private HexCell _currentCell;

        private NetworkTransform m_networkTransform;

        protected int maxHealthPoints;

        /// <summary>
        /// Gets or sets the current hex cell the pawn is occupying.
        /// When set, it updates the cell's occupancy and attaches the pawn to the cell.
        /// </summary>
        public HexCell CurrentCell
        {
            get => _currentCell;
            set
            {
                if (_currentCell != null)
                {
                    _currentCell.Pawn = null;

                    CurrentCellRenderer = null;
                }

                _currentCell = value;

                if (_currentCell != null)
                {
                    if (_currentCell.IsOccupiedByPawn)
                    {
                        Log.Error($"Cell {_currentCell} is already occupied!");
                        return;
                    }

                    _currentCell.Pawn = this;

                    CurrentCellRenderer = TabletopGameManager.Instance.GridRenderer.GetCell(_currentCell);
                }
            }
        }

        /// <summary>
        /// Pawn has been killed.
        /// </summary>
        protected bool isKilled;

        /// <summary>
        /// Pawn is playing one or more animations
        /// </summary>
        public virtual bool IsPlayingAnimation => IsDespawned == false && animationsInProgress > 0;

        public bool IsKilled => isKilled;

        /// <summary>
        /// A simple local animation count
        /// </summary>
        protected int animationsInProgress;

        protected PawnAnimator pawnAnimator;
        public PawnAnimator PawnAnimator => pawnAnimator;

        public bool IsDespawned { get; private set; }

        /// <summary>
        /// Reset position coroutine, executed only on the state authority.
        /// </summary>
        private Coroutine resetPositionCoroutine;

        public BaseTabletopPlayer Owner => BaseTabletopPlayer.GetByPlayerId(OwnerId);

        public int OwnerIndex => Owner.Index;

        protected TabletopConfig Config => TabletopGameManager.Instance.Config;

        private bool lit;

        #region Spawn

        /// <summary>
        /// Called when the pawn is spawned in the game. Places the pawn in its parent cell.
        /// </summary>
        public override void Spawned()
        {
            base.Spawned();

#if UNITY_EDITOR
            name = $"{GetType().Name}_{OwnerId}";
#endif
            pawnAnimator = GetComponentInChildren<PawnAnimator>();

            if (TabletopGameManager.Instance.Grid != null)
            {
                transform.SetParent(TabletopGameManager.Instance.GridRenderer.transform, true);
            }

            if (!visual.TryGetComponent(out pawnVisualController))
            {
                visual.AddComponent<PawnVisualController>();
            }

            m_networkTransform = GetComponent<NetworkTransform>();

            InitializeVisualEffects();
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);

            UnregisterVFXCallbacks();
            CurrentCell = null;
            animationsInProgress = 0;
            IsDespawned = true;
        }

        #endregion

        #region Drag

        public void StartDragging()
        {
            if (!Object.HasStateAuthority)
            {
                Log.Error("Only state authority can start dragging the pawn");
                return;
            }

            if (resetPositionCoroutine != null)
            {
                StopCoroutine(resetPositionCoroutine);
                resetPositionCoroutine = null;
                animationsInProgress = 0;
            }

            RPC_PlayDraggingAnim();
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_PlayDraggingAnim()
        {
            if (pawnAnimator != null)
            {
                pawnAnimator.ResetHitFloor();
                pawnAnimator.HoldByPlayer = true;
            }

            PlayGrabAudio();
        }

        /// <summary>
        /// Stop the pawn drag animation.
        /// </summary>
        /// <param name="droppedOnValidTile">true if the pawn is dropped on a tile in the range that is not the starting tile.</param>
        /// <param name="releaseHeight">the height between the pawn and the targeted tile. 0 if released above no tile.</param>
        public void StopDragging(bool droppedOnValidTile, float releaseHeight)
        {
            if (!Object.HasStateAuthority)
            {
                Log.Error("Only state authority can stop dragging the pawn");
                return;
            }

            RPC_StopDraggingAnim(droppedOnValidTile, releaseHeight);
        }

        /// <summary>
        /// RPC stopping the pawn drag animation.
        /// </summary>
        /// <param name="droppedOnValidTile">true if the pawn is dropped on a tile in the range that is not the starting tile.</param>
        /// <param name="releaseHeight">the height between the pawn and the targeted tile. 0 if released above no tile.</param>
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_StopDraggingAnim(bool droppedOnValidTile, float releaseHeight)
        {
            if (pawnAnimator != null)
            {
                pawnAnimator.HandVelocityX = 0f;
                pawnAnimator.HandVelocityZ = 0f;
                pawnAnimator.LandingZone = droppedOnValidTile;
                pawnAnimator.ReleasedHigh = droppedOnValidTile && releaseHeight >= pawnAnimator.ReleasedHighThreshold;

                // Sets "hold by player" after the other parameters, to avoid starting an animation while the parameters aren't all set
                pawnAnimator.HoldByPlayer = false;
            }

            if (!droppedOnValidTile)
            {
                StartCoroutine(PlayCancelReleaseAudio());
            }
        }

        private IEnumerator PlayCancelReleaseAudio()
        {
            InternalPlayAudio(moveReleaseDisappearAudioClip, interactionsAudioGroup);
            if (pawnAnimator != null)
            {
                yield return pawnAnimator.WaitAfterBeingReleasedFar;
            }

            InternalPlayAudio(moveReleaseReappearAudioClip, interactionsAudioGroup);
        }

        #endregion

        #region Move

        /// <summary>
        /// Move pawn to a new cell
        /// </summary>
        /// <param name="newCell"></param>
        public virtual void MoveTo(HexCell newCell, Vector3 destOffset = new Vector3())
        {
            if (Object.HasStateAuthority == false)
            {
                Log.Error("Only state authority can move the pawn");
                return;
            }

            Position = newCell.Position;

            float duration;
            if (pawnAnimator != null && pawnAnimator.ReleasedHigh)
            {
                var target = TabletopGameManager.Instance.GridRenderer.GetCell(newCell).transform;
                var releaseHeight = transform.position.y - target.position.y;
                duration = releaseHeight / pawnAnimator.FallingSpeed;
            }
            else
            {
                duration = Config.pawnPlacementAnimationDuration;
            }

            // Move animation
            RPC_PlayMoveAnim(newCell.Position, duration, destOffset);

            var moveToLootbox = CurrentCell != null && CurrentCell.LootBox != null;
            var moveToLava = CurrentCell != null && CurrentCell.Height < 0;

            if (moveToLootbox || moveToLava)
            {
                StartCoroutine(AfterMoveChecks(moveToLootbox, moveToLava));
            }
            else
            {
                // Doesn't wait for the end of the move animation
                OnMoveAnimationEnd?.Invoke();
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_PlayMoveAnim(Vector3Int position, float duration, Vector3 destOffset)
        {
            Log.Info(name + $" moved to {position}");
            CurrentCell = TabletopGameManager.Instance.Grid.Get(position);
            IsOnGrid = true;
            StartCoroutine(MoveAnim(CurrentCellRenderer.transform, duration, Tweens.EaseOut, destOffset, true));
        }

        private IEnumerator AfterMoveChecks(bool moveToLootbox, bool moveToLava)
        {
            if (pawnAnimator != null)
            {
                yield return pawnAnimator.WaitForEndMoveAnimation();
            }

            var alreadyTakingLavaDamage = false;
            if (moveToLootbox)
            {
                // Gets the loot type before the loot box is removed from the cell
                var lootType = CurrentCell.LootBox.LootType;
                MoveToLootbox(CurrentCell.LootBox);

                if (pawnAnimator != null)
                {
                    // Wait to be sure that the loot box effect has been triggered
                    yield return new WaitForSeconds(TabletopConfig.Get().LootBoxEffectDelays[(int)lootType] + LootsManager.AFFECT_PAWN_DELAY + 0.1f);

                    // The loot may have changed the cell's height
                    moveToLava = CurrentCell != null && CurrentCell.Height < 0;
                    // The loot may have already triggered the touch lava
                    alreadyTakingLavaDamage = pawnAnimator.InLava;
                }
            }

            if (moveToLava && !alreadyTakingLavaDamage)
            {
                Log.Info(name + " has moved in lava");
                DamageLava(TabletopGameManager.Instance.Settings.lavaDamage);
            }

            OnMoveAnimationEnd?.Invoke();
        }

        /// <summary>
        /// Move pawn back to its cell (offline method)
        /// </summary>
        /// <param name="waitForPawnAnimation">true if we wait for the pawn's reset animation before moving it</param>
        public virtual void ResetPosition(bool waitForPawnAnimation = false, bool hitFloor = false)
        {
            resetPositionCoroutine = StartCoroutine(ResetPositionAnim(waitForPawnAnimation, hitFloor));
        }

        private IEnumerator ResetPositionAnim(bool waitForPawnAnimation, bool hitFloor)
        {
            if (waitForPawnAnimation && pawnAnimator != null)
            {
                yield return pawnAnimator.WaitAfterBeingReleasedFar;
            }

            yield return MoveAnim(
                CurrentCellRenderer.transform, Config.pawnPlacementAnimationDuration, Tweens.EaseOut,
                Vector3.zero, hitFloor);

            resetPositionCoroutine = null;
        }

        /// <summary>
        /// Movement animation
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="duration"></param>
        /// <param name="destOffset">destination offset in Unity coordinates</param>
        /// <param name="hitFloor">true to play the hit floor sound and animation</param>
        /// <returns></returns>
        protected virtual IEnumerator MoveAnim(Transform dest, float duration, AnimationCurve ease,
            Vector3 destOffset = new Vector3(), bool hitFloor = false)
        {
            animationsInProgress++;

            // Place correctly on cell
            transform.GetPositionAndRotation(out var start, out var startRot);

            void Step(float p)
            {
                // Get the destination. It may have changed if Game Volume has moved, so we recompute every time
                var destination = dest.position + destOffset;

                var middleCell = TabletopGameManager.Instance.GridRenderer.GetCell(Vector3Int.zero);
                var dir = middleCell.transform.position - transform.position;
                dir.y = 0;

                if (dir == Vector3.zero)
                {
                    transform.position = Vector3.Lerp(start, destination, p);
                }
                else
                {
                    var endRot = Quaternion.LookRotation(dir);
                    transform.SetPositionAndRotation(
                        Vector3.Lerp(start, destination, p),
                        Quaternion.Slerp(startRot, endRot, p));
                }
            }

            yield return Tweens.Lerp(0f, 1f, duration, ease, Step);

            Step(1f);

            StopSmokeTrailVFX();

            PlayMoveSucceedVFX();

            if (hitFloor)
            {
                if (pawnAnimator != null)
                {
                    pawnAnimator.HitFloor();
                }

                if (CurrentCell.Height >= 0)
                {
                    InternalPlayAudio(moveImpactAudioClips);
                }
            }

            animationsInProgress--;
        }

        #endregion

        #region Health Gain

        [ContextMenu("GainHealth")]
        public void GainHealth()
        {
            GainHealth(1);
        }

        [ContextMenu("GainHealth3")]
        public void GainHealth3()
        {
            GainHealth(3);
        }
        [ContextMenu("HealMeee")]
        public void HealMeee()
        {
            GainHealth(30);
        }
        /// <summary>
        /// Damages the pawn with a sling shot
        /// </summary>
        /// <param name="amount"></param>
        public virtual void GainHealth(int amount)
        {
            if (Object.HasStateAuthority == false)
            {
                Log.Error("Requires state authority");
                return;
            }

            if (HealthPoints == maxHealthPoints)
            {
                return;
            }

            if (HealthPoints + amount > maxHealthPoints)
            {
                amount = maxHealthPoints - HealthPoints;
            }

            HealthPoints += amount;

            RPC_PlayHealthGainAnim(HealthPoints, amount);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_PlayHealthGainAnim(int newPointsCount, int gain)
        {
            Log.Info(name + $" gain {gain} health points");

            StartCoroutine(HealthGainAnim(newPointsCount, gain));
            if (pawnAnimator != null)
            {
                pawnAnimator.Heal();
            }

            InternalPlayAudio(addHpAudioClips);
        }

        protected virtual IEnumerator HealthGainAnim(int newPointsCount, int gain)
        {
            pawnVisualController.AnimateHealVFX();
            yield return null; // Placeholder for animation logic.
        }

        #endregion

        #region Damage
        [ContextMenu("Damage1")]
        public void Damage1()
        {
            Damage(1);
        }
        [ContextMenu("Damage3")]
        public void Damage3()
        {
            Damage(3);
        }
        [ContextMenu("KillMeee")]
        public void KillMeee()
        {
            Damage(30);
        }

        /// <summary>
        /// Damages the pawn with a sling shot
        /// </summary>
        /// <param name="amount"></param>
        public virtual void Damage(int amount)
        {
            if (Object.HasStateAuthority == false)
            {
                Log.Error("Damage Requires state authority");
                return;
            }

            if (isKilled || !IsOnGrid) return;

            Log.Info($"Removing {amount} health points to pawn {name}");
            if (amount <= 0) return;

            if (amount > HealthPoints)
            {
                amount = HealthPoints;
            }

            HealthPoints -= amount;
            RPC_PlayDamageAnim(HealthPoints, amount);
            if (HealthPoints <= 0)
            {
                Kill();
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_PlayDamageAnim(int healthPoint, int amount)
        {
            DamageAnim(healthPoint, amount);
            InternalPlayAudio(looseHpAudioClips);
        }

        protected virtual void DamageAnim(int hp, int amount)
        {
            Log.Info(name + $" took {amount} damages. Now has {hp}");
            pawnVisualController.AnimateDamageVFX();
        }

        /// <summary>
        /// Plays a static damage animation (no cell movement)</param>
        /// </summary>
        public virtual void PlayStaticDamageAnim()
        {
            RPC_PlayStaticDamageAnim();
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_PlayStaticDamageAnim()
        {
            if (pawnAnimator != null)
            {
                pawnAnimator.StaticDamage();
            }
        }

        /// <summary>
        /// Damages the pawn by a specified amount due to lava.
        /// </summary>
        /// <param name="amount">The amount of damage to apply.</param>
        public virtual void DamageLava(int amount)
        {
            if (Object.HasStateAuthority == false)
            {
                Log.Error("Requires state authority");
                return;
            }

            Log.Debug($"DamageLava amount {amount} on Pawn {name} previous HP:{HealthPoints}");

            RPC_TouchLavaAnim();
            Damage(amount);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        protected void RPC_TouchLavaAnim()
        {
            InternalPlayAudio(fallInWaterAudioClips, waterAudioGroup);
            PlaySmokeTrailVFX();
            StartCoroutine(TouchLavaAnim());
        }

        protected virtual IEnumerator TouchLavaAnim()
        {
            // TODO when slingshot animations will be implemented, see how to refactor this
            yield return null;
        }

        #endregion

        #region Death

        /// <summary>
        /// Removes and destroys the pawn from the game. Called by state authority
        /// </summary>
        protected void Kill()
        {
            if (Object.HasStateAuthority == false)
            {
                Log.Error("Requires state authority");
                return;
            }

            Log.Debug($"Kill Pawn {name}");

            RPC_PlayDeathAnim();
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_PlayDeathAnim()
        {
            isKilled = true;
            StartCoroutine(PlayDeathAnim());
        }

        protected virtual IEnumerator PlayDeathAnim()
        {
            yield return WaitForAnotherAnimation();
            Log.Info(name + " is killed");

            if (pawnAnimator != null)
            {
                animationsInProgress++;
                pawnAnimator.Dead = true;
                pawnAnimator.ResetHitFloor();

                if (CurrentCell == null)
                {
                    pawnAnimator.InLava = true;
                    yield return FallIntoWater();
                }
                else
                {
                    if (CurrentCell.Height < 0)
                    {
                        pawnAnimator.InLava = true;
                    }

                    // Falls to the cell
                    var fallHeight = pawnAnimator.PushedMaxHeight - CurrentCellRenderer.transform.localPosition.y;
                    var fallingDuration = fallHeight / pawnAnimator.FallingSpeed;
                    yield return MoveAnim(CurrentCellRenderer.transform, fallingDuration, pawnAnimator.FallingCurve);

                    pawnAnimator.HitFloor();
                }

                InternalPlayAudio(dieAudioClips);
                StopHighlightVFX();
                yield return pawnAnimator.WaitForEndDeathAnimation();

                animationsInProgress--;
            }
            else
            {
                InternalPlayAudio(dieAudioClips);
            }

            if (CurrentCell != null)
            {
                CurrentCell = null;
            }
        }

        /// <summary>
        /// Animation of the kodama falling and dying into water.
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerator FallIntoWater()
        {
            var yOffset = pawnAnimator.PushedMaxHeight - pawnAnimator.DeathInWaterHeight;
            var fallingDuration = yOffset / pawnAnimator.FallingSpeed;
            var start = transform.localPosition;
            var end = transform.localPosition - new Vector3(0, yOffset, 0);

            // Falls into water
            yield return Tweens.Lerp(start, end, fallingDuration, pawnAnimator.FallingCurve, p => transform.localPosition = p);

            pawnAnimator.HitFloor();
        }

        #endregion

        #region Pushback

        /// <summary>
        /// Pushback the pawn on another tile
        /// </summary>
        /// <param name="dest"></param>
        /// <param name="index"></param>
        /// <param name="direction"></param>
        public void Pushback(Vector3Int dest, int index, Vector3 direction)
        {
            if (Object.HasStateAuthority == false)
            {
                Log.Error("Requires state authority");
                return;
            }

            // Remove from cell to avoid any lava damage
            CurrentCell = null;

            var outOfBoard = dest == HexGridRenderer.OutOfBoardPosition;

            if (outOfBoard == false)
            {
                // Change position
                Position = dest;
            }

            RPC_Pushback(dest, index, direction);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_Pushback(Vector3Int dest, int index, Vector3 direction)
        {
            Log.Info(name + $" is pushed back to {dest} (index={index} dir={direction})");

            // Free target cell, we suppose we're in a chain of pushback
            if (CurrentCell != null && CurrentCell.Pawn != null)
            {
                CurrentCell.FreeOccupation();
            }

            if (dest != HexGridRenderer.OutOfBoardPosition)
            {
                CurrentCell = TabletopGameManager.Instance.Grid.Get(dest);
            }

            StartCoroutine(PushbackAnim(index, direction));
        }

        private IEnumerator PushbackAnim(int index, Vector3 direction)
        {
            animationsInProgress++;

            // Chain animation by delaying each one
            yield return new WaitForSeconds(0.1f * index);

            PlaySmokeTrailVFX();
            if (pawnAnimator != null)
            {
                pawnAnimator.ResetHitFloor();
            }

            // If the pawn is the one taking directly the shot
            if (index == 0)
            {
                InternalPlayAudio(shootPawnAudioClips);
            }

            InternalPlayAudio(pushbackAudioClips);

            yield return CurrentCellRenderer == null ? PushToVoidAnim(direction) : PushToCellAnim(CurrentCellRenderer);

            StopSmokeTrailVFX();
            animationsInProgress--;
        }

        protected virtual IEnumerator PushToCellAnim(HexCellRenderer dest)
        {
            yield return MoveAnim(dest.transform, Config.pawnPlacementAnimationDuration, Tweens.EaseOut);
            InternalPlayAudio(fallAudioClips);

            if (HasStateAuthority && CurrentCell.LootBox != null)
            {
                MoveToLootbox(CurrentCell.LootBox);
            }
        }

        protected virtual IEnumerator PushToVoidAnim(Vector3 direction)
        {
            var start = transform.localPosition;
            var end = transform.localPosition + direction * Config.pawnEjectionDistance;
            yield return Tweens.Lerp(
                start, end, Config.pawnPlacementAnimationDuration, Tweens.EaseOut,
                p => transform.localPosition = p);
        }

        /// <summary>
        /// Pushed back out of the board
        /// </summary>
        public abstract void OutOfBoard();

        #endregion

        #region OtherAnimations

        /// <summary>
        /// Plays sounds and animations due to tile moving when the pawn isn't shot.
        /// </summary>
        /// <param name="movingUp"></param>
        public void TileMoving(bool movingUp)
        {
            if (movingUp && (pawnAnimator == null || !pawnAnimator.IsActivatingLoot))
            {
                PlayTileGoingUpAudio();
            }

            if (pawnAnimator != null)
            {
                pawnAnimator.TileMoving(movingUp);
            }
        }

        public virtual IEnumerator WinDisappearAnim()
        {
            PlayOnSpawnVFX();
            yield return new WaitForSeconds(winDisappearDelayBeforeScale);

            yield return Tweens.Lerp(
                0, 1, winDisappearScaleDuration, Tweens.EaseOut, p =>
                    visual.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, p));

            visual.transform.localScale = Vector3.zero;
        }

        public IEnumerator WaitForAnotherAnimation()
        {
            while (IsPlayingAnimation) yield return null;
        }

        #endregion

        #region VFX

        private GameObject _dropVFXPrefab => Config.PawnDropVFX;
        private GameObject _spawnVFXPrefab => Config.PawnSpawnVFX;

        private GameObject _smokeTrailVFXPrefab => Config.PawnSmokeTrailVFX;

        private VisualEffect _dropVFX;
        private VisualEffect _spawnVFX;
        private VisualEffect _smokeTrailVFX;

        private GameObject _highlightVFXPrefab =>
            Config.PlayerHighlightVisual(BaseTabletopPlayer.LocalPlayer.Index);

        private VerticalHighlightController _highlightVFX;

        private void PlayLocalVFX(VisualEffect vfx)
        {
            vfx.gameObject.SetActive(true);
            vfx.Play();
        }

        private void StopLocalVFX(VisualEffect vfx)
        {
            vfx.Stop();
            vfx.gameObject.SetActive(false);
        }

        private void InitializeVisualEffects()
        {
            InitializeFX(out _dropVFX, _dropVFXPrefab);
            InitializeFX(out _spawnVFX, _spawnVFXPrefab);
            InitializeFX(out _smokeTrailVFX, _smokeTrailVFXPrefab);
            _smokeTrailVFX.gameObject.transform.position = smokeTrailVisual.transform.position;
            InitializeHighlightVFX();

            RegisterVFXCallbacks();
        }

        private void InitializeFX(out VisualEffect vfx, GameObject prefab)
        {
            vfx = InstantiateVisualEffect(prefab);
            vfx.gameObject.SetActive(false);
        }

        private void InitializeHighlightVFX()
        {
            // the highlight vfx position must be parented to board
            _highlightVFX = Instantiate(
                    _highlightVFXPrefab, transform.position, quaternion.identity, TabletopGameManager.Instance.BoardObjects.transform).
                GetComponent<VerticalHighlightController>();
            _highlightVFX.gameObject.SetActive(false);
            lit = false;
        }

        private VisualEffect InstantiateVisualEffect(GameObject go)
        {
            return Instantiate(go, transform).GetComponent<VisualEffect>();
        }

        // used by Kodama, could be used by slingshot in the future
        protected void PlayOnSpawnVFX() => PlayLocalVFX(_spawnVFX);

        protected void PlayMoveSucceedVFX() => PlayLocalVFX(_dropVFX);

        protected void PlayMoveFailedVFX() => PlayLocalVFX(_dropVFX);

        protected void PlaySmokeTrailVFX() => PlayLocalVFX(_smokeTrailVFX);

        protected void StopSmokeTrailVFX()
        {
            // Here we don't deactivate the VFX, to keep the last particles alive for a few seconds
            _smokeTrailVFX.Stop();
        }

        protected void PlayHighlightVFX()
        {
            if (TabletopGameManager.Instance == null)
                return;

            // this ensures only our own pawns are highlighted
            if (OwnerIndex != BaseTabletopPlayer.LocalPlayer.Index)
                return;

            if (lit)
                return;

            lit = true;
            _highlightVFX.gameObject.SetActive(true);
            _highlightVFX.gameObject.transform.position = GetHighlightPosition() + Owner.Board.transform.up * 0.005f;
            _highlightVFX.EnableHighlight();

            if (TabletopGameManager.Instance.Phase != (byte)TableTopPhase.Shoot)
            {
                InternalPlayAudio(moveHighlightClips);
            }
        }

        protected void StopHighlightVFX()
        {
            if (lit == false)
                return;

            lit = false;
            _highlightVFX.DisableHighlight();
        }

        private void RegisterVFXCallbacks()
        {
            TabletopGameEvents.OnGamePhaseChanged += OnPhaseChanged;
            TabletopGameEvents.OnPawnDragStart += OnDragStart;
            TabletopGameEvents.OnPawnDragCanceled += OnDragCanceled;
            TabletopGameEvents.OnGameOver += OnGameOver;
        }

        private void UnregisterVFXCallbacks()
        {
            TabletopGameEvents.OnGamePhaseChanged -= OnPhaseChanged;
            TabletopGameEvents.OnPawnDragStart -= OnDragStart;
            TabletopGameEvents.OnPawnDragCanceled -= OnDragCanceled;
            TabletopGameEvents.OnGameOver -= OnGameOver;
        }

        private Vector3 GetHighlightPosition()
        {
            if (IsOnGrid == false && this is Slingshot s)
            {
                return Owner.Board.GetSlot(s.BoardSlot - 1).position;
            }

            return CurrentCellRenderer != null ? CurrentCellRenderer.transform.position : transform.position;
        }

        private bool CheckAllowInteraction(TableTopPhase phase)
        {
            return (phase == TableTopPhase.Move && this is Kodama)
                   || (phase is TableTopPhase.Shoot && this is Slingshot && _currentCell != null && IsOnGrid)
                   || (phase is TableTopPhase.Summon && this is Slingshot && stateMachine.movement.AllowGrab);
        }

        private bool m_allowInteraction;

        private void OnPhaseChanged(BaseTabletopPlayer player, TableTopPhase phase)
        {
            var localId = BaseTabletopPlayer.LocalPlayer.PlayerId;
            var isAllowed = OwnerId == localId && player.PlayerId == localId;

            m_allowInteraction = isAllowed && CheckAllowInteraction(phase);

            m_allowInteraction.IfElse(PlayHighlightVFX, StopHighlightVFX);
        }

        private void OnDragStart() => m_allowInteraction.IfTrue(StopHighlightVFX);

        private void OnDragCanceled() => m_allowInteraction.IfTrue(PlayHighlightVFX);

        private void OnGameOver(BaseTabletopPlayer player)
        {
            if (player == null || OwnerId == player.PlayerId)
            {
                StopHighlightVFX();
            }
        }

        #endregion

        #region Audio

        protected void InternalPlayAudio(AudioClip clip, AudioMixerGroups audioGroup)
        {
            AudioManager.Instance.Play(clip, audioGroup, transform);
        }

        protected void InternalPlayAudio(AudioClip[] clips)
        {
            InternalPlayAudio(clips, defaultAudioGroup);
        }

        protected void InternalPlayAudio(AudioClip[] clips, AudioMixerGroups audioGroup)
        {
            AudioManager.Instance.PlayRandom(clips, audioGroup, transform);
        }

        protected virtual void PlayTileGoingUpAudio()
        {
            InternalPlayAudio(tileGoingUpAudioClips);
        }

        protected virtual void PlayGrabAudio()
        {
            InternalPlayAudio(grabAudioClips, interactionsAudioGroup);
        }

        #endregion

        #region Other

        /// <summary>
        /// Sets the disabling of the shared mode interpolation if not having the state authority.
        /// </summary>
        /// <param name="disable">true to disable.</param>
        /// <param name="onlyIfNotAuthority">true if the change should be applied only if the player doesn't have the state authority on this pawn.</param>
        public void SetDisableInterpolation(bool disable, bool onlyIfNotAuthority)
        {
            if (!onlyIfNotAuthority || !HasStateAuthority)
            {
                m_networkTransform.DisableSharedModeInterpolation = disable;
            }
        }

        protected virtual void MoveToLootbox(LootBox lootBox)
        {
            if (!HasStateAuthority)
            {
                Log.Error("Requires state authority");
                return;
            }

            TabletopGameEvents.OnLootBoxDestroyed?.Invoke(lootBox);
            lootBox.Kill();
        }

        #endregion
    }
}
