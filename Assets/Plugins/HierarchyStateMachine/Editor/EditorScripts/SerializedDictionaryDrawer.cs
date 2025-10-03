using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Generics = System.Collections.Generic;

namespace SLS.StateMachineH.SerializedDictionary
{
    [CustomPropertyDrawer(typeof(ISerializedDictionaryNonGeneric), true)]
    public class SerializedDictionaryDrawer : PropertyDrawer
    {
        protected SerializedProperty property;
        protected SerializedProperty serializedListProperty;
        protected ISerializedDictionaryNonGeneric targetDictionary;
        protected ReorderableList reorderableList;

        protected Color redWarning = new Color(1.5f, 1, 1);

        protected string noElementsDisplay => "This dictionary is empty. Click the + button to add a new item.";

        protected bool IsReorderableListValid =>
            reorderableList != null
            && reorderableList.list != null
            && reorderableList.drawElementCallback != null
            && reorderableList.elementHeightCallback != null;

        protected bool Expanded
        {
            get => serializedListProperty.isExpanded;
            set
            {
                serializedListProperty.isExpanded = value;
                reorderableList.displayAdd = value;
                reorderableList.displayRemove = value;
                reorderableList.draggable = value;
                reorderableList.showDefaultBackground = value;
            }
        }

        protected void Initialize(SerializedProperty property, Rect position, GUIContent label, FieldInfo fieldInfo)
        {
            if (property != null) this.property = property;
            if (this.property != null) serializedListProperty = this.property.FindPropertyRelative("serializedList");
            if (this.fieldInfo != null && this.property != null)
                targetDictionary = this.fieldInfo.GetValue(this.property.serializedObject.targetObject) as ISerializedDictionaryNonGeneric;
            if (reorderableList == null) UpdateReorderableList();
        }


        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            Initialize(property, default, label, fieldInfo);

            if(!Expanded) return EditorGUIUtility.singleLineHeight + 2;

            if (!IsReorderableListValid) UpdateReorderableList();
            try
            {
                return reorderableList.GetHeight();
            }
            catch (System.ArgumentNullException)
            {
                try
                {
                    UpdateReorderableList();
                    return reorderableList.GetHeight();
                }
                catch (System.ArgumentNullException)
                {
                    Debug.LogWarning("SerializedDictionaryDrawer: Could not get height for reorderable list. Returning default height.");
                    return EditorGUIUtility.singleLineHeight * 3.5f;
                }
            }
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Initialize(property, position, label, fieldInfo);

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();

            if (IsReorderableListValid) UpdateReorderableList();
            try
            {
                reorderableList.DoList(position);
            }
            catch (System.ArgumentNullException)
            {
                UpdateReorderableList();
                reorderableList.DoList(position);
            }
            finally
            {
                if (EditorGUI.EndChangeCheck())
                {
                    property.serializedObject.ApplyModifiedProperties();
                    UpdateReorderableList();
                }

                EditorGUI.EndProperty();
            }
        }


        public void UpdateReorderableList()
        {
            if (serializedListProperty == null)
            {
                if (property != null)
                    serializedListProperty = property.FindPropertyRelative("serializedList");
            }
            if (serializedListProperty == null)
            {
                Debug.LogWarning("SerializedDictionaryDrawer: Could not find 'serializedList' property.");
                return;
            }
            if (property == null || property.serializedObject == null || property.serializedObject.targetObject == null)
                return;

            Undo.RecordObject(property.serializedObject.targetObject, "Modify SerializedDictionary");

            reorderableList = new ReorderableList(property.serializedObject, serializedListProperty);
            if (targetDictionary != null)
                reorderableList.list = targetDictionary.listAccess;

            reorderableList.drawHeaderCallback = HeaderDrawer;

            reorderableList.drawElementCallback = (position, id, isActive, isFocused) =>
            {
                if (!Expanded) return;

                KeyValuePairDrawer(serializedListProperty.GetArrayElementAtIndex(id), position, id, IsDupe(id));
            };

            bool IsDupe(int id)
            {
                bool[] duplicates = targetDictionary?.DuplicateValues;
                return duplicates != null && duplicates.Length > id && duplicates[id];
            }

            reorderableList.elementHeightCallback = index => Expanded
                ? KeyValuePairHeight(serializedListProperty, index)
                : 0;

            

            reorderableList.onChangedCallback = list => UpdateReorderableList();
            reorderableList.onAddCallback = list => { AddNewItem(serializedListProperty, list); };
            reorderableList.onRemoveCallback = list => { RemoveItem(serializedListProperty, list); };
            reorderableList.onReorderCallbackWithDetails = (list, oldID, newID) => UpdateReorderableList();
            reorderableList.drawNoneElementCallback = rect =>
            {
                if (Expanded)
                    EditorGUI.LabelField(rect, noElementsDisplay);
                else rect.height = 0;
            };

            property.serializedObject.ApplyModifiedProperties();
        }


