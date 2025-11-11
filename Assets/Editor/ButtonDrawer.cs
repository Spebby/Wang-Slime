using UnityEditor;
using UnityEngine;
using System.Reflection;
using Tiles;


namespace Editor {


    [CustomPropertyDrawer(typeof(ButtonAttribute))]
    public class ButtonDrawer : PropertyDrawer {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            ButtonAttribute buttonAttribute = (ButtonAttribute)attribute;

            if (!GUI.Button(position, label.text)) return;
            // Get the target object of the serialized property
            object     target = property.serializedObject.targetObject;
            MethodInfo method = target.GetType().GetMethod(buttonAttribute.MethodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (method != null) {
                method.Invoke(target, null);
            } else {
                Debug.LogWarning($"No method named '{buttonAttribute.MethodName}' found in {target.GetType()}");
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUIUtility.singleLineHeight;
        }
    }
}