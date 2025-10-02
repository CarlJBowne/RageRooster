using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RageRooster.Systems.SaveSystem.Flags
{
    [System.Serializable]
    public abstract class Flag : ICloneable<Flag>
    {
        public bool TryGetValue<T>(out T value)
        {
            if (IsType<T>())
            {
                value = (T)valueObject;
                return true;
            }
            value = default;
            return false;
        }

        public bool TrySetValue<T>(T value)
        {
            if (IsType<T>())
            {
                valueObject = value;
                return true;
            }
            return false;
        }

        public bool IsType<T>() => type == TypeEnumFromCType<T>();
        public static FlagTypes TypeEnumFromCType<T>()
               => typeof(T) == typeof(bool) ? FlagTypes.Bool
                : typeof(T) == typeof(int) ? FlagTypes.Int
                : typeof(T) == typeof(float) ? FlagTypes.Float
                : typeof(T) == typeof(Vector3) ? FlagTypes.Vector3
                : typeof(T) == typeof(string) ? FlagTypes.String
                : throw new System.Exception("No matching FlagType for type " + typeof(T).Name);

        public abstract object valueObject { get; set; }
        public abstract FlagTypes type { get; }

        // Shared TrySetValue(object value) implementation
        public virtual bool TrySetValue(object value)
        {
            if(value == null || valueObject.GetType() != value.GetType()) return false;
            valueObject = value;
            return true;
        }

        public abstract Flag Clone(Flag target = null);

        public abstract void LoadFromJson(JToken input);
        public abstract JToken SaveToJson();

        public static Flag CreateInstanceFromEnum(FlagTypes type)
        {
            return type switch
            {
                FlagTypes.Bool => new Flag_Bool(),
                FlagTypes.Int => new Flag_Int(),
                FlagTypes.Float => new Flag_Float(),
                FlagTypes.Vector3 => new Flag_Vector3(),
                FlagTypes.String => new Flag_String(),
                _ => null,
            };
        }
    }

    public enum FlagTypes
    {
        Bool,
        Int,
        Float,
        Vector3,
        String,
    }

    public class Flag_Bool : Flag
    {
        public bool value;

        public override object valueObject
        {
            get => value;
            set { if (value is bool B) this.value = B; }
        }

        public override FlagTypes type => FlagTypes.Bool;

        public override Flag Clone(Flag target = null)
        {
            if (target is not Flag_Bool t) t = new Flag_Bool();
            t.value = value;
            return t;
        }
        public override JToken SaveToJson() => new JValue(value);
        public override void LoadFromJson(JToken input)
        {
            if (input == null || input.Type != JTokenType.Boolean)
                return;

            value = input.Value<bool>();
        }
    }

    public class Flag_Int : Flag
    {
        public int value;

        public override object valueObject
        {
            get => value;
            set { if (value is int B) this.value = B; }
        }

        public override FlagTypes type => FlagTypes.Int;
        public override Flag Clone(Flag target = null)
        {
            if (target is not Flag_Int t) t = new Flag_Int();
            t.value = value;
            return t;
        }
        public override JToken SaveToJson() => new JValue(value);
        public override void LoadFromJson(JToken input)
        {
            if (input == null || input.Type != JTokenType.Integer)
                return;

            value = input.Value<int>();
        }
    }

    public class Flag_Float : Flag
    {
        public float value;

        public override object valueObject
        {
            get => value;
            set { if (value is float B) this.value = B; }
        }

        public override FlagTypes type => FlagTypes.Float;

        public override Flag Clone(Flag target = null)
        {
            if (target is not Flag_Float t) t = new Flag_Float();
            t.value = value;
            return t;
        }
        public override JToken SaveToJson() => new JValue(value);
        public override void LoadFromJson(JToken input)
        {
            if (input == null || (input.Type != JTokenType.Float && input.Type != JTokenType.Integer))
                return;

            value = input.Value<float>();
        }
    }

    public class Flag_Vector3 : Flag
    {
        public Vector3 value;

        public override object valueObject
        {
            get => value;
            set { if (value is Vector3 B) this.value = B; }
        }

        public override FlagTypes type => FlagTypes.Vector3;
        public override Flag Clone(Flag target = null)
        {
            if (target is not Flag_Vector3 t) t = new Flag_Vector3();
            t.value = value;
            return t;
        }

        public override JToken SaveToJson() => value.Serialize();
        public override void LoadFromJson(JToken input) => value.Deserialize((JObject)input);
    }

    public class Flag_String : Flag
    {
        public string value;

        public override object valueObject
        {
            get => value;
            set { if (value is string B) this.value = B; }
        }

        public override FlagTypes type => FlagTypes.String;
        public override Flag Clone(Flag target = null)
        {
            if (target is not Flag_String t) t = new Flag_String();
            t.value = value;
            return t;
        }
        public override JToken SaveToJson() => new JValue(value);
        public override void LoadFromJson(JToken input)
        {
            if (input == null || (input.Type != JTokenType.String && input.Type != JTokenType.Null))
                return;

            value = input.Type == JTokenType.Null ? null : input.Value<string>();
        }
    }
}
