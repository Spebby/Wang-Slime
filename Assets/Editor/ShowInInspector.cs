using Tiles;
using UnityEditor;
using UnityEngine;


namespace Editor {
    
    [CustomPropertyDrawer(typeof(ShowInInspectorAttribute))]
    public class ShowInInspectorPropertyDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.BeginProperty(position, label, property);
            Object targetObject = property.serializedObject.targetObject;
            object value        = fieldInfo.GetValue(targetObject);
            EditorGUI.LabelField(position, $"{label.text}: {value}");
            EditorGUI.EndProperty();
        }
    }
}