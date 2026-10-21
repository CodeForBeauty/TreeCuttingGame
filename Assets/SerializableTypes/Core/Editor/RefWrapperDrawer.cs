using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SerializableTypes.Editor {

    /// <summary>
    /// Draws the wrapper's value without foldout
    /// </summary>
    [CustomPropertyDrawer(typeof(RefWrapper<>))]
    public class RefWrapperDrawer : PropertyDrawer {
        static readonly private string _valueName = "_value";

        public override VisualElement CreatePropertyGUI(SerializedProperty property) {
            VisualElement container = new();

            container.Add(new Label(property.displayName));

            SerializedProperty value = property.FindPropertyRelative(_valueName);
            container.Add(new PropertyField(value, property.displayName));

            return container;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.PropertyField(position, property.FindPropertyRelative(_valueName), label);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUI.GetPropertyHeight(property.FindPropertyRelative(_valueName));
        }
    }

}
