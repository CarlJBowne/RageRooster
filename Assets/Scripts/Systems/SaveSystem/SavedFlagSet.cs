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
                    float rowHeight = EditorGUIUtility.singleLineHeight;
                    float enumWidth = 80f;
                    float spacing = 5f;

                    Rect keyRect = new Rect(position.x, position.y, position.width, rowHeight);
                    Rect enumRect = new Rect(position.x, position.y + rowHeight + spacing, enumWidth, rowHeight);
                    Rect valueRect = new Rect(position.x + enumWidth + spacing, position.y + rowHeight + spacing, position.width - enumWidth - spacing, rowHeight);

                    EditorGUI.PropertyField(keyRect, item.FindPropertyRelative("Key"), GUIContent.none);

                    //EditorGUI.BeginChangeCheck();

                    var flagProp = item.FindPropertyRelative("Value");

                    FlagBase flagObj = flagProp.managedReferenceValue as FlagBase;

                    Enum enumOutput = EditorGUI.EnumPopup(enumRect, flagObj.type);
                    if (enumOutput != (Enum)flagObj.type)
                    {
                        flagProp.managedReferenceValue = FlagBase.CreateInstanceFromEnum((FlagTypes)enumOutput);
                        flagObj = flagProp.managedReferenceValue as FlagBase;
                    }



                    if (flagObj.type == FlagTypes.Bool)
                    {
                        flagObj.TryGetValue(out bool existingValue);
                        var input = EditorGUI.Toggle(valueRect, GUIContent.none, existingValue);
                        if(input != existingValue) Enforce();

                    }
                    else if (flagObj.type == FlagTypes.Int)
                    {
                        flagObj.TryGetValue(out int existingValue);
                        var input = EditorGUI.IntField(valueRect, GUIContent.none, existingValue);
                        if (input != existingValue) Enforce();
                    }
                    else if (flagObj.type == FlagTypes.Float)
                    {
                        flagObj.TryGetValue(out float existingValue);
                        var input = EditorGUI.FloatField(valueRect, GUIContent.none, existingValue);
                        if (input != existingValue) Enforce();
                    }
                    else if (flagObj.type == FlagTypes.Vector3)
                    {
                        flagObj.TryGetValue(out Vector3 existingValue);
                        var input = EditorGUI.Vector3Field(valueRect, GUIContent.none, existingValue);
                        if (input != existingValue) Enforce();
                    }
                    else if (flagObj.type == FlagTypes.String)
                    {
                        flagObj.TryGetValue(out string existingValue);
                        var input = EditorGUI.TextField(valueRect, GUIContent.none, existingValue);
                        if (input != existingValue) Enforce();
                    } 

                    void Enforce()
                    {
                        drawerInstance.property.serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(drawerInstance.property.serializedObject.targetObject);
                    }

                    //if (EditorGUI.EndChangeCheck()) drawerInstance.property.serializedObject.ApplyModifiedProperties();

                }
                protected override float KeyValuePairHeight(SerializedProperty serializedListProperty, Instance drawerInstance, int index)
                    => EditorGUIUtility.singleLineHeight * 2 + EditorGUIUtility.standardVerticalSpacing + 5;

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