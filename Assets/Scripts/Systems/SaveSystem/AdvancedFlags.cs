using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RageRooster.Systems.SaveSystem.Flags
{
    [System.Serializable]
    public abstract class FlagBase : ICloneable<FlagBase>
    {
        public abstract FlagTypes type { get; }

        public abstract bool TryGetValueObj(out object value);
        public abstract bool TrySetValueObj(object value);

        public abstract bool TryGetValue<T>(out T value);
        public abstract bool TrySetValue<T>(T value);

        public abstract FlagBase Clone(FlagBase target = null);

        public static FlagBase CreateInstanceFromEnum(FlagTypes type)
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

    public class Flag_Bool : FlagBase
    {
        public bool value;
        public override FlagTypes type => FlagTypes.Bool;
        public override bool TryGetValueObj(out object outValue)
        {
            outValue = value;
            return true;
        }
        public override bool TrySetValueObj(object value)
        {
            if (value is bool b)
            {
                this.value = b;
                return true;
            }
            return false;
        }
        public override bool TryGetValue<T>(out T outValue)
        {
            if (typeof(T) == typeof(bool))
            {
                outValue = (T)(object)value;
                return true;
            }
            outValue = default;
            return false;
        }
        public override bool TrySetValue<T>(T value)
        {
            if (value is bool b)
            {
                this.value = b;
                return true;
            }
            return false;
        }
        public override FlagBase Clone(FlagBase target = null)
        {
            if (target is not Flag_Bool t) t = new Flag_Bool();
            t.value = value;
            return t;
        }
    }

    public class Flag_Int : FlagBase
    {
        public int value;
        public override FlagTypes type => FlagTypes.Int;
        public override bool TryGetValueObj(out object outValue)
        {
            outValue = value;
            return true;
        }
        public override bool TrySetValueObj(object value)
        {
            if (value is int i)
            {
                this.value = i;
                return true;
            }
            return false;
        }
        public override bool TryGetValue<T>(out T outValue)
        {
            if (typeof(T) == typeof(int))
            {
                outValue = (T)(object)value;
                return true;
            }
            outValue = default;
            return false;
        }
        public override bool TrySetValue<T>(T value)
        {
            if (value is int i)
            {
                this.value = i;
                return true;
            }
            return false;
        }
        public override FlagBase Clone(FlagBase target = null)
        {
            if (target is not Flag_Int t) t = new Flag_Int();
            t.value = value;
            return t;
        }
    }

    public class Flag_Float : FlagBase
    {
        public float value;
        public override FlagTypes type => FlagTypes.Float;
        public override bool TryGetValueObj(out object outValue)
        {
            outValue = value;
            return true;
        }
        public override bool TrySetValueObj(object value)
        {
            if (value is float f)
            {
                this.value = f;
                return true;
            }
            return false;
        }
        public override bool TryGetValue<T>(out T outValue)
        {
            if (typeof(T) == typeof(float))
            {
                outValue = (T)(object)value;
                return true;
            }
            outValue = default;
            return false;
        }
        public override bool TrySetValue<T>(T value)
        {
            if (value is float f)
            {
                this.value = f;
                return true;
            }
            return false;
        }
        public override FlagBase Clone(FlagBase target = null)
        {
            if (target is not Flag_Float t) t = new Flag_Float();
            t.value = value;
            return t;
        }
    }

    public class Flag_Vector3 : FlagBase
    {
        public Vector3 value;
        public override FlagTypes type => FlagTypes.Vector3;
        public override bool TryGetValueObj(out object outValue)
        {
            outValue = value;
            return true;
        }
        public override bool TrySetValueObj(object value)
        {
            if (value is Vector3 v)
            {
                this.value = v;
                return true;
            }
            return false;
        }
        public override bool TryGetValue<T>(out T outValue)
        {
            if (typeof(T) == typeof(Vector3))
            {
                outValue = (T)(object)value;
                return true;
            }
            outValue = default;
            return false;
        }
        public override bool TrySetValue<T>(T value)
        {
            if (value is Vector3 v)
            {
                this.value = v;
                return true;
            }
            return false;
        }
        public override FlagBase Clone(FlagBase target = null)
        {
            if (target is not Flag_Vector3 t) t = new Flag_Vector3();
            t.value = value;
            return t;
        }
    }

    public class Flag_String : FlagBase
    {
        public string value;
        public override FlagTypes type => FlagTypes.String;
        public override bool TryGetValueObj(out object outValue)
        {
            outValue = value;
            return true;
        }
        public override bool TrySetValueObj(object value)
        {
            if (value is string s)
            {
                this.value = s;
                return true;
            }
            return false;
        }
        public override bool TryGetValue<T>(out T outValue)
        {
            if (typeof(T) == typeof(string))
            {
                outValue = (T)(object)value;
                return true;
            }
            outValue = default;
            return false;
        }
        public override bool TrySetValue<T>(T value)
        {
            if (value is string s)
            {
                this.value = s;
                return true;
            }
            return false;
        }
        public override FlagBase Clone(FlagBase target = null)
        {
            if (target is not Flag_String t) t = new Flag_String();
            t.value = value;
            return t;
        }
    }
}
