using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;


namespace UI {
    public class SOBoolUpdater : MonoBehaviour {
        [SerializeField] ScriptableObject target;
        [SerializeField] Toggle toggle;

        [SerializeField] string propertyName;
        FieldInfo _fieldInfo;
    
        void Start() {
            _fieldInfo = target.GetType().GetField(propertyName);
            if (toggle) toggle.onValueChanged.AddListener(OnInputChanged);
            if (bool.TryParse(_fieldInfo.GetValue(target).ToString(), out bool isOn)) {
                toggle.SetIsOnWithoutNotify(isOn);
            }
        }
    
        void OnInputChanged(bool value) {
            if (!target) return;

            if (_fieldInfo == null) throw new Exception($"{target.name} does not have property {propertyName}");
            _fieldInfo.SetValue(target, value);
        }
    }
}
