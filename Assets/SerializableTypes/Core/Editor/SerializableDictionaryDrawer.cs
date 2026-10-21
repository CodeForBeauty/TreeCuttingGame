using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SerializableTypes.Editor {

    /// <summary>
    /// Draw the wrapper class as a list. Getting rid of additional foldouts.
    /// Shows warning if there are duplicates.
    /// </summary>
    [CustomPropertyDrawer(typeof(SerializableDictionary<,>))]
    public class SerializableDictionaryDrawer : PropertyDrawer {
        static readonly string _dictionaryName = "_dictionary";
        static readonly string _hasDuplicatesName = "_hasDuplicates";

        public override VisualElement CreatePropertyGUI(SerializedProperty property) {
            VisualElement container = new();

            SerializedProperty list = property.FindPropertyRelative(_dictionaryName);
            PropertyField listField = new(list, property.displayName);

            container.Add(listField);

            if (property.FindPropertyRelative(_hasDuplicatesName).boolValue) {
                HelpBox message = new("This dictionary has duplicate keys", HelpBoxMessageType.Warning);
                container.Add(message);
            }

            return container;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            SerializedProperty list = property.FindPropertyRelative(_dictionaryName);
            EditorGUI.PropertyField(position, list, label);

            if (property.FindPropertyRelative(_hasDuplicatesName).boolValue) {
                float listHeight = EditorGUI.GetPropertyHeight(list);
                Rect messagePos = new(position.position.x, position.position.y + listHeight, position.width, EditorGUIUtility.singleLineHeight * 2);
                EditorGUI.HelpBox(messagePos, "This dictionary has duplicate keys", MessageType.Warning);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            float height = EditorGUI.GetPropertyHeight(property.FindPropertyRelative(_dictionaryName));
            if (property.FindPropertyRelative(_hasDuplicatesName).boolValue) {
                height += EditorGUIUtility.singleLineHeight * 2;
            }
            return height;
        }
    }

}
