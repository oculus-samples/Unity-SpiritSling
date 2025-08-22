// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using System.Linq;
using TMPro;
using UnityEngine;

namespace SpiritSling.TableTop
{
    /// <summary>
    /// UI Displayed on the player board
    /// </summary>
    [MetaCodeSample("SpiritSling")]
    public class PlayerboardUI : MonoBehaviour
    {
        [Header("Game Over")]
        [SerializeField]
        private GameObject winPanel;

        [SerializeField]
        private GameObject loosePanel;

        [SerializeField]
        private CustomButton backMenuButton;

        [SerializeField]
        private GameObject backMenuButtonRoot;

        public Playerboard Playerboard { get; set; }

        private bool ShouldGoBackToLobby => ConnectionManager.Instance.IsPrivateRoom && ConnectionManager.Instance.BotCount == 0;

        private BaseTabletopPlayer player;
        private bool isVisible;

        private void Awake()
        {
            HideEndPanels();
            backMenuButton.onClick.AddListener(OnClickBack);

            var backText = backMenuButton.GetComponentInChildren<TMP_Text>();
            backText.text = ShouldGoBackToLobby ? "Back to Lobby" : "Back to Menu";
        }

        private void HideEndPanels()
        {
            winPanel.SetActive(false);
            loosePanel.SetActive(false);
            backMenuButtonRoot.SetActive(false);
            isVisible = false;
        }

        public void Update()
        {
            var gameManager = TabletopGameManager.Instance;
            if (!gameManager || !BaseTabletopPlayer.LocalPlayer || !BaseTabletopPlayer.LocalPlayer.IsGameReady)
                return;

            if (player == null)
                player = BaseTabletopPlayer.TabletopPlayers.FirstOrDefault(
                    p => p.PlayerId == Playerboard.OwnerId);

            var isLocalPlayer = player.PlayerId == BaseTabletopPlayer.LocalPlayer.PlayerId;
            var gameOver = player != null && player.IsGameOver;

            // Wait for Game Over
            if (player == null || isLocalPlayer == false || !gameOver)
                return;

            if (isVisible == false)
            {
                isVisible = true;

                if (player.IsWinner)
                {
                    winPanel.SetActive(true);
                }
                else
                {
                    loosePanel.SetActive(true);
                }

                backMenuButtonRoot.SetActive(true);
            }
        }

        private void OnClickBack()
        {
            if (ShouldGoBackToLobby)
                TabletopGameManager.Instance.RPC_LeaveToLobby();
            else
                TabletopGameManager.Instance.BackToMenu();

            HideEndPanels();
        }
    }
}