        protected virtual void HeaderDrawer(Rect rect)
        {
            var newRect = new Rect(rect.x, rect.y, rect.width - 10, rect.height);
            Expanded = EditorGUI.Foldout(newRect, Expanded, property.displayName, true);

            if (Event.current.type == EventType.ContextClick && rect.Contains(Event.current.mousePosition))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Clear"), false, () =>
                {
                    serializedListProperty.ClearArray();
                    targetDictionary?.RecalculateOccurences();
                    serializedListProperty.serializedObject.ApplyModifiedProperties();
                    UpdateReorderableList();
                });
                menu.AddItem(new GUIContent("Remove Duplicates"), false, () =>
                {
                    targetDictionary?.RemoveDuplicates();
                    targetDictionary?.RecalculateOccurences();
                    serializedListProperty.serializedObject.ApplyModifiedProperties();
                    UpdateReorderableList();
                });
                menu.ShowAsContext();
                Event.current.Use();
            }
        }

        protected virtual void KeyValuePairDrawer(SerializedProperty item, Rect position, int id, bool isDupe)
        {
            SerializedProperty keyProperty = item.FindPropertyRelative("Key");
            SerializedProperty valueProperty = item.FindPropertyRelative("Value");

            if (keyProperty == null || valueProperty == null) return;

            float keyHeight = EditorGUI.GetPropertyHeight(keyProperty, true);
            float valueHeight = EditorGUI.GetPropertyHeight(valueProperty, true);
            float elementHeight = Mathf.Max(keyHeight, valueHeight);

            Rect keyRect = new Rect(position.x, position.y, position.width * .3f, elementHeight);
            Rect valueRect = new Rect(position.x + position.width * .3f, position.y, position.width * .7f, elementHeight);

            var prevColor = GUI.color;
            if (isDupe) GUI.color = redWarning;

            try
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(keyRect, keyProperty, GUIContent.none);
            }
            finally { GUI.color = prevColor; }
            EditorGUI.PropertyField(valueRect, valueProperty, GUIContent.none);

            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();
                UpdateReorderableList();
            }
        }

        protected virtual float KeyValuePairHeight(SerializedProperty serializedListProperty, int index)
        {
            SerializedProperty element = serializedListProperty.GetArrayElementAtIndex(index);
            SerializedProperty keyProperty = element.FindPropertyRelative("Key");
            SerializedProperty valueProperty = element.FindPropertyRelative("Value");
            return Mathf.Max(
                EditorGUI.GetPropertyHeight(keyProperty, true),
                EditorGUI.GetPropertyHeight(valueProperty, true),
                EditorGUIUtility.singleLineHeight
            );
        }


        protected virtual void AddNewItem(SerializedProperty serializedListProperty, ReorderableList list)
        {
            int place = serializedListProperty.arraySize > 0 ? serializedListProperty.arraySize - 1 : 0;
            serializedListProperty.InsertArrayElementAtIndex(place);
            serializedListProperty.serializedObject.ApplyModifiedProperties();
            UpdateReorderableList();
        }

        protected virtual void RemoveItem(SerializedProperty serializedListProperty, ReorderableList list)
        {
            if (serializedListProperty.arraySize > 0)
            {
                serializedListProperty.DeleteArrayElementAtIndex(list.index);
                serializedListProperty.serializedObject.ApplyModifiedProperties();
                UpdateReorderableList();
            }
        }

    }
}