using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SerializableTypes.Editor {

    /// <summary>
    /// Draw the wrapper without foldouts.
    /// </summary>
    [CustomPropertyDrawer(typeof(SerializableQueue<>))]
    public class SerializableQueueDrawer : PropertyDrawer {
        static readonly string _queueName = "_queue";

        public override VisualElement CreatePropertyGUI(SerializedProperty property) {
            return new PropertyField(property.FindPropertyRelative(_queueName), property.displayName);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            EditorGUI.PropertyField(position, property.FindPropertyRelative(_queueName), label);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            return EditorGUI.GetPropertyHeight(property.FindPropertyRelative(_queueName));
        }
    }

}
