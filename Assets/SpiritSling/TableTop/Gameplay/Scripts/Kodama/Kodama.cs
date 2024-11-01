// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using Fusion;
using UnityEngine;
using UnityEngine.VFX;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// This class is managing each Kodama
    /// </summary>
    [SelectionBase]
    public class Kodama : Pawn
    {
        private const int FALLING_ASLEEP_IDLE_NUMBER = 6;
        private const int LOOKING_IDLE_NUMBER = 7;

        private const int LAUGH_1_REACTION_NUMBER = 1;
        private const int LAUGH_2_REACTION_NUMBER = 2;
        private const int TAUNT_1_REACTION_NUMBER = 3;
        private const int TAUNT_2_REACTION_NUMBER = 4;

        private int m_activateAnim = Animator.StringToHash("Activate");
        private int m_deactivateAnim = Animator.StringToHash("Deactivate");
        /// <summary>
        /// True if any kodama is in kill coroutine. Only used by the state authority.
        /// </summary>
        private static bool inKillCoroutine;

        [Header("Kodama specific")]
        [SerializeField]
        internal VFXHealthPointManager vfxHealthPointManager;

        [SerializeField]
        private AudioClip[] activeSlingAudioClips;

        [SerializeField]
        private AudioClip[] waitingTakenAudioClips;

        [SerializeField]
        private AudioClip[] fallingAsleepAudioClips;

        [SerializeField]
        private AudioClip[] grabVoiceAudioClips;

        [SerializeField]
        private AudioClip[] laughAudioClips;

        [SerializeField]
        private AudioClip[] lookingAudioClips;

        [SerializeField]
        private AudioClip[] tauntAudioClips;

        [Networked, OnChangedRender(nameof(OnImmunityChanged))]
        public NetworkBool IsImmune { get; private set; }

        public delegate void OnSpawnHealthPointsHandler();

        private LeafAnimator leafAnimator;
        private bool isInCutscene;
        private Coroutine waitAndPlayIdle;

        private VisualEffect _spawnVFX2;

        private GameObject PlayerKodamaVisual => Config.PlayerKodamaVisual(OwnerIndex);

        #region Spawn

        public override void Spawned()
        {
            Instantiate(PlayerKodamaVisual, visual.transform);

            base.Spawned();
            leafAnimator = GetComponentInChildren<LeafAnimator>();
            Reset();

            // A kodama always starts on the grid
            IsOnGrid = true;
            CurrentCell = TabletopGameManager.Instance.Grid.Get(Position);
            CurrentCellRenderer.OnCellOccupied(CurrentCell);

            if (isInCutscene == false)
            {
                SetCutsceneMode(false, false); // Prepare for start cutscene
            }

            Log.Debug("Kodama Spawned " + gameObject.name + $" owner={OwnerId}");

            foreach (var p in BaseTabletopPlayer.TabletopPlayers)
            {
                if (OwnerId == p.PlayerId)
                {
                    p.Kodama = this;
                    break;
                }
            }

            TabletopGameManager.Instance.Kodamas.Add(this);
            TabletopGameEvents.OnGamePhaseChanged += ShowAllHealthPoints;
            TabletopGameEvents.OnGamePhaseChanged += OnGamePhaseChanged;
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (_spawnVFX2 != null)
                Destroy(_spawnVFX2.gameObject);

            base.Despawned(runner, hasState);

            if (TabletopGameManager.Instance != null)
            {
                TabletopGameManager.Instance.Kodamas.Remove(this);
            }

            var kodamaOwner = Owner;
            if (kodamaOwner != null)
            {
                kodamaOwner.Kodama = null;
            }
        }

        private void OnDestroy()
        {
            TabletopGameEvents.OnGamePhaseChanged -= ShowAllHealthPoints;
            TabletopGameEvents.OnGamePhaseChanged -= OnGamePhaseChanged;
        }

        /// <summary>
        /// Reset the initial state and variables of a kodama
        /// </summary>
        private void Reset()
        {
            HealthPoints = maxHealthPoints = TabletopGameManager.Instance.Settings.kodamaStartHealthPoints;
            IsImmune = true;
        }

        /// <summary>
        /// activate all current Healthpoint
        /// </summary>
        private void ShowAllHealthPoints(BaseTabletopPlayer player, TableTopPhase phase)
        {
            vfxHealthPointManager.ShowAllHealthPoints();
        }


        /// <summary>
        /// Prepare for cutscenes (disable many scripts) or not
        /// </summary>
        public void SetCutsceneMode(bool enableVisuals, bool enableGameplay)
        {
            visual.SetActive(enableVisuals);

            isInCutscene = !enableGameplay;

            if (isInCutscene == false)
            {
                // Ensures that the immunity animation is up-to-date with the starting state
                // Except for first player: immunity will be removed immediately
                if (OwnerId != BaseTabletopPlayer.FirstPlayer.PlayerId)
                {
                    OnImmunityChanged();
                }
            }
        }

        public void PlaySpawnVFX()
        {
            _spawnVFX2 = Instantiate(Config.PawnSpawnVFX, transform.position, Quaternion.identity).
                GetComponent<VisualEffect>();
            _spawnVFX2.Play();
            vfxHealthPointManager.SpawnHealthPoints();
        }

        #endregion

        #region Damage

        /// <summary>
        /// Reset the immunity of the Kodama
        /// </summary>
        public void ResetImmunity()
        {
            if (HasStateAuthority && isKilled == false)
            {
                IsImmune = false;
            }
        }

        private void OnImmunityChanged()
        {
            pawnAnimator.Immune = IsImmune;
        }

        private void OnGamePhaseChanged(BaseTabletopPlayer player, TableTopPhase phase)
        {
            if (!HasStateAuthority)
            {
                return;
            }

            if (phase == TableTopPhase.Move)
            {
                // If the kodama is immune or its owner turn begins, sets its idle to default
                var resetIdle = IsImmune || OwnerId == player.PlayerId;
                var newIdle = resetIdle ? 0 : Random.Range(0, 8);
                if (pawnAnimator.Idle != newIdle)
                {
                    RPC_SetIdle(newIdle, resetIdle ? 0 : Random.Range(0.2f, 1f));
                }
            }
        }

        public override void Damage(int amount)
        {
            if (!IsImmune)
            {
                base.Damage(amount);
                IsImmune = !isKilled;
            }
        }

        protected override void DamageAnim(int newHealthPoints, int amount)
        {
            if (vfxHealthPointManager != null)
            {
                vfxHealthPointManager.Damage(newHealthPoints, amount);
            }
            base.DamageAnim(newHealthPoints, amount);
        }

        public override void PlayStaticDamageAnim()
        {
            if (!IsImmune)
            {
                base.PlayStaticDamageAnim();
            }
        }

        /// <summary>
        /// Coroutine for the heal animation when the pawn is healed.
        /// </summary>
        /// <returns>Enumerator for the coroutine.</returns>
        protected override IEnumerator HealthGainAnim(int newPointsCount, int gain)
        {
            animationsInProgress++;

            if (vfxHealthPointManager != null)
                vfxHealthPointManager.Heal(newPointsCount);

            animationsInProgress--;

            yield return base.HealthGainAnim(newPointsCount, gain);
        }

        public override void DamageLava(int amount)
        {
            if (IsImmune == false)
            {
                // Kodama doesn't takes damages if immune
                base.DamageLava(amount);
            }
            else
            {
                RPC_TouchLavaAnim();
            }

            // It will respawn the pawn on a safe place anyway
            if (isKilled == false)
            {
                Respawn();
            }
        }

        protected override IEnumerator TouchLavaAnim()
        {
            animationsInProgress++;

            // Hides the leaf in case the kodama has fallen into water
            leafAnimator.HideLeaf();

            pawnAnimator.InLava = true;
            yield return pawnAnimator.WaitForGoingInAir();

            // Moves up
            var yOffset = pawnAnimator.PushedMaxHeight - transform.localPosition.y;
            var start = transform.localPosition;
            var end = transform.localPosition + new Vector3(0, yOffset, 0);
            yield return Tweens.Lerp(
                start, end, pawnAnimator.GoingInAirDuration, Tweens.EaseOut, p => transform.localPosition = p);

            animationsInProgress--;
        }

        public void ImpactShield()
        {
            RPC_ImpactShield();
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_ImpactShield()
        {
            if (IsImmune)
            {
                leafAnimator.ImpactShield();
            }
        }
        #endregion

        #region Pushback

        protected override IEnumerator PushToCellAnim(HexCellRenderer dest)
        {
            // Check for lootbox as soon as possible because it will be removed from cell when picked up by state authority
            var pushedBackOnLootbox = CurrentCell.LootBox != null;
            if (pushedBackOnLootbox)
            {
                pawnAnimator.ActivateLoot(PawnAnimator.ACTIVATE_LOOT_PLACEHOLDER);
            }

            pawnAnimator.Hit();
            yield return pawnAnimator.WaitAfterPushedBack;

            var destTransform = dest.transform;
            var yOffset = pawnAnimator.PushedMaxHeight - destTransform.localPosition.y;

            // Moves up and above the destination cell
            yield return MoveAnim(
                destTransform, pawnAnimator.MovePushBackDuration, Tweens.EaseOut, new Vector3(0, yOffset, 0));

            if (HealthPoints <= 0)
            {
                yield break;
            }

            // Falls to the destination cell
            var fallingDuration = yOffset / pawnAnimator.FallingSpeed;
            yield return MoveAnim(destTransform, fallingDuration, pawnAnimator.FallingCurve);
            if (!IsImmune)
            {
                leafAnimator.HideLeaf();
            }

            if (CurrentCell.Height >= 0)
            {
                InternalPlayAudio(fallAudioClips);
            }

            if (pushedBackOnLootbox && HasStateAuthority)
            {
                MoveToLootbox(CurrentCell.LootBox);
            }

            pawnAnimator.HitFloor();
        }

        protected override IEnumerator PushToVoidAnim(Vector3 direction)
        {

            pawnAnimator.Hit();
            yield return pawnAnimator.WaitAfterPushedBack;

            // Moves up and above the void
            var yOffset = pawnAnimator.PushedMaxHeight - transform.localPosition.y;
            var start = transform.localPosition;
            var end = transform.localPosition + direction * Config.pawnEjectionDistance + new Vector3(0, yOffset, 0);
            yield return Tweens.Lerp(
                start, end, pawnAnimator.MovePushBackDuration, Tweens.EaseOut, p => transform.localPosition = p);

            if (HealthPoints <= 0)
            {
                yield break;
            }

            // Falls to the water
            yOffset = pawnAnimator.PushedMaxHeight - pawnAnimator.WaterHeight;
            start = transform.localPosition;
            end = transform.localPosition - new Vector3(0, yOffset, 0);
            yield return Tweens.Lerp(
                start, end, yOffset / pawnAnimator.FallingSpeed, pawnAnimator.FallingCurve, p => transform.localPosition = p);

            pawnAnimator.InLava = true;
            pawnAnimator.HitFloor();
        }

        #endregion

        #region Death

        protected override IEnumerator PlayDeathAnim()
        {
            TabletopGameEvents.OnKodamaDeath?.Invoke(Owner);

            yield return base.PlayDeathAnim();

            if (HasStateAuthority && isKilled)
            {
                yield return KillCoroutine();
            }
        }

        private IEnumerator KillCoroutine()
        {
            // This method mustn't be executed twice during the same time to avoid having all players loose the game
            while (inKillCoroutine)
            {
                yield return null;
            }

            Log.Info(name + " is in KillCoroutine");
            animationsInProgress++;
            inKillCoroutine = true;

            // Kodama's owner (!= State Authority !)
            var kodamaOwner = Owner;

            // If the remaining kodamas die in the same action, the winner will have its kodama killed, but shouldn't be defeated
            if (!kodamaOwner.IsWinner)
            {
                var isCurrentPlayer = kodamaOwner == TabletopGameManager.Instance.CurrentPlayer;
                var isFirstPlayer = kodamaOwner == BaseTabletopPlayer.FirstPlayer;

                // -- Mark player as defeated
                kodamaOwner.RPC_SetDefeated();
                while (BaseTabletopPlayer.TabletopPlayers.Contains(kodamaOwner))
                {
                    yield return null; // Wait for Game Over to be propagated    
                }

                // Killed during its own turn: end it now if nobody won
                if (isCurrentPlayer && !TabletopGameManager.Instance.HasSomeoneWon())
                {
                    // End the turn (we have authority over board here)
                    TabletopGameManager.Instance.SetNextPlayer();
                    // If the current player is the first one, force no new round for the next player to avoid having two new rounds in a row
                    TabletopGameManager.Instance.RPC_NextTurn(TabletopGameManager.Instance.CurrentPlayerIndex, isFirstPlayer);
                }

                // Clearing the pawns will affect this Kodama, so do it at the end of the coroutine.
                kodamaOwner.ClearPawns();
            }

            inKillCoroutine = false;
            animationsInProgress--;
        }

        #endregion

        #region Respawn

        /// <summary>
        /// Respawn the pawn on a safe place
        /// </summary>
        protected void Respawn()
        {
            if (Object.HasStateAuthority == false)
            {
                Log.Error("Requires authority");
                return;
            }

            if (isKilled) return;

            var cell = TabletopGameManager.Instance.GetSafeRespawnCell(this);

            // Ensures that this cell won't be selected as a respawn cell for another kodama until this one has moved or died
            cell.WillBeOccupiedByPawn = true;

            Position = cell.Position;
            RPC_PlayRespawnAnim(cell.Position);
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_PlayRespawnAnim(Vector3Int safePosition)
        {
            Log.Info($"{gameObject.name} respawn at {safePosition}");
            var safeCell = TabletopGameManager.Instance.Grid.Get(safePosition);
            StartCoroutine(RespawnAnim(safeCell));
        }

        private IEnumerator RespawnAnim(HexCell cell)
        {
            yield return WaitForAnotherAnimation();

            animationsInProgress++;
            pawnAnimator.ResetHitFloor();

            CurrentCell = cell;
            var respawnTarget = CurrentCellRenderer.transform;
            var yOffset = pawnAnimator.PushedMaxHeight - respawnTarget.localPosition.y;

            // Moves above the respawn cell while keeping the same height
            var aboveRespawnTarget = respawnTarget.transform.position + new Vector3(0, yOffset, 0);
            var horizontalMoveDuration =
                Vector3.Distance(transform.position, aboveRespawnTarget) / pawnAnimator.HorizontalSpeed;

            yield return MoveAnim(respawnTarget, horizontalMoveDuration, Tweens.EaseOut, new Vector3(0, yOffset, 0));

            // Falls to the respawn cell
            var fallingDuration = yOffset / pawnAnimator.FallingSpeed;
            yield return MoveAnim(respawnTarget, fallingDuration, pawnAnimator.FallingCurve);

            if (cell.Height >= 0)
            {
                pawnAnimator.InLava = false;
            }
            pawnAnimator.HitFloor();
            InternalPlayAudio(fallAudioClips);

            PlayOnSpawnVFX();

            if (vfxHealthPointManager != null)
                vfxHealthPointManager.RepositionHealthPoints(HealthPoints);

            // If the kodama respawned in water
            if (cell.Height < 0 && HasStateAuthority)
            {
                DamageLava(TabletopGameManager.Instance.Settings.lavaDamage);
            }

            animationsInProgress--;
        }

        public override void OutOfBoard()
        {
            Log.Info("Kodama is out of board");
            DamageLava(TabletopGameManager.Instance.Settings.lavaDamage);
        }

        #endregion

        #region Audio

        protected override void PlayTileGoingUpAudio()
        {
            InternalPlayAudio(tileGoingUpAudioClips, AudioMixerGroups.SFX_KodamaVoices);
        }

        protected override void PlayGrabAudio()
        {
            InternalPlayAudio(grabAudioClips, AudioMixerGroups.UI_Kodama);
            InternalPlayAudio(grabVoiceAudioClips, AudioMixerGroups.SFX_KodamaVoices);
        }

        #endregion

        #region Animation

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_SetIdle(int newIdle, float delay)
        {
            if (waitAndPlayIdle != null)
            {
                StopCoroutine(waitAndPlayIdle);
            }
            waitAndPlayIdle = StartCoroutine(WaitAndPlayIdle(newIdle, delay));
        }

        private IEnumerator WaitAndPlayIdle(int newIdle, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (newIdle == FALLING_ASLEEP_IDLE_NUMBER)
            {
                InternalPlayAudio(fallingAsleepAudioClips, AudioMixerGroups.SFX_KodamaVoices);
            }
            else if (newIdle == LOOKING_IDLE_NUMBER)
            {
                InternalPlayAudio(lookingAudioClips, AudioMixerGroups.SFX_KodamaVoices);
            }

            pawnAnimator.Idle = newIdle;
            waitAndPlayIdle = null;
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_OnSummon()
        {
            InternalPlayAudio(activeSlingAudioClips, AudioMixerGroups.SFX_KodamaVoices);
            if (!IsImmune)
            {
                pawnAnimator.ActivateSling();
            }
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_EncouragePlayer(bool encourage)
        {
            if (encourage)
            {
                InternalPlayAudio(waitingTakenAudioClips, AudioMixerGroups.SFX_KodamaVoices);
            }

            pawnAnimator.EncouragePlayer = encourage;
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        public void RPC_SetReaction(int reactionNumber)
        {
            switch (reactionNumber)
            {
                case LAUGH_1_REACTION_NUMBER:
                case LAUGH_2_REACTION_NUMBER:
                    InternalPlayAudio(laughAudioClips, AudioMixerGroups.SFX_KodamaVoices);
                    break;

                case TAUNT_1_REACTION_NUMBER:
                case TAUNT_2_REACTION_NUMBER:
                    InternalPlayAudio(tauntAudioClips, AudioMixerGroups.SFX_KodamaVoices);
                    break;
            }

            pawnAnimator.ReactionNumber = reactionNumber;
        }

        private IEnumerator PlaySummonFx(GameObject inst)
        {
            animationsInProgress++;
            var anim = inst.GetComponent<Animator>();
            anim.SetTrigger(m_activateAnim);
            yield return new WaitForSeconds(2f);

            anim.SetTrigger(m_deactivateAnim);
            yield return new WaitForSeconds(5f);

            Destroy(inst);
            animationsInProgress--;
        }

        public void UseLootBox(LootItem.Types lootType, GameObject inst)
        {
            // Ensures that the leaf is hidden before activating loot
            leafAnimator.HideLeaf();

            if(inst!=null)
                StartCoroutine(PlaySummonFx(inst));

            var lootId = lootType switch
            {
                LootItem.Types.Health => 1,
                LootItem.Types.Health_Mega => 1,
                LootItem.Types.Impact => 2,
                LootItem.Types.Impact_Mega => 2,
                LootItem.Types.HeightUp => 3,
                LootItem.Types.HeightUp_Mega => 3,
                LootItem.Types.HeightDown => 4,
                LootItem.Types.HeightDown_Mega => 4,
                _ => 0
            };

            pawnAnimator.ActivateLoot(lootId, lootType.IsStrong());
        }

        #endregion

        #region Other

        protected override void MoveToLootbox(LootBox lootBox)
        {
            if (!HasStateAuthority)
            {
                Log.Error("Requires state authority");
                return;
            }

            TabletopGameEvents.OnLootBoxPickedUp?.Invoke(lootBox, this);
            lootBox.Deactivate();
        }

        #endregion
    }
}