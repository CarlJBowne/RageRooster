using FMOD;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

//Note, this is unfinished.

namespace RageRooster.Systems.SaveSystem.Variables
{

    [Serializable]
    public class Variable
    {
        public enum Type
        {
            Bool,
            Int,
            Float,
            String,
            Vector2,
            Vector3
        }

        [SerializeField] private Type type = Type.Bool;

        [SerializeField] private bool valueBool;
        [SerializeField] private int valueInt;
        [SerializeField] private float valueFloat;
        [SerializeField] private string valueString;
        [SerializeField] private Vector2 valueVector2;
        [SerializeField] private Vector3 valueVector3;

        public Type VarType => type;

        public object Value
        {
            get => type switch
            {
                Type.Bool => valueBool,
                Type.Int => valueInt,
                Type.Float => valueFloat,
                Type.String => valueString,
                Type.Vector2 => valueVector2,
                Type.Vector3 => valueVector3,
                _ => null
            };
            set
            {
                switch (type)
                {
                    case Type.Bool: valueBool = Convert.ToBoolean(value); break;
                    case Type.Int: valueInt = Convert.ToInt32(value); break;
                    case Type.Float: valueFloat = Convert.ToSingle(value); break;
                    case Type.String: valueString = Convert.ToString(value); break;
                    case Type.Vector2: valueVector2 = (Vector2)value; break;
                    case Type.Vector3: valueVector3 = (Vector3)value; break;
                }
            }
        }

        public bool TryGetValue<T>(out T result)
        {
            if (EnumToType(type) != typeof(T))
            {
                result = default;
                return false;
            }
            result = (T)Value;
            return true;
        }

        public bool TrySetValue<T>(T value)
        {
            if (EnumToType(type) != typeof(T))
                return false;
            Value = value;
            return true;
        }

        public static Type TypeFromValue(object value)
        {
            return value switch
            {
                bool => Type.Bool,
                int => Type.Int,
                float => Type.Float,
                string => Type.String,
                Vector2 => Type.Vector2,
                Vector3 => Type.Vector3,
                _ => throw new ArgumentException("Unsupported type")
            };
        }

        public static System.Type EnumToType(Type type) => type switch
        {
            Type.Bool => typeof(bool),
            Type.Int => typeof(int),
            Type.Float => typeof(float),
            Type.String => typeof(string),
            Type.Vector2 => typeof(Vector2),
            Type.Vector3 => typeof(Vector3),
            _ => typeof(object)
        };

        public JProperty Serialize(string name) => new JProperty(name, Value);
    }



#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(Variable))]
    public class SavedVariableProprtyDrawer : PropertyDrawer
    {

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var typeProp = property.FindPropertyRelative("type");

            Variable.Type currentType = (Variable.Type)typeProp.enumValueIndex;
            var chosenType = (Variable.Type)EditorGUILayout.EnumPopup("Type", currentType);

            if (chosenType != currentType)
            {
                typeProp.enumValueIndex = (int)chosenType;
                currentType = chosenType;
            }

            // Draw the correct value field based on the selected type
            switch (currentType)
            {
                case Variable.Type.Bool:
                    var boolProp = property.FindPropertyRelative("valueBool");
                    boolProp.boolValue = EditorGUILayout.Toggle("Value", boolProp.boolValue);
                    break;
                case Variable.Type.Int:
                    var intProp = property.FindPropertyRelative("valueInt");
                    intProp.intValue = EditorGUILayout.IntField("Value", intProp.intValue);
                    break;
                case Variable.Type.Float:
                    var floatProp = property.FindPropertyRelative("valueFloat");
                    floatProp.floatValue = EditorGUILayout.FloatField("Value", floatProp.floatValue);
                    break;
                case Variable.Type.String:
                    var stringProp = property.FindPropertyRelative("valueString");
                    stringProp.stringValue = EditorGUILayout.TextField("Value", stringProp.stringValue);
                    break;
                case Variable.Type.Vector2:
                    var vector2Prop = property.FindPropertyRelative("valueVector2");
                    vector2Prop.vector2Value = EditorGUILayout.Vector2Field("Value", vector2Prop.vector2Value);
                    break;
                case Variable.Type.Vector3:
                    var vector3Prop = property.FindPropertyRelative("valueVector3");
                    vector3Prop.vector3Value = EditorGUILayout.Vector3Field("Value", vector3Prop.vector3Value);
                    break;
            }
        }
    }

#endif
















}
