// Copyright (c) Meta Platforms, Inc. and affiliates.

using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;

namespace SpiritSling.TableTop
{
    public class TabletopTipsState : TabletopMenuBaseState
    {
        [Header("Tips menu")]
        [SerializeField]
        private TMP_Text tipText;

        [SerializeField]
        private GameObject botPrefab;

        public override void Enter()
        {
            base.Enter();

            var tipsList = TabletopConfig.Get().tipsList;
            tipText.text = tipsList[Random.Range(0, tipsList.Count)];

            _ = StartCoroutine(AutoExit());
        }

        private IEnumerator AutoExit()
        {
            // Wait a few seconds to read the tips
            yield return new WaitForSeconds(TabletopConfig.Get().tipsDisplayTime);

            // Wait for all players to be created and initialized
            while (BaseTabletopPlayer.TabletopPlayers.Count(p => p.IsHuman && p.IsSpawned) < MenuStateMachine.ConnectionManager.Runner.SessionInfo.PlayerCount)
            {
                yield return null;
            }

            // Start loading the board for the local player
            var gameStateMachines = MenuStateMachine.ConnectionManager.GameStateMachines;
            gameStateMachines[0].Player = BaseTabletopPlayer.LocalPlayer;
            gameStateMachines[0].gameObject.SetActive(true);

            // Load the bots if there are any
            for (var i = 0; i < ConnectionManager.Instance.BotCount; i++)
            {
                TabletopAiPlayer bot = null;
                var botSpawnOperation = ConnectionManager.Instance.Runner.SpawnAsync(botPrefab, onBeforeSpawned: (runner, obj) =>
                {
                    bot = obj.GetComponent<TabletopAiPlayer>();
                    bot.FakePlayerId = BaseTabletopPlayer.LocalPlayer.PlayerId + 1 + i;
                });

                yield return new WaitUntil(() => botSpawnOperation.IsSpawned);

                gameStateMachines[i + 1].Player = bot;
                gameStateMachines[i + 1].gameObject.SetActive(true);
            }

            // Display the game count down
            MenuStateMachine.ChangeState(MenuStateMachine.countDownState);
        }
    }
}