// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace SpiritSling.TableTop
{
    /// <summary> Hexagon display and animations </summary>
    [SelectionBase]
    public class HexCellRenderer : MonoBehaviour
    {
        /// <summary>
        /// To set regarding pawns falling animations duration
        /// </summary>
        public const float UPDATE_HEIGHT_ANIMATION_LENGTH = 1.033f;

        public enum CellState
        {
            Unset,
            Default,
            Selected,
            Range,
            WillHaveCLOC,
            CliffSelected,
            WillDrown,
            RangeCLOC,
            RangeWillDrown,
            SelectedCLOC,
            SelectedWillDrown,
            HasDrowned
        }

        [Header("Renderer")]
        [SerializeField]
        private float yHeight = 0.03f;

        [SerializeField]
        private MeshRenderer meshRenderer;

        [Header("Materials")]
        [SerializeField]
        private Material defaultMaterial;

        [SerializeField]
        private Material selectionMaterial;

        [SerializeField]
        private Material rangeMaterial;

        [SerializeField]
        private Material shakeMaterial;

        [SerializeField]
        private Material willDrownMaterial;

        [SerializeField]
        private Material shakeShootMaterial;

        [SerializeField]
        private Material willDrownShootMaterial;

        [SerializeField]
        private Material shakeSelectedMaterial;

        [SerializeField]
        private Material willDrownSelectedMaterial;

        [SerializeField]
        private Material hasDrownMaterial;

        [SerializeField]
        private Material cliffDefaultMaterial;

        [SerializeField]
        private Material cliffSelectedMaterial;

        [SerializeField]
        private Material cliffShakeMaterial;

        static int _RandomSeedID = Shader.PropertyToID("_RandomSeed");

        /// <summary>
        /// Cell data
        /// </summary>
        public HexCell Cell { get; private set; }

        /// <summary>
        /// Cell State
        /// </summary>
        public CellState State { get; private set; } = CellState.Unset;

        private CellState LastState = CellState.Unset;
        private bool _dirtyState;

        private Coroutine currentAnim;

        private readonly Material[] allMaterials = new Material[2];
        private Material cachedMat = null;
        private static readonly Dictionary<CellState, Material> s_stateMaterialMap = new Dictionary<CellState, Material>();
        private static readonly Dictionary<CellState, Material> s_cliffMaterialMap = new Dictionary<CellState, Material>();

        private static Dictionary<int, Queue<Material>> _materialPools = new();

        private static Material GetMaterial(Material originalMaterial, Vector2 seed)
        {
            int key = originalMaterial.GetInstanceID();

            if (!_materialPools.TryGetValue(key, out var pool))
            {
                pool = new Queue<Material>();
                _materialPools[key] = pool;
            }

            Material material;
            if (pool.Count > 0)
            {
                material = pool.Dequeue();
            }
            else
            {
                material = new Material(originalMaterial);
            }

            if (material.HasProperty(_RandomSeedID))
            {
                // Randomize each cells Materials, we can't use world position, because constantly change in MR
                material.SetVector(_RandomSeedID, seed); //set Seed
            }

            return material;
        }
        private static void ReleaseMaterial(Material material)
        {
            var key = material.shader.GetInstanceID();
            if (!_materialPools.TryGetValue(key, out var pool))
            {
                pool = new Queue<Material>();
                _materialPools[key] = pool;
            }

            pool.Enqueue(material);
        }

        private void Initialize()
        {
            var randomSeed = GetRandomSeed();

            defaultMaterial = GetMaterial(defaultMaterial, randomSeed);
            selectionMaterial = GetMaterial(selectionMaterial, randomSeed);
            rangeMaterial = GetMaterial(rangeMaterial, randomSeed);
            shakeMaterial = GetMaterial(shakeMaterial, randomSeed);
            willDrownMaterial = GetMaterial(willDrownMaterial, randomSeed);
            shakeShootMaterial = GetMaterial(shakeShootMaterial, randomSeed);
            willDrownShootMaterial = GetMaterial(willDrownShootMaterial, randomSeed);
            shakeSelectedMaterial = GetMaterial(shakeSelectedMaterial, randomSeed);
            willDrownSelectedMaterial = GetMaterial(willDrownSelectedMaterial, randomSeed);
            hasDrownMaterial = GetMaterial(hasDrownMaterial, randomSeed);
            cliffDefaultMaterial = GetMaterial(cliffDefaultMaterial, randomSeed);
            cliffSelectedMaterial = GetMaterial(cliffSelectedMaterial, randomSeed);
            cliffShakeMaterial = GetMaterial(cliffShakeMaterial, randomSeed);

            meshRenderer.material = defaultMaterial;
            cachedMat = meshRenderer.material;

            s_stateMaterialMap[CellState.Selected] = selectionMaterial;
            s_stateMaterialMap[CellState.Range] = rangeMaterial;
            s_stateMaterialMap[CellState.CliffSelected] = rangeMaterial;
            s_stateMaterialMap[CellState.WillHaveCLOC] = shakeMaterial;
            s_stateMaterialMap[CellState.WillDrown] = willDrownMaterial;
            s_stateMaterialMap[CellState.RangeCLOC] = shakeShootMaterial;
            s_stateMaterialMap[CellState.RangeWillDrown] = willDrownShootMaterial;
            s_stateMaterialMap[CellState.SelectedCLOC] = shakeSelectedMaterial;
            s_stateMaterialMap[CellState.SelectedWillDrown] = willDrownSelectedMaterial;
            s_stateMaterialMap[CellState.HasDrowned] = hasDrownMaterial;

            s_cliffMaterialMap[CellState.WillHaveCLOC] = cliffShakeMaterial;
            s_cliffMaterialMap[CellState.WillDrown] = cliffShakeMaterial;
            s_cliffMaterialMap[CellState.RangeCLOC] = cliffShakeMaterial;
            s_cliffMaterialMap[CellState.RangeWillDrown] = cliffShakeMaterial;
            s_cliffMaterialMap[CellState.SelectedCLOC] = cliffShakeMaterial;
            s_cliffMaterialMap[CellState.SelectedWillDrown] = cliffShakeMaterial;
            s_cliffMaterialMap[CellState.CliffSelected] = cliffSelectedMaterial;
        }

        private static Vector2 GetRandomSeed() => new(Random.Range(0.0f, 1f), 1.0f);


        private void Start()
        {
            Initialize();

            RegisterCallbacks();
        }

        private void OnGamePhaseChanged(BaseTabletopPlayer player, TableTopPhase phase)
        {
            if (phase == TableTopPhase.Move && TabletopGameManager.Instance.Round == 1)
            {
                if (Cell.IsOccupiedByPawn)
                {
                    AnimateDirtProperty(0, 1, 0.5f);
                }
            }
        }

        private void OnDestroy()
        {
            ReleaseMaterial(defaultMaterial);
            ReleaseMaterial(selectionMaterial);
            ReleaseMaterial(rangeMaterial);
            ReleaseMaterial(shakeMaterial);
            ReleaseMaterial(willDrownMaterial);
            ReleaseMaterial(shakeShootMaterial);
            ReleaseMaterial(willDrownShootMaterial);
            ReleaseMaterial(shakeSelectedMaterial);
            ReleaseMaterial(willDrownSelectedMaterial);
            ReleaseMaterial(hasDrownMaterial);
            ReleaseMaterial(cliffDefaultMaterial);
            ReleaseMaterial(cliffSelectedMaterial);
            ReleaseMaterial(cliffShakeMaterial);

            UnRegisterCallbacks();
        }

        private void RegisterCallbacks()
        {
            HexCell.OnCellOccupied += OnCellOccupied;
            HexCell.OnCellFreed += OnCellFreed;
            TabletopGameEvents.OnGamePhaseChanged += OnGamePhaseChanged;
        }

        private void UnRegisterCallbacks()
        {
            HexCell.OnCellOccupied -= OnCellOccupied;
            HexCell.OnCellFreed -= OnCellFreed;
            TabletopGameEvents.OnGamePhaseChanged -= OnGamePhaseChanged;
        }

        /// <summary> Set cell data and initialize mesh </summary>
        /// <param name="gridRenderer"> </param>
        /// <param name="cell"></param>
        public void SetCell(HexGridRenderer gridRenderer, HexCell cell)
        {
            Cell = cell;

            // Random 60° rotation
            var rotation = gridRenderer.Random.Next(0, 6);
            meshRenderer.transform.rotation = Quaternion.Euler(0, 60 * rotation, 0);
        }

        /// <summary> Request height update for the cell </summary>
        public void UpdateHeight()
        {
            if (Cell == null) return;

            if (currentAnim != null) StopCoroutine(currentAnim);
            currentAnim = StartCoroutine(UpdateHeightAnimation());

#if UNITY_EDITOR
            name = $"Cell {Cell.Position} {Cell.Height}";
#endif
        }

        /// <summary> Change cell state visualization </summary>
        /// <param name="state"> </param>
        public void SetState(CellState state, bool allowStateOverride = true)
        {
            if (allowStateOverride)
            {
                if (Cell.Height < 0 && state == CellState.Default)
                {
                    state = CellState.HasDrowned;
                }
                else if (Cell.WillHaveCLOC && Cell.Height >= 0)
                {
                    if (Cell.Height == 0)
                    {
                        if (state == CellState.Default)
                            state = CellState.WillDrown;
                        else if (state == CellState.Range)
                            state = CellState.RangeWillDrown;
                        else if (state == CellState.Selected)
                            state = CellState.SelectedWillDrown;
                    }
                    else
                    {
                        if (state == CellState.Default)
                            state = CellState.WillHaveCLOC;
                        else if (state == CellState.Range)
                            state = CellState.RangeCLOC;
                        else if (state == CellState.Selected)
                            state = CellState.SelectedCLOC;
                    }
                }
            }

            if (State != state)
            {
                State = state;
                _dirtyState = true;
            }
        }

        private void LateUpdate()
        {
            // If the state has changed
            if (_dirtyState && State != LastState)
            {
                LastState = State;
                _dirtyState = false;

                allMaterials[0] = s_stateMaterialMap.TryGetValue(State, out Material mainMaterial) ? mainMaterial : defaultMaterial;
                allMaterials[1] = s_cliffMaterialMap.TryGetValue(State, out Material cliffMaterial) ? cliffMaterial : cliffDefaultMaterial;
                meshRenderer.materials = allMaterials;
                Destroy(cachedMat);
                cachedMat = meshRenderer.material;

                if (Cell != null)
                {
                    if (Cell.IsOccupiedByPawn)
                        SetDirtAmountProperty(1.0f);
                    else
                        SetDirtAmountProperty(0.0f);
                }
            }
        }


        private IEnumerator UpdateHeightAnimation()
        {
            // Gets the pawn and loot box before they may be removed from the cell
            var pawn = Cell.Pawn;
            var lootBox = Cell.LootBox;

            // Enable interpolation for proxies
            if (pawn != null)
            {
                pawn.SetDisableInterpolation(false, true);
            }

            if (lootBox != null)
            {
                lootBox.SetDisableInterpolation(false, true);
            }

            var h = Cell.Height * yHeight;

            var startPosition = transform.localPosition;
            var endPosition = new Vector3(transform.localPosition.x, h, transform.localPosition.z);

            yield return Tweens.Lerp(
                0f, 1f, UPDATE_HEIGHT_ANIMATION_LENGTH, Tweens.BounceOut, step =>
                {
                    transform.localPosition = Vector3.Lerp(startPosition, endPosition, step);
                    if (Cell.Pawn != null)
                    {
                        Cell.Pawn.transform.localPosition = transform.localPosition;
                    }

                    if (Cell.LootBox != null)
                    {
                        Cell.LootBox.transform.localPosition = transform.localPosition;
                    }
                });

            yield return null;

            // Disable interpolation for all (because state authority may have changed)
            if (pawn != null)
            {
                pawn.SetDisableInterpolation(true, false);
            }

            if (lootBox != null)
            {
                lootBox.SetDisableInterpolation(true, false);
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (Cell != null)
            {
                UnityEditor.Handles.Label(transform.position, $"{Cell.Position} {Cell.Height} {Cell.Pawn}");
            }
        }
#endif

        /// <summary> Checks if a position is below the tile top area (cliff) </summary>
        public bool IsAimingAtCliff(Vector3 p)
        {
            var cellCliffYOffset = TabletopGameManager.Instance.Config.cellCliffYOffset;

            var localPos = transform.InverseTransformPoint(p);

            return localPos.y < cellCliffYOffset;
        }

        #region Animation

        private static readonly int s_dirtAmount = Shader.PropertyToID("_DirtAmount");

        private void SetDirtAmountProperty(float value)
        {
            cachedMat.SetFloat(s_dirtAmount, value);
        }

        public void OnCellOccupied(HexCell cell)
        {
            if (cell == Cell)
                AnimateDirtProperty(0, 1, 0.5f);
        }

        public void OnCellFreed(HexCell cell)
        {
            if (cell == Cell)
                AnimateDirtProperty(1, 0, 0.5f);
        }

        private Coroutine _dirtPropertyCoroutine;

        public void AnimateDirtProperty(float startValue, float endValue, float duration)
        {
            if (_dirtPropertyCoroutine != null)
                StopCoroutine(_dirtPropertyCoroutine);

            _dirtPropertyCoroutine = StartCoroutine(
                VFXAnimationUtility.AnimateFloatProperty(SetDirtAmountProperty, startValue, endValue, duration, math.sqrt));
        }

        #endregion
    }
}