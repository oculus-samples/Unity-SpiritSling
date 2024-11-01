// Copyright (c) Meta Platforms, Inc. and affiliates.

using Fusion;
using UnityEngine;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// Represents a loot box in the tabletop game.
    /// </summary>
    public class LootBox : NetworkBehaviour
    {
        [SerializeField]
        private AudioClip spawnAudioClip;

        [SerializeField]
        private AudioClip destroyAudioClip;

        [SerializeField]
        private AudioClip warningAudioClip;

        [SerializeField]
        private AudioClip pickUpAudioClip;

        [SerializeField]
        private AudioClip powerAudioClip;

        /// <summary>
        /// The type of loot contained in the box.
        /// </summary>
        [Networked]
        public LootItem.Types LootType { get; set; }

        /// <summary>
        /// The ID of the player owning the object. This may not be the authority.
        /// </summary>
        [Networked]
        public int OwnerId { get; set; }

        /// <summary>
        /// The networked position of the object on the grid, triggering a change handler when modified.
        /// </summary>
        [Networked]
        public Vector3Int Position { get; protected set; }

        /// <summary>
        /// The health points of the pawn, which are networked and trigger a change handler.
        /// </summary>
        [Networked]
        public int HealthPoints
        {
            get => healthPoints;
            protected set
            {
                var val = Mathf.Clamp(value, 0, maxHealthPoints);
                healthPoints = val;
            }
        }

        /// <summary>
        /// Gets or sets the current hex cell the pawn is occupying.
        /// When set, it updates the cell's occupancy and attaches the pawn to the cell.
        /// </summary>
        public HexCell CurrentCell
        {
            get => currentCell;
            set
            {
                if (currentCell != null)
                {
                    currentCell.LootBox = null;
                }

                currentCell = value;

                if (currentCell != null)
                {
                    currentCell.LootBox = this;
                }
            }
        }

        [Networked]
        public bool IsKilled { get; set; }

        /// <summary>
        /// Indicates with link VFX Where is the target
        /// </summary>
        public bool showLinkVfx=false;

        /// <summary>
        /// The current hex cell that the pawn occupies.
        /// </summary>
        private HexCell currentCell;

        private NetworkTransform m_networkTransform;

        /// <summary>
        /// Current Health Points
        /// </summary>
        private int healthPoints;

        private int maxHealthPoints;
        private Animator anim;
        private bool initialized;
        private int m_activateAnim = Animator.StringToHash("Activate");
        private int m_deactivateAnim = Animator.StringToHash("Deactivate");
        private int m_destructionAnim = Animator.StringToHash("Destruction");
        private int m_warningAnim = Animator.StringToHash("Warning");

        /// <summary>
        /// Reset the initial state and variables
        /// </summary>
        private void Reset()
        {
            HealthPoints = maxHealthPoints = TabletopGameManager.Instance.Settings.lootboxStartHealthPoints;
        }

        /// <summary>
        /// Initializes the position of the loot box before it is spawned.
        /// </summary>
        /// <param name="spawnCellPosition">The position to spawn the loot box at.</param>
        public void InitalizePosition(Vector3Int spawnCellPosition)
        {
            Position = spawnCellPosition;
            Init();
        }

        private void Awake()
        {
            anim = GetComponent<Animator>();
        }

        /// <summary>
        /// Initializes the loot box on start.
        /// </summary>
        public void Start()
        {
            Init();
            anim.SetTrigger(m_activateAnim);
        }

        /// <summary>
        /// Handles the despawning of the loot box, de-referencing it in the loot manager.
        /// </summary>
        /// <param name="runner">The network runner.</param>
        /// <param name="hasState">Indicates whether the object has state.</param>
        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            LootsManager.Instance.DereferenceLootBox(this);
            base.Despawned(runner, hasState);
        }

        public override void Spawned()
        {
            base.Spawned();
            Init();
        }

        /// <summary>
        /// Initializes the loot box and sets its properties.
        /// </summary>
        private void Init()
        {
            if (initialized)
                return;

            if (LootsManager.Instance != null)
            {
                LootsManager.Instance.ReferenceLootBox(this, CurrentCell);
            }

#if UNITY_EDITOR
            name = $"LootBox_{LootType}";
#endif
            if (TabletopGameManager.Instance != null)
            {
                if (TabletopGameManager.Instance.Grid != null)
                {
                    CurrentCell = TabletopGameManager.Instance.Grid.Get(Position);
                    transform.SetParent(TabletopGameManager.Instance.GridRenderer.transform, true);
                    transform.localPosition = TabletopGameManager.Instance.GridRenderer.GetCell(CurrentCell).transform.localPosition;
                }
            }

            initialized = true;
            Reset();
            AudioManager.Instance.Play(spawnAudioClip, AudioMixerGroups.SFX_Loot, transform);

            m_networkTransform = GetComponent<NetworkTransform>();
        }

        #region Damages

        /// <summary>
        /// Applies lava damage on the lootbox if needed.
        /// </summary>
        public void CheckForLava()
        {
            if (CurrentCell == null)
            {
                Log.Error($"Lootbox currentcell is null on {gameObject.name}");
                return;
            }

            if (CurrentCell.Height < 0)
            {
                DamageLava(TabletopGameManager.Instance.Settings.lootboxDamage);
            }
        }

        /// <summary>
        /// Damages the pawn by a specified amount due to lava.
        /// </summary>
        /// <param name="amount">The amount of damage to apply.</param>
        public void DamageLava(int amount)
        {
            if (Object.HasStateAuthority == false)
            {
                Log.Error("Requires state authority");
                return;
            }

            Log.Info($"[LOOT] DamageLava amount {amount} on Lootbox {name} previous HP:{HealthPoints}");

            if (IsKilled) return;

            Damage(amount);
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

            Log.Info($"Removing {amount} health points to lootbox {name}");
            if (amount <= 0) return;

            HealthPoints -= amount;

            if (HealthPoints <= 0)
            {
                Kill();
            }
        }
        #endregion

        #region Death

        /// <summary>
        /// Removes and destroys the pawn from the game. Called by state authority
        /// </summary>
        public virtual void Kill()
        {
            if (Object.HasStateAuthority == false)
            {
                Log.Error("Requires state authority");
                return;
            }

            IsKilled = true;

            RPC_PlayDeathAnim();
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_PlayDeathAnim()
        {
            Log.Info(name + " is killed");
            CurrentCell = null;
            anim.SetTrigger(m_destructionAnim);
            AudioManager.Instance.Play(destroyAudioClip, AudioMixerGroups.SFX_Loot, transform);
            LootsManager.Instance.DereferenceLootBox(this);
        }

        #endregion

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

        #region Warning

        /// <summary>
        /// Plays the warning sound and animation or stops the warning animation.
        /// </summary>
        public void SetWarning(bool warning)
        {
            anim.SetBool(m_warningAnim, warning);
            if (warning)
            {
                AudioManager.Instance.Play(warningAudioClip, AudioMixerGroups.SFX_Loot, transform);
            }
        }

        #endregion

        #region Deactivation

        /// <summary>
        /// Removes and destroys the pawn from the game. Called by state authority
        /// </summary>
        public virtual void Deactivate()
        {
            if (Object.HasStateAuthority == false)
            {
                Log.Error("Requires state authority");
                return;
            }

            IsKilled = true;

            RPC_PlayDeactivateAnim();
        }

        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_PlayDeactivateAnim()
        {
            Log.Info(name + " is Deactivated");
            CurrentCell = null;
            anim.SetTrigger(m_deactivateAnim);
            AudioManager.Instance.Play(pickUpAudioClip, AudioMixerGroups.UI_Gameplay);
            AudioManager.Instance.Play(powerAudioClip, AudioMixerGroups.UI_Powers);
            LootsManager.Instance.DereferenceLootBox(this);
        }

        #endregion

        #region Animation Triggers

        private void Update()
        {
            if (anim.GetCurrentAnimatorStateInfo(0).shortNameHash == (m_deactivateAnim) &&
                anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f)
            {
                OnDeactivationAnimationEnd();
            }
            else if (anim.GetCurrentAnimatorStateInfo(0).shortNameHash == (m_destructionAnim) &&
                     anim.GetCurrentAnimatorStateInfo(0).normalizedTime >= 1.0f)
            {
                OnDestructionAnimationEnd();
            }
        }

        public void OnDeactivationAnimationEnd()
        {
            Destroy(gameObject);
        }

        public void OnDestructionAnimationEnd()
        {
            Destroy(gameObject);
        }

        #endregion
    }
}