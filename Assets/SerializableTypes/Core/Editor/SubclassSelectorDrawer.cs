using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace SerializableTypes.Editor {

    /// <summary>
    /// Polymorphic subclass selector.
    /// </summary>
    [CustomPropertyDrawer(typeof(SubclassSelectorAttribute))]
    public class SubclassSelectorDrawer : PropertyDrawer {
        static readonly private Dictionary<Type, List<Type>> _cache = new();


        #region UI Toolkit
        public override VisualElement CreatePropertyGUI(SerializedProperty property) {
            VisualElement container = new();

            if (property.propertyType != SerializedPropertyType.ManagedReference) {
                container.Add(new Label("Type not supported"));
                return container;
            }

            // Type selection dropdown creation
            string typeName = property.managedReferenceValue == null
                ? "Null"
                : property.managedReferenceValue.GetType().Name;

            List<Type> types = GetTypes(GetPropertyType());
            List<string> options = types.ConvertAll(t => t != null ? t.Name : "Null");

            DropdownField dropdown = new(property.displayName, options, -1);

            dropdown.value = typeName;

            dropdown.RegisterValueChangedCallback(evt => {
                property.serializedObject.Update();
                Type t = types[dropdown.index];
                if (t != null) {
                    property.managedReferenceValue = Activator.CreateInstance(t);
                }
                else {
                    property.managedReferenceValue = null;
                }
                property.serializedObject.ApplyModifiedProperties();
            });

            container.Add(dropdown);

            // Drawing managedReference's fields
            if (property.managedReferenceValue != null) {
                Foldout refFoldout = new();
                refFoldout.text = "Data";
                refFoldout.style.marginLeft = 15;

                var iterator = property.Copy();
                var end = iterator.GetEndProperty();

                bool enterChildren = true;

                while (iterator.NextVisible(enterChildren) &&
                       !SerializedProperty.EqualContents(iterator, end)) {
                    enterChildren = false;

                    refFoldout.Add(new PropertyField(iterator.Copy()));
                }
                container.Add(refFoldout);
            }

            return container;
        }
        #endregion

        #region IMGUI
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
            if (property.propertyType != SerializedPropertyType.ManagedReference) {
                EditorGUI.LabelField(position, label.text, "[SerializeReference] required");
                return;
            }

            float lineHeight = EditorGUIUtility.singleLineHeight;

            Rect btnRect = new(position.position.x, position.position.y, position.width, lineHeight);

            string typeName = property.managedReferenceValue == null
                ? "Null"
                : property.managedReferenceValue.GetType().Name;

            if (EditorGUI.DropdownButton(btnRect, new GUIContent($"{label.text}: {typeName}"), FocusType.Keyboard)) {
                ShowSelectMenu(property);
            }

            if (property.managedReferenceValue != null) {
                Rect contentRect = new(
                    position.x,
                    position.y + lineHeight + 2,
                    position.width,
                    position.height - lineHeight - 2
                );

                EditorGUI.indentLevel++;
                EditorGUI.PropertyField(contentRect, property, new GUIContent("Data"), true);
                EditorGUI.indentLevel--;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) {
            float height = EditorGUIUtility.singleLineHeight;

            if (property.propertyType == SerializedPropertyType.ManagedReference
                    && property.managedReferenceValue != null) {
                height += EditorGUI.GetPropertyHeight(property, true) + 2;
            }

            return height;
        }

        /// <summary>
        /// Show the type list to select from
        /// </summary>
        /// <param name="property">property to update upon selection</param>
        private void ShowSelectMenu(SerializedProperty property) {
            GenericMenu menu = new();

            foreach (Type type in GetTypes(GetPropertyType())) {
                menu.AddItem(new GUIContent(type != null ? type.Name : "Null"), false, () => {
                    property.serializedObject.Update();
                    if (type == null) {
                        property.managedReferenceValue = null;
                    }
                    else {
                        property.managedReferenceValue = Activator.CreateInstance(type);
                    }
                    property.serializedObject.ApplyModifiedProperties();
                });
            }

            menu.ShowAsContext();
        }
        #endregion

        #region Common
        /// <summary>
        /// Get the list of non abstract types derived fom a baseType.
        /// </summary>
        /// <param name="baseType">baseType to search types</param>
        /// <returns>List of derived types</returns>
        private List<Type> GetTypes(Type baseType) {
            if (_cache.ContainsKey(baseType)) {
                return _cache[baseType];
            }

            var derived = TypeCache.GetTypesDerivedFrom(baseType);

            List<Type> subclasses = new();
            foreach (Type type in derived) {
                if (!type.IsAbstract) {
                    subclasses.Add(type);
                }
            }

            subclasses.Add(null);

            _cache.Add(baseType, subclasses);

            return subclasses;
        }

        /// <summary>
        /// Get the type of the current field.
        /// </summary>
        /// <returns>Element type in arrays, and the first argument in generic types</returns>
        private Type GetPropertyType() {
            Type type = fieldInfo.FieldType;

            if (type.IsArray) {
                return type.GetElementType();
            }

            if (type.IsGenericType) {
                return type.GetGenericArguments()[0];
            }

            return type;
        }
        #endregion
    }

}
