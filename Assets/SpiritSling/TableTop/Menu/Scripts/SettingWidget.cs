// Copyright (c) Meta Platforms, Inc. and affiliates.

using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SpiritSling.TableTop
{
    public class SettingWidget : MonoBehaviour
    {
        [SerializeField]
        private Button plusBtn;

        [SerializeField]
        private Button minusBtn;

        [SerializeField]
        private TMP_InputField valueInputField;

        [SerializeField]
        private int startValue = 1;

        [SerializeField]
        private int step = 1;

        [SerializeField]
        private int min = 1;

        [SerializeField]
        private int max = 4;

        [SerializeField]
        private string format = "N0";

        private int _value;
        private bool _useDefaultValue = true;

        public int Value
        {
            get => _value;
            set
            {
                _value = value;
                _useDefaultValue = false;
                valueInputField.text = _value.ToString(format);
            }
        }

        public int Min
        {
            get => min;
            set => min = value;
        }

        public int Max
        {
            get => max;
            set => max = value;
        }

        public string Format
        {
            get => format;
            set => format = value;
        }

        private void Awake()
        {
            plusBtn.onClick.AddListener(AddToValue);
            minusBtn.onClick.AddListener(RemoveToValue);
            valueInputField.interactable = false;
            if (_useDefaultValue) Value = startValue;
        }

        /// <summary>
        /// Sets how many players are needed in a room before the game starts
        /// </summary>
        /// <param name="v"></param>
        private void AddToValue(int v)
        {
            Value = Mathf.Clamp(Value + v, min, max);
        }

        private void AddToValue()
        {
            AddToValue(step);
        }

        private void RemoveToValue()
        {
            AddToValue(-step);
        }
    }
}