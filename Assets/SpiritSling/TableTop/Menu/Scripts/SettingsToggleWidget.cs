// Copyright (c) Meta Platforms, Inc. and affiliates.

using Meta.XR.Samples;
using UnityEngine;
using UnityEngine.UI;

namespace SpiritSling.TableTop
{
    [MetaCodeSample("SpiritSling")]
    public class SettingsToggleWidget : MonoBehaviour
    {
        [SerializeField]
        private Toggle toggle;

        [SerializeField]
        private bool startValue;

        private bool _value;
        private bool _useDefaultValue = true;

        public bool Value
        {
            get => _value;
            set
            {
                _value = value;
                _useDefaultValue = false;
                toggle.isOn = _value;
            }
        }

        private void Awake()
        {
            toggle.onValueChanged.AddListener(ValueChanged);

            if (_useDefaultValue) Value = startValue;
        }

        private void ValueChanged(bool v)
        {
            _value = v;
        }
    }
}
