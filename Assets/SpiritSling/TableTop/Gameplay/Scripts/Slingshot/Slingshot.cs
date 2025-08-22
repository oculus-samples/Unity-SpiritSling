// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using System.Collections;
using Fusion;
using UnityEngine;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// Manages the behavior and state of each Slingshot object in the tabletop game.
    /// </summary>
    [MetaCodeSample("SpiritSling")]
    [SelectionBase]
    public class Slingshot : Pawn, IAfterSpawned
    {
        /// <summary>
        /// The MeshRenderer component used to set the color of the Slingshot and mask when killed.
        /// </summary>
        [SerializeField]
        private MeshRenderer bodyRenderer;

        [SerializeField]
        private SlingBall slingBall;

        public SlingBall SlingBall => slingBall;

        /// <summary>
        /// The MeshRenderer component used to mask when killed.
        /// </summary>
        [SerializeField]
        private MeshRenderer baseRenderer;

        [SerializeField]
        private GameObject slingHealthPointsGlow;

        [SerializeField]
        private AudioClip[] respawnAudioClips;

        [SerializeField]
        private VFXController vfxController;

        /// <summary>
        /// Networked property indicating the current board slot of the Slingshot. 
        /// </summary>
        [Networked]
        public int BoardSlot { get; set; }

        /// <summary>
        /// Gets the color of the Slingshot based on the player's ID.
        /// </summary>
        public Color Color => TabletopConfig.Get().PlayerColor(OwnerIndex);

        public override bool IsPlayingAnimation => base.IsPlayingAnimation && IsOnGrid;

        public override bool IsOnGrid
        {
            get => _isOnGrid;
            protected set
            {
                if (_isOnGrid != value)
                {
                    _isOnGrid = value;
                    UpdateHealthVfx();
                }
            }
        }

        /// <summary>
        /// Called when the Slingshot is spawned in the game. Initializes its state and registers it with the game manager.
        /// </summary>
        public override void Spawned()
        {
            base.Spawned();

            slingBall = GetComponentInChildren<SlingBall>(true);
            Owner.Slingshots.Add(this);
            TabletopGameManager.Instance.Slingshots.Add(this);
            Reset();

            // Set the Slingshot's material color based on the player's color.
            bodyRenderer.material.color = Color;
            // The slingshot is hidden until moved to a board slot
            bodyRenderer.enabled = false;
            baseRenderer.enabled = false;
        }

        public void AfterSpawned()
        {
            // Moves to the board slot in AfterSpawned to be sure that the owner player's board has done its Spawned call
            StartCoroutine(MoveToBoardSlot(true));
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            base.Despawned(runner, hasState);

            if (TabletopGameManager.Instance != null)
                TabletopGameManager.Instance.Slingshots.Remove(this);

            var slingshotOwner = Owner;
            if (slingshotOwner != null)
            {
                slingshotOwner.Slingshots.Remove(this);
            }
        }

        /// <summary>
        /// Resets the Slingshot to its initial state, including health points.
        /// </summary>
        private void Reset()
        {
            isKilled = false;
            HealthPoints = maxHealthPoints = TabletopGameManager.Instance.Settings.slingshotStartHealthPoints;
        }

        public override void ResetPosition(bool waitForPawnAnimation = false, bool hitFloor = false)
        {
            if (IsOnGrid)
            {
                base.ResetPosition(waitForPawnAnimation, hitFloor);
            }
            else
            {
                StartCoroutine(MoveToBoardSlot(false));
            }
        }

        private void UpdateHealthVfx()
        {
            if (!IsOnGrid)
            {
                Log.Debug("[SLINGSHOT] UpdateHealthVfx: is not on grid");
                vfxController.Trigger();
            }
            else if (HealthPoints == maxHealthPoints)
            {
                Log.Debug("[SLINGSHOT] UpdateHealthVfx: max health");
                vfxController.Activate();
            }
            else if (HealthPoints == 1)
            {
                Log.Debug("[SLINGSHOT] UpdateHealthVfx: 1 health point");
                vfxController.Deactivate();
            }
            else
            {
                Log.Debug("[SLINGSHOT] UpdateHealthVfx : death");
                vfxController.Trigger();
                vfxController.TriggerSpecial();
            }
        }

        protected override void DamageAnim(int hp, int amount)
        {
            base.DamageAnim(hp, amount);
            UpdateHealthVfx();
        }

        protected override IEnumerator HealthGainAnim(int newPointsCount, int gain)
        {
            yield return base.HealthGainAnim(newPointsCount, gain);
            UpdateHealthVfx();
        }

        public override void DamageLava(int amount)
        {
            // Lava instakill slingshots
            base.DamageLava(9999);
        }
        protected override IEnumerator TouchLavaAnim()
        {
            yield return null;
            StopSmokeTrailVFX();
        }

        protected override IEnumerator PlayDeathAnim()
        {
            yield return base.PlayDeathAnim();
            yield return MoveToBoardSlot(true);
        }

        /// <summary>
        /// Remove slingshot from the grid on put it back on player board
        /// </summary>
        public void SendBackToBoard()
        {
            if (Object.HasStateAuthority == false)
            {
                Log.Error("Requires authority");
                return;
            }

            RPC_SendBackToBoard();
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_SendBackToBoard()
        {
            // Immediately frees the cell
            CurrentCell = null;
            StartCoroutine(MoveToBoardSlot(true));
        }

        /// <summary>
        /// Attaches the Slingshot to its designated board slot.
        /// </summary>
        private IEnumerator MoveToBoardSlot(bool stopHighlightAfterMove)
        {
            animationsInProgress++;
            PlayHighlightVFX();
            yield return null;

            Log.Debug($"Sending Slingshot {name} back to board slot {BoardSlot}");

            if (HealthPoints <= 0)
            {
                //mask sling when dead and goback to playerboard
                bodyRenderer.enabled = false;
                baseRenderer.enabled = false;

                //play death animation
                yield return new WaitForSeconds(2f);
            }

            var boardSlot = Owner.Board.GetSlot(BoardSlot - 1);
            transform.SetPositionAndRotation(boardSlot.position, boardSlot.rotation);
            if (HealthPoints <= 0)
            {
                InternalPlayAudio(respawnAudioClips);
            }

            bodyRenderer.enabled = true;
            baseRenderer.enabled = true;
            Reset();
            IsOnGrid = false;

            if (stopHighlightAfterMove)
            {
                StopHighlightVFX();
            }
            animationsInProgress--;
        }

        public override void OutOfBoard()
        {
            Log.Debug("Slingshot is out of board");
            DamageLava(9999);
        }

        public override IEnumerator WinDisappearAnim()
        {
            StopHighlightVFX();
            PlayOnSpawnVFX();
            yield return new WaitForSeconds(winDisappearDelayBeforeScale);

            slingHealthPointsGlow.SetActive(false);
            yield return Tweens.Lerp(
                0, 1, winDisappearScaleDuration, Tweens.EaseOut, p =>
                {
                    visual.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, p);
                    slingBall.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, p);
                });

            visual.SetActive(false);
            slingBall.gameObject.SetActive(false);
        }
    }
}
