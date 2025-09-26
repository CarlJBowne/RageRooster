using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.UIElements;
using SLS.StateMachineH.SerializedDictionary;

namespace RageRooster.Systems.SaveSystem.Flags
{
    [CreateAssetMenu(fileName = "SerializedFlagSet", menuName = "ScriptableObjects/SerializedFlagSet")]
    public class SavedFlagSet : ScriptableObject, ICloneable<SavedFlagSet>
    {
        [SerializeField]
        public FlagDictionary flags = new();

        public void LoadFromJson(JToken json)
        {
            throw new System.NotImplementedException();
            //foreach (string key in new List<string>(flags.Keys))
            //    if (json[key] != null)
            //        flags[key] = (bool)json[key];
        }

        public SavedFlagSet Clone(SavedFlagSet target = null)
        {
            if (target == null) target = Instantiate(this);
            else
            {
                foreach (string key in new List<string>(target.flags.Keys))
                    target.flags[key] = flags[key];
            }
            return target;
        }







        [System.Serializable]
        public class FlagDictionary : SerializedReferenceDictionary<string, FlagBase>
        {
            [CustomPropertyDrawer(typeof(FlagDictionary), true)]
            public class FlagDictionaryDrawer : SerializedDictionaryDrawer
            {
                protected override void KeyValuePairDrawer(SerializedProperty item, Instance drawerInstance, Rect position, int id, bool isDupe)
                {
                    var keyProp = item.FindPropertyRelative("Key");
                    var valueProp = item.FindPropertyRelative("Value");
                    FlagBase valueObj = valueProp.managedReferenceValue as FlagBase;

                    float padding = 2f;
                    float rowHeight = EditorGUIUtility.singleLineHeight;
                    float enumWidth = 80f;
                    float spacing = 5f;

                    // First row: Key as string
                    Rect keyRect = new Rect(position.x, position.y + padding, position.width, rowHeight);
                    EditorGUI.PropertyField(keyRect, keyProp, GUIContent.none);

                    // Second row: Enum and value
                    Rect enumRect = new Rect(position.x, position.y + rowHeight + padding + spacing, enumWidth, rowHeight);
                    Rect valueRect = new Rect(position.x + enumWidth + spacing, position.y + rowHeight + padding + spacing, position.width - enumWidth - spacing, rowHeight);

                    Enum enumOutput = EditorGUI.EnumPopup(enumRect, valueObj.type);
                    if (enumOutput != (Enum)valueObj.type)
                    {
                        valueObj = FlagBase.CreateInstanceFromEnum((FlagTypes)enumOutput);
                        drawerInstance.property.serializedObject.ApplyModifiedProperties();
                    }
                    valueObj.Draw(valueRect, valueProp.FindPropertyRelative("value"));
                }
                protected override float KeyValuePairHeight(SerializedProperty serializedListProperty, Instance drawerInstance, int index)
                    => EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing;

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