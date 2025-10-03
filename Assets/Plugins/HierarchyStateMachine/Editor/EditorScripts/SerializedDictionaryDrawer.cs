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

        protected readonly Color redWarning = new Color(1.5f, 1, 1);
        protected virtual string NoElementsDisplay => "This dictionary is empty. Click the + button to add a new item.";

        protected bool IsReorderableListValid =>
            reorderableList != null 
            && reorderableList.list != null 
            && reorderableList.drawElementCallback != null 
            && reorderableList.elementHeightCallback != null;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            Initialize(property, label);

            if (!Expanded)
                return EditorGUIUtility.singleLineHeight;

            MakeReorderableList();
            return reorderableList.GetHeight();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            Initialize(property, label);

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();

            MakeReorderableList();
            if(!Expanded)ReorderableList.defaultBehaviours.DrawHeaderBackground(position);
            reorderableList.DoList(position);

            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedProperties();
                MakeReorderableList();
            }
            EditorGUI.EndProperty();
        }

        protected void Initialize(SerializedProperty property, GUIContent label)
        {
            if (property != null)
                this.property = property;

            if (this.property != null && serializedListProperty == null)
                serializedListProperty = this.property.FindPropertyRelative("serializedList");

            if (fieldInfo != null && this.property != null && targetDictionary == null)
                targetDictionary = fieldInfo.GetValue(this.property.serializedObject.targetObject) as ISerializedDictionaryNonGeneric;

            MakeReorderableList();
        }

        protected void MakeReorderableList()
        {
            if (IsReorderableListValid) return;

            if (property == null || property.serializedObject == null || property.serializedObject.targetObject == null)
                return;

            if (serializedListProperty == null && property != null)
                serializedListProperty = property.FindPropertyRelative("serializedList");

            if (serializedListProperty == null)
            {
                Debug.LogWarning("SerializedDictionaryDrawer: Could not find 'serializedList' property.");
                return;
            }

            Undo.RecordObject(property.serializedObject.targetObject, "Modify SerializedDictionary");

            reorderableList = new ReorderableList(property.serializedObject, serializedListProperty);
            if (targetDictionary != null) reorderableList.list = targetDictionary.listAccess;

            

            reorderableList.drawHeaderCallback = HeaderDrawer;
            reorderableList.drawElementCallback = (position, index, isActive, isFocused) =>
            {
                if (!Expanded) return;
                KeyValuePairDrawer(serializedListProperty.GetArrayElementAtIndex(index), position, index, IsDuplicate(index));
            };
            reorderableList.elementHeightCallback = index =>
            {
                return Expanded ? KeyValuePairHeight(serializedListProperty, index) : 0;
            };
            reorderableList.onAddCallback = list => AddNewItem(serializedListProperty, list);
            reorderableList.onRemoveCallback = list => RemoveItem(serializedListProperty, list);
            reorderableList.drawNoneElementCallback = rect => 
            { 
                if (Expanded) 
                    EditorGUI.LabelField(rect, NoElementsDisplay); 
            };

            Expanded = Expanded;

            property.serializedObject.ApplyModifiedProperties();
        }

        protected bool Expanded
        {
            get => property?.isExpanded ?? false;
            set
            {
                if (property == null || reorderableList == null) return;
                property.isExpanded = value;
                reorderableList.displayAdd = value;
                reorderableList.displayRemove = value;
                reorderableList.draggable = value;
                //reorderableList.drawElementBackgroundCallback = value ? DrawElementBackground : null;
                //reorderableList.footerHeight = value ? EditorGUIUtility.singleLineHeight : 0;
                reorderableList.showDefaultBackground = value;
            }
        }


        protected virtual void HeaderDrawer(Rect rect)
        {
            var newRect = new Rect(rect.x, rect.y, rect.width - 10, rect.height);
            Expanded = EditorGUI.Foldout(newRect, Expanded, property.displayName, true);

            if (Event.current.type == EventType.ContextClick && rect.Contains(Event.current.mousePosition)) HeaderContextMenu(new());
        }

        protected virtual void HeaderContextMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Clear"), false, () =>
            {
                serializedListProperty.ClearArray();
                targetDictionary?.RecalculateOccurences();
                serializedListProperty.serializedObject.ApplyModifiedProperties();
                MakeReorderableList();
            });
            menu.AddItem(new GUIContent("Remove Duplicates"), false, () =>
            {
                targetDictionary?.RemoveDuplicates();
                targetDictionary?.RecalculateOccurences();
                serializedListProperty.serializedObject.ApplyModifiedProperties();
                MakeReorderableList();
            });
            menu.ShowAsContext();
            Event.current.Use();
        }

        protected virtual void KeyValuePairDrawer(SerializedProperty item, Rect position, int id, bool isDupe)
        {
            var keyProperty = item.FindPropertyRelative("Key");
            var valueProperty = item.FindPropertyRelative("Value");

            if (keyProperty == null || valueProperty == null) return;

            float keyHeight = EditorGUI.GetPropertyHeight(keyProperty, true);
            float valueHeight = EditorGUI.GetPropertyHeight(valueProperty, true);
            float elementHeight = Mathf.Max(keyHeight, valueHeight);

            Rect keyRect = new Rect(position.x, position.y, position.width * 0.3f, elementHeight);
            Rect valueRect = new Rect(position.x + position.width * 0.3f, position.y, position.width * 0.7f, elementHeight);

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
                MakeReorderableList();
            }
        }

        protected virtual float KeyValuePairHeight(SerializedProperty serializedListProperty, int index)
        {
            var element = serializedListProperty.GetArrayElementAtIndex(index);
            var keyProperty = element.FindPropertyRelative("Key");
            var valueProperty = element.FindPropertyRelative("Value");
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
            MakeReorderableList();
        }

        protected virtual void RemoveItem(SerializedProperty serializedListProperty, ReorderableList list)
        {
            if (serializedListProperty.arraySize > 0)
            {
                serializedListProperty.DeleteArrayElementAtIndex(list.index);
                serializedListProperty.serializedObject.ApplyModifiedProperties();
                MakeReorderableList();
            }
        }

        protected bool IsDuplicate(int id)
        {
            bool[] duplicates = targetDictionary?.DuplicateValues;
            return duplicates != null && duplicates.Length > id && duplicates[id];
        }
    }
}