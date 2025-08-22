// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace SpiritSling.TableTop
{
    [MetaCodeSample("SpiritSling")]
    public class SettingsMenu : MonoBehaviour
    {
        protected const int MAX_PLAYER_COUNT = 4;

        [SerializeField]
        protected SettingWidget playerCount;

        [SerializeField]
        protected Button closeBtn;

        [SerializeField]
        protected SettingsToggleWidget showAvatars;

        [SerializeField]
        protected SettingWidget phaseCount;

        [SerializeField]
        protected SettingWidget roundsBeforeCloc;

        [SerializeField]
        protected SettingsToggleWidget randomCloc;

        [SerializeField]
        protected SettingWidget kodamaHp;

        [SerializeField]
        protected SettingWidget kodamaMoveRange;

        [SerializeField]
        protected SettingWidget kodamaSummonRange;

        [SerializeField]
        protected SettingWidget kodamaSummonCount;

        [SerializeField]
        protected SettingsToggleWidget kodamaImmunity;

        [SerializeField]
        protected SettingWidget lavaDamage;

        [SerializeField]
        protected SettingWidget slingshotsHP;

        [SerializeField]
        protected SettingWidget slingshotsDamages;

        [SerializeField]
        protected SettingWidget slingsShotsPerTurn;

        [SerializeField]
        protected SettingWidget lootboxHP;

        [SerializeField]
        protected SettingWidget lootboxStartCount;

        [SerializeField]
        protected SettingWidget lootboxMinCount;

        [SerializeField]
        protected SettingWidget lootboxTurnCount;

        public void Awake()
        {
            closeBtn.onClick.AddListener(OnClickClose);
        }

        protected void OnClickClose()
        {
            gameObject.SetActive(false);
        }

        public void Init()
        {
            // Edit locally the configuration for the player creating the room.
            // The whole struct will be transmitted to all players.
            var config = TabletopConfig.Get(true);
            UpdateFields(config.defaultGameSettings);
        }

        protected void UpdateFields(TabletopGameSettings settings)
        {
            showAvatars.Value = settings.showAvatars;

            phaseCount.Min = 1;
            phaseCount.Max = 99;
            phaseCount.Value = settings.timePhase;

            roundsBeforeCloc.Min = 0;
            roundsBeforeCloc.Max = 99;
            roundsBeforeCloc.Value = settings.roundsBeforeCloc;

            randomCloc.Value = settings.isClocRandom;

            kodamaHp.Min = 1;
            kodamaHp.Max = 99;
            kodamaHp.Value = settings.kodamaStartHealthPoints;

            kodamaMoveRange.Min = 1;
            kodamaMoveRange.Max = 99;
            kodamaMoveRange.Value = settings.kodamaMoveRange;

            kodamaSummonRange.Min = 1;
            kodamaSummonRange.Max = 99;
            kodamaSummonRange.Value = settings.kodamaSummonRange;

            kodamaSummonCount.Min = 1;
            kodamaSummonCount.Max = 99;
            kodamaSummonCount.Value = settings.kodamaSummonPerTurn;

            kodamaImmunity.Value = settings.kodamaHasImmunity;

            lavaDamage.Min = 1;
            lavaDamage.Max = 99;
            lavaDamage.Value = settings.lavaDamage;

            slingshotsHP.Min = 1;
            slingshotsHP.Max = 99;
            slingshotsHP.Value = settings.slingshotStartHealthPoints;

            slingshotsDamages.Min = 1;
            slingshotsDamages.Max = 99;
            slingshotsDamages.Value = settings.slingshotDamage;

            slingsShotsPerTurn.Min = 1;
            slingsShotsPerTurn.Max = 10;
            slingsShotsPerTurn.Value = settings.slingBallShotsPerTurn;

            lootboxHP.Min = 1;
            lootboxHP.Max = 10;
            lootboxHP.Value = settings.lootboxStartHealthPoints;

            lootboxStartCount.Min = 1;
            lootboxStartCount.Max = 99;
            lootboxStartCount.Value = settings.lootboxStartCount;

            lootboxMinCount.Min = 1;
            lootboxMinCount.Max = 10;
            lootboxMinCount.Value = settings.lootboxMinCount;

            lootboxTurnCount.Min = 1;
            lootboxTurnCount.Max = 10;
            lootboxTurnCount.Value = settings.lootBoxSpawnCountPerTurn;
        }

        public TabletopGameSettings GetGameData()
        {
            var config = TabletopConfig.Get();
            var data = config.defaultGameSettings;
            data.seed = DateTime.Now.Millisecond;
            data.showAvatars = showAvatars.Value;
            data.timePhase = (byte)phaseCount.Value;
            data.roundsBeforeCloc = (byte)roundsBeforeCloc.Value;
            data.isClocRandom = randomCloc.Value;
            data.kodamaStartHealthPoints = (byte)kodamaHp.Value;
            data.kodamaMoveRange = (byte)kodamaMoveRange.Value;
            data.kodamaSummonRange = (byte)kodamaSummonRange.Value;
            data.kodamaSummonPerTurn = (byte)kodamaSummonCount.Value;
            data.kodamaHasImmunity = kodamaImmunity.Value;
            data.lavaDamage = (byte)lavaDamage.Value;
            data.slingshotStartHealthPoints = (byte)slingshotsHP.Value;
            data.slingshotDamage = (byte)slingshotsDamages.Value;
            data.slingBallShotsPerTurn = (byte)slingsShotsPerTurn.Value;
            data.lootboxStartHealthPoints = (byte)lootboxHP.Value;
            data.lootboxStartCount = (byte)lootboxStartCount.Value;
            data.lootboxMinCount = (byte)lootboxMinCount.Value;
            data.lootBoxSpawnCountPerTurn = (byte)lootboxTurnCount.Value;
            return data;
        }
    }
}
