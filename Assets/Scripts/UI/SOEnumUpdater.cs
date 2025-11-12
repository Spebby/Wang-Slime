using System;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;


namespace UI {
    public class SOEnumUpdater : MonoBehaviour {
        [SerializeField] ScriptableObject target;
        [SerializeField] TMP_Dropdown dropdown;

        [SerializeField] string propertyName;
        FieldInfo _fieldInfo;
        Array _enumValues;

        void Start() {
            _fieldInfo = target.GetType().GetField(propertyName);
            if (_fieldInfo == null) {
                Debug.LogError($"Field {propertyName} not found on {target}");
                return;
            }

            Type enumType = _fieldInfo.FieldType;
            if (!enumType.IsEnum) {
                Debug.LogError($"Field {propertyName} is not an enum!");
                return;
            }

            // Get enum values and names
            _enumValues = Enum.GetValues(enumType);
            string[] names = Enum.GetNames(enumType);

            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(names));

            // Set current value
            object currentVal   = _fieldInfo.GetValue(target);
            int    currentIndex = Array.IndexOf(_enumValues, currentVal);
            dropdown.value = currentIndex;

            // Add listener
            dropdown.onValueChanged.AddListener(OnInputChanged);
        }
    
        void OnInputChanged(int value) {
            if (!target) return;

            if (_fieldInfo == null) throw new Exception($"{target.name} does not have property {propertyName}");
            _fieldInfo.SetValue(target, value);
        }
    }
}
