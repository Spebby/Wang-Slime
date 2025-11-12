using System;
using System.Reflection;
using TMPro;
using UnityEngine;


namespace UI {
    public class SOIntFieldUpdater : MonoBehaviour {
        [SerializeField] ScriptableObject target;
        [SerializeField] TMP_InputField field;

        [SerializeField] string propertyName;
        FieldInfo _fieldInfo;
    
        void Start() {
            _fieldInfo = target.GetType().GetField(propertyName);
            if (field) field.onEndEdit.AddListener(OnInputChanged);
            field.text = _fieldInfo.GetValue(target).ToString();
        }
    
        void OnInputChanged(string value) {
            if (!target) return;

            if (_fieldInfo == null) throw new Exception($"{target.name} does not have property {propertyName}");
            if (float.TryParse(value, out float result)) {
                _fieldInfo.SetValue(target, Mathf.FloorToInt(result));
            }
        }
    }
}
