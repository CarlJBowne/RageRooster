using System.Reflection;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Generics = System.Collections.Generic;

namespace SLS.StateMachineH.SerializedDictionary
{
    [CustomPropertyDrawer(typeof(SerializedDictionary<,>), true)]
    public class SerializedDictionaryDrawer : PropertyDrawer
    {

        static Generics.Dictionary<string, Instance> instanceDrawers;

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            PrepareInstance(property, default, property.propertyPath, label, fieldInfo);
            return instanceDrawers[property.propertyPath].GetPropertyHeight();
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            PrepareInstance(property, position, property.propertyPath, label, fieldInfo);
            instanceDrawers[property.propertyPath].OnGUI();
        }

        private void PrepareInstance(SerializedProperty property, Rect position, string propertyPath, GUIContent label, FieldInfo fieldInfo)
        {
            if (property.serializedObject.targetObject == null)
            {
                instanceDrawers?.Remove(propertyPath);
                return;
            }

            instanceDrawers ??= new Generics.Dictionary<string, Instance>();

            if (!instanceDrawers.TryGetValue(propertyPath, out Instance instance))
                instanceDrawers[propertyPath] = new Instance(position, property, label, fieldInfo, this);
            else instance.Update(position, property, label, fieldInfo);
        }


        protected class Instance
        {
            public Instance(Rect position, SerializedProperty property, GUIContent label, FieldInfo fieldInfo, SerializedDictionaryDrawer drawer)
            {
                this.drawer = drawer;
                TargetDictionary = fieldInfo.GetValue(property.serializedObject.targetObject) as ISerializedDictionaryNonGeneric;
                Update(position, property, label, fieldInfo, true);
            }

            public void Update(Rect position = default, SerializedProperty property = null, GUIContent label = null, FieldInfo fieldInfo = null, bool updateList = false)
            {
                if (property == null && this.property == null && EditorUtility.DisplayDialog("Oops", "Somehow a Serialized Dictionary wasn't initialized. Reset?", "Yes!", "No!?"))
                {
                    ClearInstancesOnReload();
                    return;
                }

                if (position != default) startingPosition = position;
                if (property != null) this.property = property;
                if (this.property != null) serializedListProperty = this.property.FindPropertyRelative("serializedList");
                if (label != null) this.label = label;
                if (fieldInfo != null) this.fieldInfo = fieldInfo;
                if (updateList) UpdateReorderableList();
            }



            public Rect startingPosition;
            public SerializedProperty property;
            public SerializedProperty serializedListProperty;
            public SerializedDictionaryDrawer drawer;
            public GUIContent label;
            public FieldInfo fieldInfo;
            public ISerializedDictionaryNonGeneric TargetDictionary;
            public ReorderableList reorderableList;


