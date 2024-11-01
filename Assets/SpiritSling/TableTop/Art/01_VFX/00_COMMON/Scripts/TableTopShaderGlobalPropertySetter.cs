// Copyright (c) Meta Platforms, Inc. and affiliates.

using UnityEngine;
using static SpiritSling.TableTop.VFXAnimationUtility;

namespace SpiritSling.TableTop
{
    public class TableTopShaderGlobalPropertySetter : Singleton<TableTopShaderGlobalPropertySetter>
    {
        // Player
        private static readonly int s_localPlayerColor = Shader.PropertyToID("_LocalPlayerColor");

        // PlayerBoard Stones
        private static readonly int s_stoneInteractionRange = Shader.PropertyToID("_StoneInteractionRange");
        private static readonly int s_stoneSkipRange = Shader.PropertyToID("_StoneSkipRange");

        // Tiles
        private static readonly int s_shakeIntensity = Shader.PropertyToID("_ShakeIntensity");
        private static readonly int s_shakeFrequency = Shader.PropertyToID("_ShakeFrequency");
        private static readonly int s_verticalShakeFrequency = Shader.PropertyToID("_VerticalShakeFrequency");

        // stones
        private static readonly int s_allowPhaseSkip = Shader.PropertyToID("_AllowPhaseSkip");

        private TabletopConfig Config => TabletopConfig.Get();

        private void Start()
        {
            RegisterCallbacks();
            SetGlobalShaderProperties(Config);
        }

        private void OnDestroy()
        {
            UnRegisterCallbacks();
        }

        private void RegisterCallbacks()
        {
            TabletopGameEvents.GameStart += SetLocalPlayerColor;
            TabletopGameEvents.OnPawnDragStart += OnPawnDragStart;
            TabletopGameEvents.OnPawnDragEnd += OnPawnDragEnd;
        }

        private void UnRegisterCallbacks()
        {
            TabletopGameEvents.GameStart -= SetLocalPlayerColor;
            TabletopGameEvents.OnPawnDragStart -= OnPawnDragStart;
            TabletopGameEvents.OnPawnDragEnd -= OnPawnDragEnd;
        }

        private void SetLocalPlayerColor()
        {
            var color = Config.PlayerColor(BaseTabletopPlayer.LocalPlayer.Index);
            Shader.SetGlobalColor(s_localPlayerColor, color);
        }

        public static void SetGlobalShaderProperties(TabletopConfig config)
        {
            Shader.SetGlobalFloat(s_stoneSkipRange, config.StoneSkipRange);
            Shader.SetGlobalFloat(s_stoneInteractionRange, config.StoneInteractionRange);
            Shader.SetGlobalFloat(s_shakeIntensity, config.ShakeIntensity);
            Shader.SetGlobalFloat(s_shakeFrequency, config.ShakeFrequency);
            Shader.SetGlobalFloat(s_verticalShakeFrequency, config.VerticalShakeFrequency);
            SetAllowPhaseValue(1);
        }

        #region Stones

        public static void SetAllowPhaseValue(float value) => Shader.SetGlobalFloat(s_allowPhaseSkip, value);

        Coroutine m_allowSkipCoroutine;

        private void SetStoneAllowSkip(float startValue, float endValue)
        {
            StopIfNotNull(m_allowSkipCoroutine);
            StartCoroutine(AnimateFloatProperty(SetAllowPhaseValue, startValue, endValue, 0.3f));
        }

        private void OnPawnDragStart()
        {
            SetStoneAllowSkip(1, 0);
        }

        private void OnPawnDragEnd(Pawn pawn)
        {
            SetStoneAllowSkip(0, 1);
        }

        #endregion

        private void StopIfNotNull(Coroutine coroutine)
        {
            if (coroutine != null)
            {
                StopCoroutine(coroutine);
            }
        }
    }
}