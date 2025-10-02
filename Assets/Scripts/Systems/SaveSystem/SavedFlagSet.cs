using Newtonsoft.Json.Linq;
using SLS.StateMachineH.SerializedDictionary;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace RageRooster.Systems.SaveSystem.Flags
{
    [CreateAssetMenu(fileName = "SerializedFlagSet", menuName = "ScriptableObjects/SerializedFlagSet")]
    public class SavedFlagSet : ScriptableObject, ICloneable<SavedFlagSet>
    {
        [SerializeField]
        private FlagDictionary flags = new();

        public void LoadFromJson(JToken json)
        {
            foreach (var pair in flags)
                pair.Value.LoadFromJson((JValue)json[pair.Key]);
        }

        public JToken SaveToJson()
        {
            var result = new JObject();

            foreach (var pair in flags) result[pair.Key] = pair.Value.SaveToJson();
            return result;
        }


        public SavedFlagSet Clone(SavedFlagSet target = null)
        {
            if (target == null) target = Instantiate(this);
            else
            {
                foreach (string key in flags.Keys)
                    flags[key].Clone(target.flags[key]);
            }
            return target;
        }



        public bool TryGetFlag<T>(string key, out T value)
        {
            value = default;
            return flags.ContainsKey(key) && flags[key].TryGetValue(out value);
        }

        public bool TrySetFlag<T>(string key, T value) => flags.ContainsKey(key) && flags[key].TrySetValue(value);


        [System.Serializable]
        public class FlagDictionary : SerializedReferenceDictionary<string, Flag>
        {
            [CustomPropertyDrawer(typeof(FlagDictionary), true)]
            public class FlagDictionaryDrawer : SerializedDictionaryDrawer
            {
                float enumWidth = 70f;
                float spacing = 2f;
                float rowHeight => EditorGUIUtility.singleLineHeight;

                protected override void KeyValuePairDrawer(SerializedProperty item, Instance drawerInstance, Rect position, int id, bool isDupe)
                {
                    if (isDupe) GUI.color = redWarning;

                    Rect keyRect = new(position.x, position.y + 1, position.width, rowHeight);
                    Rect enumRect = new(position.x, position.y + rowHeight + spacing, enumWidth, rowHeight);
                    Rect valueRect = new(position.x + enumWidth + spacing, position.y + rowHeight + spacing, position.width - enumWidth - spacing, rowHeight);

                    EditorGUI.PropertyField(keyRect, item.FindPropertyRelative("Key"), GUIContent.none);

                    EditorGUI.BeginChangeCheck();

                    var flagProp = item.FindPropertyRelative("Value");
                    Flag flagObj = flagProp.managedReferenceValue as Flag;

                    Enum prevEnum = flagObj.type;
                    Enum enumOutput = EditorGUI.EnumPopup(enumRect, prevEnum);
                    if (!Equals(enumOutput, prevEnum))
                    {
                        flagProp.managedReferenceValue = Flag.CreateInstanceFromEnum((FlagTypes)enumOutput);
                        flagObj = flagProp.managedReferenceValue as Flag;
                        flagProp.serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(flagProp.serializedObject.targetObject);
                    }

                    object existingValue = flagObj.valueObject;
                    object inputValue = existingValue;

                    inputValue = flagObj.type switch
                    {
                        FlagTypes.Bool => EditorGUI.Toggle(valueRect, GUIContent.none, (bool)existingValue),
                        FlagTypes.Int => EditorGUI.DelayedIntField(valueRect, GUIContent.none, (int)existingValue),
                        FlagTypes.Float => EditorGUI.DelayedFloatField(valueRect, GUIContent.none, (float)existingValue),
                        FlagTypes.Vector3 => EditorGUI.Vector3Field(valueRect, GUIContent.none, (Vector3)existingValue),
                        FlagTypes.String => EditorGUI.DelayedTextField(valueRect, GUIContent.none, (string)existingValue),
                        _ => throw new System.Exception("Invalid Type.")
                    };

                    if (!Equals(inputValue, existingValue))
                    {
                        flagObj.TrySetValue(inputValue);
                        flagProp.managedReferenceValue = flagObj;
                    }

                    if (EditorGUI.EndChangeCheck())
                    {
                        flagProp.serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(flagProp.serializedObject.targetObject);
                    }

                    if (isDupe) GUI.color = Color.white;
                }
                protected override float KeyValuePairHeight(SerializedProperty serializedListProperty, Instance drawerInstance, int index)
                    => EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing + spacing;

                protected override void AddNewItem(SerializedProperty serializedListProperty, Instance drawerInstance, ReorderableList list)
                {
                    int place = serializedListProperty.arraySize > 0 ? serializedListProperty.arraySize - 1 : 0;

                    serializedListProperty.InsertArrayElementAtIndex(place);
                    serializedListProperty.serializedObject.ApplyModifiedProperties(); // <-- Ensure property tree is updated

                    var elementValue = serializedListProperty.GetArrayElementAtIndex(place).FindPropertyRelative("Value");
                    if (elementValue == null)
                    {
                        Debug.LogError("Could not find 'Value' property. Check your SerializedKeyValuePair definition and serialization attributes.");
                        return;
                    }
                    elementValue.managedReferenceValue = new Flag_Bool();

                    serializedListProperty.serializedObject.ApplyModifiedProperties();
                    drawerInstance.UpdateReorderableList();
                }
            }
        }
    }
}