using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;


namespace UI {
    public class SOFloatSliderUpdater : MonoBehaviour {
        [SerializeField] ScriptableObject target;
        [SerializeField] Slider slider;

        [SerializeField] string propertyName;
        FieldInfo _fieldInfo;
    
        void Start() {
            _fieldInfo = target.GetType().GetField(propertyName);
            if (slider) slider.onValueChanged.AddListener(OnInputChanged);
            if (float.TryParse(_fieldInfo.GetValue(target).ToString(), out float result)) {
                slider.value = result;
            }
        }
    
        void OnInputChanged(float value) {
            if (!target) return;

            if (_fieldInfo == null) throw new Exception($"{target.name} does not have property {propertyName}");
            _fieldInfo.SetValue(target, value);
        }
    }
}
