using FMOD;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace RageRooster.Systems.SaveSystem.Variables
{


    public class Variable
    {
        [SerializeField] private Type type = Type.Bool;
        [SerializeField] private object value = null;
        public Type GetVarType => type;

        public bool GetValue<T>(out T result)
        {
            result = default;
            if (typeof(T) != EnumToType(type)) return false;
            result = (T)value;
            return true;
        }

        public bool SetValue<T>()
        {
            if (typeof(T) != EnumToType(type)) return false;
            value = default;
            return true;
        }

        public static System.Type EnumToType(Type type) => type switch
        {
            Type.Bool => typeof(bool),
            Type.Int => typeof(int),
            Type.Float => typeof(float),
            Type.Char => typeof(char),
            Type.String => typeof(string),
            Type.Vector2 => typeof(Vector2),
            Type.Vector3 => typeof(Vector3),
            _ => typeof(bool)
        };

        public JProperty Serialize(string name) => new JProperty(name, value);
    }

    public enum Type
    {
        Bool,
        Int,
        Float,
        Char,
        String,
        Vector2,
        Vector3
    }

#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(Variable))]
    public class SavedVariableProprtyDrawer : PropertyDrawer
    {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var typeProp = property.FindPropertyRelative("type");
            var valueProp = property.FindPropertyRelative("value");

            Type currentType = (Type)typeProp.enumValueIndex;
            var chosenType = (Type)EditorGUILayout.EnumPopup("Type", currentType);

            if (chosenType != currentType)
            {
                typeProp.enumValueIndex = (int)chosenType;
                valueProp.managedReferenceValue = null; // Correctly reset the value  
                currentType = chosenType;
            }

            object prevValue = valueProp.managedReferenceValue;

            EditorGUI.BeginChangeCheck();
            object value = currentType switch
            {
                Type.Bool => EditorGUILayout.Toggle("Value", prevValue is bool b ? b : false),
                Type.Int => EditorGUILayout.IntField("Value", prevValue is int i ? i : 0),
                Type.Float => EditorGUILayout.FloatField("Value", prevValue is float f ? f : 0f),
                Type.Char => EditorGUILayout.TextField("Value", prevValue is char c ? c.ToString() : string.Empty)[0],
                Type.String => EditorGUILayout.TextField("Value", prevValue as string ?? string.Empty),
                Type.Vector2 => EditorGUILayout.Vector2Field("Value", prevValue is Vector2 v2 ? v2 : Vector2.zero),
                Type.Vector3 => EditorGUILayout.Vector3Field("Value", prevValue is Vector3 v3 ? v3 : Vector3.zero),
                _ => null
            };

            if (EditorGUI.EndChangeCheck())
                valueProp.managedReferenceValue = value;
        }
    }

#endif
















}
