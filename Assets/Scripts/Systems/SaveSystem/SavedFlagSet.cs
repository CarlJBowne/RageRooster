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
        public FlagDictionary flags = new();

        public void LoadFromJson(JToken json)
        {
            foreach (var pair in new List<KeyValuePair<string, FlagBase>>(flags))
            {
                if (json[pair.Key] is JToken token)
                {
                    Enum.TryParse((string)token["type"], out FlagTypes type);

                    if (type != pair.Value.type) continue;

                    pair.Value.TrySetValueObj(token["value"].ToObject<object>());
                }
            }
        }

        public JToken SaveToJson()
        {
            var result = new JObject();

            foreach (var pair in flags)
            {
                pair.Value.TryGetValueObj(out object value);
                result[pair.Key] = new JObject()
                {
                    ["type"] = pair.Value.type.ToString(),
                    ["value"] = (JToken)value
                };
            }
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







        [System.Serializable]
        public class FlagDictionary : SerializedReferenceDictionary<string, FlagBase>
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
                    FlagBase flagObj = flagProp.managedReferenceValue as FlagBase;

                    Enum prevEnum = flagObj.type;
                    Enum enumOutput = EditorGUI.EnumPopup(enumRect, prevEnum);
                    if (!Equals(enumOutput, prevEnum))
                    {
                        flagProp.managedReferenceValue = FlagBase.CreateInstanceFromEnum((FlagTypes)enumOutput);
                        flagObj = flagProp.managedReferenceValue as FlagBase;
                        flagProp.serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(flagProp.serializedObject.targetObject);
                    }

                    flagObj.TryGetValueObj(out object existingValue);
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
                        flagObj.TrySetValueObj(inputValue);
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