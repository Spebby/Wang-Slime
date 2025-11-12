using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;
using Xenia.ColorPicker;


namespace UI {
    public class SOColourUpdater : MonoBehaviour {
        [SerializeField] ScriptableObject target;
        [SerializeField] ColorPicker picker;
        [SerializeField] Graphic graphic;
    
        [SerializeField] string propertyName;
        FieldInfo _fieldInfo;

        public void OpenPicker() {
            // remove all other listeners
            picker.ColorChanged.RemoveAllListeners();
            picker.Open();
        
            picker.CurrentColor = (Color)_fieldInfo.GetValue(target);
            picker.ColorChanged.AddListener(OnInputChanged);
            picker.ColorChanged.AddListener((Color c) => {
                graphic.color = c;
            }); 
        }
    
        void Start() {
            _fieldInfo    = target.GetType().GetField(propertyName);
            graphic.color = (Color)_fieldInfo.GetValue(target);
        }
    
        void OnInputChanged(Color value) {
            if (!target) return;

            if (_fieldInfo == null) throw new Exception($"{target.name} does not have property {propertyName}");
            _fieldInfo.SetValue(target, value);
        }
    }
}
