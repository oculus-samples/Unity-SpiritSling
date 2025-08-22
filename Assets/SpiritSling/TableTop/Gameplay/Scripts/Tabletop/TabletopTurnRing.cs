// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

namespace SpiritSling.TableTop
{
    [MetaCodeSample("SpiritSling")]
    public class TabletopTurnRing : MonoBehaviour
    {
        [SerializeField]
        private GameObject _turnRing;

        private Animator _animator;

        private int _playerParam = Animator.StringToHash("PlayerColor");
        void Awake()
        {
            _animator = _turnRing.GetComponent<Animator>();

            TabletopGameEvents.OnNextTurnCalled += ChangePlayer;
            TabletopGameEvents.GameStart += Show;
            TabletopGameEvents.OnGameOver += OnGameOver;
            TabletopGameEvents.OnMainMenuEnter += Hide;
            TabletopGameEvents.OnFirstMenuEnter += Hide;
            Hide();
        }

        void OnDestroy()
        {
            TabletopGameEvents.OnNextTurnCalled -= ChangePlayer;
            TabletopGameEvents.GameStart -= Show;
            TabletopGameEvents.OnGameOver -= OnGameOver;
            TabletopGameEvents.OnMainMenuEnter -= Hide;
            TabletopGameEvents.OnFirstMenuEnter -= Hide;
        }

        private void ChangePlayer(byte nextPlayerIndex, bool forceNoNewRound)
        {
            Show();
            _animator.SetInteger(_playerParam, nextPlayerIndex + 1);
        }

        private void OnGameOver(BaseTabletopPlayer player)
        {
            if (TabletopGameManager.Instance.HasSomeoneWon())
            {
                Hide();
            }
        }
        private void Show()
        {
            _turnRing.SetActive(true);
            _animator.SetInteger(_playerParam, 0);
        }

        private void Hide()
        {
            _turnRing.SetActive(false);
        }
    }
}
