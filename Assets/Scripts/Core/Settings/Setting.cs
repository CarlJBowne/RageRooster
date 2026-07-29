using Newtonsoft.Json.Linq;
using System;
using UnityEngine.UI;

namespace RageRooster.Settings
{
    [System.Serializable]
    public class Setting<T>
    {
        private T _value;
        public T defaultValue;
        public Action<T> onChanged;
        public Action<T> updateUI;

        public Setting(T defaultValue, Action<T> onChanged = null, Action<T> updateUI = null)
        {
            _value = defaultValue;
            this.defaultValue = defaultValue;
            this.onChanged = onChanged;
            this.updateUI = updateUI;
        }

        public T Value
        {
            get => _value;
            set
            {
                _value = value;
                onChanged?.Invoke(value);
                updateUI?.Invoke(value);
            }
        }
        public void ValueFromUI(T value)
        {
            _value = value;
            onChanged?.Invoke(value);
        }
        public static implicit operator T(Setting<T> This) => This._value;

        public void TakeSaveInput(JToken input)
        {
            if (input == null) return;
            Value = input.ToObject<T>();
        }
    }

    [System.Serializable]
    public class FloatSetting : Setting<float>
    {
        public FloatSetting(float defaultValue, Action<float> onChanged = null, Slider slider = null)
            : base(defaultValue, onChanged)
        {
            if (slider != null) SetupSlider(slider);
        }

        public void SetupSlider(Slider slider, float min = 0, float max = 1)
        {
            slider.minValue = min;
            slider.maxValue = max;
            updateUI += value => slider.value = value;
            updateUI?.Invoke(Value);
            onChanged?.Invoke(Value);
            slider.onValueChanged.AddListener(ValueFromUI);
        }
    }
}
