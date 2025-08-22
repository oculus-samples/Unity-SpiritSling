// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;

namespace SpiritSling.TableTop
{
    [MetaCodeSample("SpiritSling")]
    public class PlayerboardPhaseUI : MonoBehaviour
    {
        [SerializeField]
        private GameObject selected;

        private void Awake()
        {
            SetSelected(false);
        }

        public void SetSelected(bool yes)
        {
            selected.gameObject.SetActive(yes);
        }
    }
}
