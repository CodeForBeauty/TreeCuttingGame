using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SerializableTypes.Editor {

    /// <summary>
    /// Draw KeyValue pair without foldout with coloring on duplicates.
    /// </summary>
    [CustomPropertyDrawer(typeof(SerializableDictionary<,>.KeyValue))]
    public class DictionaryPairDrawer : PropertyDrawer {
        static readonly string _keyName = "Key";
        static readonly string _valueName = "Value";

        public override VisualElement CreatePropertyGUI(SerializedProperty property) {
            VisualElement container = new();

            container.Add(new Label(property.displayName));

            SerializedProperty keyProperty = property.FindPropertyRelative(_keyName);
            container.Add(new PropertyField(keyProperty));

            SerializedProperty valProperty = property.FindPropertyRelative(_valueName);
            container.Add(new PropertyField(valProperty));

            return container;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            float lineHeight = EditorGUIUtility.singleLineHeight;
            Rect labelPos = new(position.position.x, position.position.y, position.width, lineHeight);
            EditorGUI.LabelField(labelPos, label);

            SerializedProperty keyProperty = property.FindPropertyRelative(_keyName);
            float keyHeight = EditorGUI.GetPropertyHeight(keyProperty);
            Rect keyPos = new(position.position.x, position.position.y + lineHeight + 2, position.width, keyHeight);

            EditorGUI.PropertyField(keyPos, keyProperty);

            SerializedProperty valProperty = property.FindPropertyRelative(_valueName);
            float valHeight = EditorGUI.GetPropertyHeight(valProperty);
            Rect valPos = new(keyPos.position.x, keyPos.position.y + keyHeight + 2, position.width, valHeight);

            EditorGUI.PropertyField(valPos, valProperty);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            float keyHeight = EditorGUI.GetPropertyHeight(property.FindPropertyRelative(_keyName)) + 2;
            float valHeight = EditorGUI.GetPropertyHeight(property.FindPropertyRelative(_valueName)) + 2;
            float labelHeight = EditorGUIUtility.singleLineHeight + 2;
            return keyHeight + valHeight + labelHeight;
        }
    }

}
