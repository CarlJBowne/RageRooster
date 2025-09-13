using AYellowpaper.SerializedCollections;
using System;
using UnityEngine;
using UnityEditor;
using Newtonsoft.Json.Linq;
using VarType = RageRooster.Systems.SaveSystem.SavedVariableSet.Variable.VarType;

namespace RageRooster.Systems.SaveSystem
{
    public class SavedVariableSet : ScriptableObject
    {
        public SerializedDictionary<string, Variable> flags;

        public class Variable
        {
            [SerializeField] private VarType type = VarType.Bool;
            public VarType Type => type;
            private object value = null;

            public enum VarType
            {
                Bool,
                Int,
                Float,
                Char,
                String,
                Vector2,
                Vector3,
            }

            public static Type EnumToType(VarType type) => type switch
            {
                VarType.Bool => typeof(bool),
                VarType.Int => typeof(int),
                VarType.Float => typeof(float),
                VarType.Char => typeof(char),
                VarType.String => typeof(string),
                VarType.Vector2 => typeof(Vector2),
                VarType.Vector3 => typeof(Vector3),
                _ => typeof(bool)
            };
            

            public JProperty Serialize(string name) => new JProperty(name, value);
        }
    }

#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(SavedVariableSet.Variable))]
    public class SavedVariableProprtyDrawer : PropertyDrawer
    {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            VarType currentType = (VarType)property.FindProperty("type").enumValueIndex;
            var chosenType = (VarType)EditorGUILayout.EnumPopup("Type", currentType);

            if(chosenType != currentType)
            {

















                currentType = chosenType;
            }

            object prevValue = property.serializedObject.FindProperty("value").objectReferenceValue;

            EditorGUI.BeginChangeCheck();
            object value = currentType switch 
            {   VarType.Bool => EditorGUILayout.Toggle("Value", (bool)prevValue),
                VarType.Int => EditorGUILayout.IntField("Value", (int)prevValue),
                VarType.Float => EditorGUILayout.FloatField("Value", (float)prevValue),
                VarType.Char => EditorGUILayout.TextField("Value", (string)prevValue)[0],
                VarType.String => EditorGUILayout.TextField("Value", (string)prevValue),
                VarType.Vector2 => EditorGUILayout.Vector2Field("Value", (Vector2)prevValue),
                VarType.Vector3 => EditorGUILayout.Vector3Field("Value", (Vector3)prevValue),
                _ => null
            };
            if(EditorGUI.EndChangeCheck())
                property.FindProperty("value").objectReferenceValue = value as UnityEngine.Object;
            



        }
    }

#endif

}