            public float GetPropertyHeight()
            {
                if (reorderableList == null || reorderableList.list == null || reorderableList.draggable == false) UpdateReorderableList();
                try
                {
                    return reorderableList.GetHeight();
                }
                catch (System.ArgumentNullException)
                {
                    try
                    {
                        Update();
                        return reorderableList.GetHeight();
                    }
                    catch (System.ArgumentNullException)
                    {
                        Debug.LogWarning("SerializedDictionaryDrawer: Could not get height for reorderable list. Returning default height.");
                        return EditorGUIUtility.singleLineHeight * 3.5f;
                    }
                }

            }
            public void OnGUI()
            {
                EditorGUI.BeginProperty(startingPosition, label, property);

                if (reorderableList == null || reorderableList.draggable == false) UpdateReorderableList();
                try
                {
                    reorderableList.DoList(startingPosition);
                }
                catch (System.ArgumentNullException X)
                {
                    Update();
                    reorderableList.DoList(startingPosition);
                }
                finally
                {
                    EditorGUI.EndProperty();
                }
            }
            public void UpdateReorderableList()
            {
                if (serializedListProperty == null) Update();
                if (serializedListProperty == null)
                {
                    Debug.LogWarning("SerializedDictionaryDrawer: Could not find 'serializedList' property.");
                    return;
                }
                Undo.RecordObject(property.serializedObject.targetObject, "Modify SerializedDictionary");

                bool[] duplicates = TargetDictionary.DuplicateValues;
                bool IsDupe(int id) => duplicates != null && duplicates.Length > id && duplicates[id];

                reorderableList = new ReorderableList(property.serializedObject, serializedListProperty);

                reorderableList.drawHeaderCallback = rect =>
                {
                    var newRect = new Rect(rect.x, rect.y, rect.width - 10, rect.height);
                    serializedListProperty.isExpanded = EditorGUI.Foldout(newRect, serializedListProperty.isExpanded, property.displayName, true);

                    //EditorGUI.LabelField(rect, property.displayName);
                    if (Event.current.type == EventType.ContextClick && rect.Contains(Event.current.mousePosition))
                    {
                        var menu = new GenericMenu();
                        menu.AddItem(new GUIContent("Clear"), false, () =>
                        {
                            serializedListProperty.ClearArray();
                            TargetDictionary.RecalculateOccurences();
                            serializedListProperty.serializedObject.ApplyModifiedProperties();
                            UpdateReorderableList();
                        });
                        menu.AddItem(new GUIContent("Remove Duplicates"), false, () =>
                        {
                            TargetDictionary.RemoveDuplicates();
                            TargetDictionary.RecalculateOccurences();
                            serializedListProperty.serializedObject.ApplyModifiedProperties();
                            UpdateReorderableList();
                        });
                        menu.ShowAsContext();
                        Event.current.Use();
                    }
                };

                reorderableList.drawElementCallback = (position, id, isActive, isFocused) =>
                {
                    if (!serializedListProperty.isExpanded) return;

                    drawer.KeyValuePairDrawer(serializedListProperty.GetArrayElementAtIndex(id), this, position, id, IsDupe(id));
                    //Look into more later.
                    //if (Event.current.type == EventType.ContextClick && position.Contains(Event.current.mousePosition))
                    //{
                    //    var menu = new GenericMenu();
                    //    menu.AddItem(new GUIContent("Copy Pair"), false, () =>
                    //    {
                    //        var element = serializedListProperty.GetArrayElementAtIndex(id);
                    //        var key = element.FindPropertyRelative("Key").stringValue;
                    //        var value = element.FindPropertyRelative("Value").stringValue;
                    //        EditorGUIUtility.systemCopyBuffer = $"{key}:{value}";
                    //    });
                    //    menu.AddItem(new GUIContent("Paste Pair"), false, () =>
                    //    {
                    //        var clipboard = EditorGUIUtility.systemCopyBuffer;
                    //        var parts = clipboard.Split(':');
                    //        if (parts.Length == 2)
                    //        {
                    //            var element = serializedListProperty.GetArrayElementAtIndex(id);
                    //            element.FindPropertyRelative("Key").stringValue = parts[0];
                    //            element.FindPropertyRelative("Value").stringValue = parts[1];
                    //            serializedListProperty.serializedObject.ApplyModifiedProperties();
                    //        }
                    //    });
                    //    menu.ShowAsContext();
                    //    Event.current.Use();
                    //}
                };

                reorderableList.elementHeightCallback = index => serializedListProperty.isExpanded
                                                                    ? drawer.KeyValuePairHeight(serializedListProperty, this, index)
                                                                    : 0;

                reorderableList.draggable = serializedListProperty.isExpanded;
                reorderableList.onSelectCallback = list =>
                {
                    if (list.index >= 0 && list.index < serializedListProperty.arraySize)
                    {
                        list.Select(list.index);
                    }
                };

                reorderableList.onChangedCallback = list => UpdateReorderableList();
                reorderableList.onAddCallback = list =>
                {
                    if (serializedListProperty.arraySize < 1)
                        serializedListProperty.InsertArrayElementAtIndex(0);
                    else
                        serializedListProperty.InsertArrayElementAtIndex(serializedListProperty.arraySize - 1);
                    serializedListProperty.serializedObject.ApplyModifiedProperties();
                    UpdateReorderableList();
                };
                reorderableList.onRemoveCallback = list =>
                {
                    if (serializedListProperty.arraySize > 0)
                    {
                        serializedListProperty.DeleteArrayElementAtIndex(list.index);
                        serializedListProperty.serializedObject.ApplyModifiedProperties();
                        UpdateReorderableList();
                    }
                };
                reorderableList.onReorderCallbackWithDetails = (list, oldID, newID) => UpdateReorderableList();

                property.serializedObject.ApplyModifiedProperties();
            }
        }

        protected virtual void KeyValuePairDrawer(SerializedProperty item, Instance drawerInstance, Rect position, int id, bool isDupe)
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
            if (isDupe) GUI.color = new Color(1.5f, 1, 1);

            try
            {
                EditorGUI.BeginChangeCheck();
                EditorGUI.PropertyField(keyRect, keyProperty, GUIContent.none);
            }
            finally { GUI.color = prevColor; }
            EditorGUI.PropertyField(valueRect, valueProperty, GUIContent.none);

            if (EditorGUI.EndChangeCheck())
            {
                drawerInstance.property.serializedObject.ApplyModifiedProperties();
                drawerInstance.Update(updateList: true);
            }
        }

        protected virtual float KeyValuePairHeight(SerializedProperty serializedListProperty, Instance drawerInstance, int index)
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

        [InitializeOnLoadMethod]
        private static void ClearInstancesOnReload() => instanceDrawers?.Clear();
    }
}
