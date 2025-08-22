// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using TMPro;
using UnityEngine;

namespace SpiritSling
{
    [MetaCodeSample("SpiritSling")]
    public class PlayerSlot : MonoBehaviour
    {
        public enum State { Waiting, Ready, Accepted, Declined }

        public State currentState;

        public GameObject WaitingGo;
        public GameObject ReadyGo;
        public GameObject AcceptedGo;
        public GameObject DeclinedGo;

        [SerializeField]
        private TextMeshProUGUI playerName;

        public string PlayerName { set => playerName.text = value; }

        public void SetState(State s)
        {
            currentState = s;
            switch (currentState)
            {
                case State.Waiting:
                    Activate(WaitingGo);
                    break;

                case State.Ready:
                    Activate(ReadyGo);
                    break;

                case State.Accepted:
                    Activate(AcceptedGo);
                    break;

                case State.Declined:
                    Activate(DeclinedGo);
                    break;
            }
        }

        private void Activate(GameObject go)
        {
            WaitingGo.SetActive(WaitingGo == go);
            ReadyGo.SetActive(ReadyGo == go);
            AcceptedGo.SetActive(AcceptedGo == go);
            DeclinedGo.SetActive(DeclinedGo == go);
        }
    }
}
