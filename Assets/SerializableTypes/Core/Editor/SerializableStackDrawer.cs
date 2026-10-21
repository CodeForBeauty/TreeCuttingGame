using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SerializableTypes.Editor {

    /// <summary>
    /// Draw the wrapper without foldouts.
    /// </summary>
    [CustomPropertyDrawer(typeof(SerializableStack<>))]
    public class SerializableStackDrawer : PropertyDrawer {
        static readonly string _stackName = "_stack";

        public override VisualElement CreatePropertyGUI(SerializedProperty property) {
            return new PropertyField(property.FindPropertyRelative(_stackName), property.displayName);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.PropertyField(position, property.FindPropertyRelative(_stackName), label);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUI.GetPropertyHeight(property.FindPropertyRelative(_stackName));
        }
    }

}
