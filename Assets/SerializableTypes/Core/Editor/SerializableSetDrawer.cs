using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;


namespace SerializableTypes.Editor {

    /// <summary>
    /// Draw the wrapper class as a list. Getting rid of additional foldouts.
    /// Shows warning if there are duplicates.
    /// </summary>
    [CustomPropertyDrawer(typeof(SerializableSet<>))]
    public class SerializableSetDrawer : PropertyDrawer {
        static readonly string _setName = "_set";
        static readonly string _hasDuplicatesName = "_hasDuplicates";

        public override VisualElement CreatePropertyGUI(SerializedProperty property) {
            VisualElement container = new();

            SerializedProperty list = property.FindPropertyRelative(_setName);
            PropertyField listField = new(list, property.displayName);

            container.Add(listField);

            if (property.FindPropertyRelative(_hasDuplicatesName).boolValue) {
                HelpBox message = new("This HashSet has duplicate values", HelpBoxMessageType.Warning);
                container.Add(message);
            }

            return container;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            SerializedProperty list = property.FindPropertyRelative(_setName);
            float listHeight = EditorGUI.GetPropertyHeight(list);
            EditorGUI.PropertyField(new Rect(position.x, position.y, position.width, listHeight), list, label);

            if (property.FindPropertyRelative(_hasDuplicatesName).boolValue) {
                Rect messagePos = new(position.position.x, position.position.y + listHeight, position.width, EditorGUIUtility.singleLineHeight * 2);
                EditorGUI.HelpBox(messagePos, "This HashSet has duplicate values", MessageType.Warning);
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            float height = EditorGUI.GetPropertyHeight(property.FindPropertyRelative(_setName));
            if (property.FindPropertyRelative(_hasDuplicatesName).boolValue) {
                height += EditorGUIUtility.singleLineHeight * 2;
            }
            return height;
        }
    }

}
