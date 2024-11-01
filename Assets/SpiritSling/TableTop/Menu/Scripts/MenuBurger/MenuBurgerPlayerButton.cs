// Copyright (c) Meta Platforms, Inc. and affiliates.

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpiritSling.TableTop
{
    public class MenuBurgerPlayerButton : MonoBehaviour
    {
        public CustomButton button;
        public TextMeshProUGUI actionText;
        public Image imageTalk;
        public Image imageMute;

        private BaseTabletopPlayer _player;

        public BaseTabletopPlayer Player
        {
            get => _player;
            set
            {
                _player = value;
                UpdatePanel();
            }
        }

        private void Awake()
        {
            button.onClick.AddListener(ToggleMute);
        }

        private void ToggleMute()
        {
            if (!Player) return;

            Player.SetVoiceEnabled(!Player.IsVoiceEnabled);
            UpdatePanel();
        }

        private void UpdatePanel()
        {
            if (Player && Player.IsHuman)
            {
                actionText.gameObject.SetActive(true);
                actionText.text = Player.DisplayName;
                imageMute.gameObject.SetActive(Player.IsVoiceEnabled);
                imageTalk.gameObject.SetActive(!Player.IsVoiceEnabled);
            }
            else
            {
                actionText.gameObject.SetActive(false);
                imageMute.gameObject.SetActive(false);
                imageTalk.gameObject.SetActive(false);
            }
        }
    }
}