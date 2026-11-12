using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.GeneralUtilities.Events;
using SLS.ListUtilities;
using static SLS.SaveFileCore.Flag;





#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SLS.SaveFileCore
{
    [Serializable]
    public abstract class Flag : Polymorph
    {
        public abstract object valueObject { get; set; }
        public abstract Type ValueType { get; }

        public abstract void LoadFromJson(JToken input);
        public abstract JToken SaveToJson();
        public abstract Flag Clone(Flag source);

        protected abstract void CallValueChangedCallback();

        public static implicit operator JToken(Flag source) => source.SaveToJson();
        /// <summary>
        /// psudeo assignment operator.
        /// </summary>
        public static Flag operator &(Flag flag, object value)
        {
            flag.valueObject = value;
            return flag;
        }


        //[Polymorph.ValidTypes(typeof(int), typeof(float), typeof(bool), typeof(UnityEngine.Vector3))]
        public abstract class Generic<T> : Flag
        {
            public T Value
            {
                get => value;
                set
                {
                    if ((this.value == null && value == null) || (this.value != null && this.value.Equals(value))) return;
                    this.value = value;
                    CallValueChangedCallback();
                }
            }
            public override object valueObject
            {
                get => value;
                set
                {
                    if (value.GetType() != typeof(T)) return;
                    this.value = (T)value;
                    CallValueChangedCallback();
                }
            }
            [SerializeField] protected T value;

            public override Type ValueType => typeof(T);


            public event Action<T> OnValueChanged;

            public EventTicket Subscribe(Action<T> callback, bool now = true)
                => new(() => OnValueChanged += callback, () => OnValueChanged -= callback, now);

            protected override void CallValueChangedCallback() => OnValueChanged?.Invoke(Value);

            public override Flag Clone(Flag source)
            {
                if (source == null) return this;
                if (source.valueObject.GetType() != typeof(T)) return this;
                valueObject = source.valueObject;
                return this;
            }
            public Generic<T> Clone(Generic<T> source)
            {
                if (source == null) return this;
                Value = source.Value;
                return this;
            }

            public static implicit operator T(Generic<T> input) => input.Value;
            public static Generic<T> operator &(Generic<T> flag, T value)
            {
                flag.Value = value;
                return flag;
            }
        }

        [Serializable]
        public class Bool : Generic<bool>
        {
            public override void LoadFromJson(JToken input)
            {
                if (input == null || input.Type != JTokenType.Boolean) return;
                Value = (bool)input;
            }
            public override JToken SaveToJson() => (JToken)Value;
        }
        [Serializable]
        public class Int : Generic<int>
        {
            public override void LoadFromJson(JToken input)
            {
                if (input == null || input.Type != JTokenType.Integer) return;
                Value = (int)input;
            }
            public override JToken SaveToJson() => (JToken)Value;
        }
        [Serializable]
        public class Float : Generic<float>
        {
            public override void LoadFromJson(JToken input)
            {
                if (input == null || input.Type != JTokenType.Float) return;
                Value = (float)input;
            }
            public override JToken SaveToJson() => (JToken)Value;
        }
        [Serializable]
        public class Vector3 : Generic<UnityEngine.Vector3>
        {
            public override void LoadFromJson(JToken input)
            {
                if (input == null || input is not JArray array || array.Count != 3) return;
                Value = new((float)array[0], (float)array[1], (float)array[2]);
            }
            public override JToken SaveToJson() => new JArray { value.x, value.y, value.z };
        }
        [Serializable]
        public class String : Generic<string>
        {
            public override void LoadFromJson(JToken input)
            {
                if (input == null || input.Type != JTokenType.String) return;
                Value = (string)input;
            }
            public override JToken SaveToJson() => (JToken)Value;
        }
        [Serializable]
        public class Char : Generic<char>
        {
            public override void LoadFromJson(JToken input)
            {
                if (input == null || input.Type != JTokenType.Integer) return;
                Value = (char)(int)input;
            }
            public override JToken SaveToJson() => (JToken)(int)Value;
        }

        [Serializable]
        public class Collection : Dictionary<Flag>
        {
            public Generic<T> Get<T>(string name)
            {
                if (TryGet(name, out Flag existing, true) && existing is Generic<T> f) return f;
                return null;
            }

            public bool TryGet<T>(string name, out Generic<T> value)
            {
                value = default;
                if (!ContainsKey(name)) return false;
                if (this[name].ValueType != typeof(T)) return false;
                if (this[name] is not Generic<T> gen) return false;

                value = gen;
                return true;
            }

            public EventTicket Subscribe<T>(string name, Action<T> callback, bool now = true) =>
                TryGet(name, out Generic<T> res) ? res.Subscribe(callback, now) : null;

            public string[] AllNamesOfType<T>() => AllNamesOfType(typeof(T));
            public string[] AllNamesOfType(Type type)
            {
                List<string> result = new();
                for (int i = 0; i < Count; i++)
                    if (Values[i].ValueType == type)
                        result.Add(Keys[i]);
                return result.ToArray();
            }

            public Generic<T> AddNew<T>(string name)
            {
                Flag newFlag = typeof(T) switch
                {
                    Type t when t == typeof(bool) => new Bool(),
                    Type t when t == typeof(int) => new Int(),
                    Type t when t == typeof(float) => new Float(),
                    Type t when t == typeof(UnityEngine.Vector3) => new Vector3(),
                    Type t when t == typeof(string) => new String(),
                    Type t when t == typeof(char) => new Char(),
                    _ => throw new ArgumentException($"Type {typeof(T)} is not a valid Flag type.")
                };
                this[name] = newFlag;
                return newFlag as Generic<T>;
            }
            public Flag AddNew(string name, Type type)
            {
                Flag newFlag = type switch
                {
                    Type t when t == typeof(bool) => new Bool(),
                    Type t when t == typeof(int) => new Int(),
                    Type t when t == typeof(float) => new Float(),
                    Type t when t == typeof(UnityEngine.Vector3) => new Vector3(),
                    Type t when t == typeof(string) => new String(),
                    Type t when t == typeof(char) => new Char(),
                    _ => throw new ArgumentException($"Type {type} is not a valid Flag type.")
                };
                this[name] = newFlag;
                return newFlag;
            }

            public void Clone(Collection source, DictionaryCloneOp op = DictionaryCloneOp.Transfer)
            {
                if (source == null) return;
                if (Count == 0) op = DictionaryCloneOp.TransferAndAdd;
                if (op is DictionaryCloneOp.ReplaceEntirely) Clear();
                for (int i = 0; i < source.Count; i++)
                {
                    if (!ContainsHash(source.Hashes[i]) && op is not DictionaryCloneOp.Transfer)
                        this.AddNew(source.Keys[i], source.Values[i].ValueType);
                    this.Add(source.Keys[i], Activator.CreateInstance(source.Values[i].GetType()) as Flag);
                    this[source.Keys[i]].Clone(source.Values[i]);
                }
            }

            public void LoadFromJson(JObject list)
            {
                if (!list.HasValues) return;
                foreach (JProperty prop in list.Properties())
                    if (ContainsKey(prop.Name))
                        this[prop.Name].LoadFromJson(prop.Value);
            }
            public JObject SaveToJson()
            {
                JObject result = new();
                for (int i = 0; i < Count; i++)
                    result.Add(new JProperty(Keys[i], Values[i].SaveToJson()));
                return result;
            }

            public float PercentageIsntDefault(float mult = 1f)
            {
                if (Count == 0) return 0;
                int notDefault = 0;
                for (int i = 0; i < Count; i++)
                    if (Values[i].valueObject != null && !Values[i].valueObject.Equals(default))
                        notDefault++;
                return (float)notDefault / (float)Count * mult;
            }
        }

        /*
        [Serializable]
        public class Boolean : FlagBase
        {
            public bool value;

            public override object valueObject
            {
                get => value;
                set { if (value is bool B) this.value = B; }
            }

            public override Type type => Type.Bool;

            public override FlagBase Clone(FlagBase target = null)
            {
                if (target is not Boolean t) t = new Boolean();
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
        [Serializable]
        public class Integer : FlagBase
        {
            public int value;

            public override object valueObject
            {
                get => value;
                set { if (value is int B) this.value = B; }
            }

            public override Type type => Type.Int;
            public override FlagBase Clone(FlagBase target = null)
            {
                if (target is not Integer t) t = new Integer();
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
        [Serializable]
        public class Float : FlagBase
        {
            public float value;

            public override object valueObject
            {
                get => value;
                set { if (value is float B) this.value = B; }
            }

            public override Type type => Type.Float;

            public override FlagBase Clone(FlagBase target = null)
            {
                if (target is not Float t) t = new Float();
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
        [Serializable]
        public class Vector3 : FlagBase
        {
            public UnityEngine.Vector3 value;

            public override object valueObject
            {
                get => value;
                set { if (value is UnityEngine.Vector3 B) this.value = B; }
            }

            public override Type type => Type.Vector3;
            public override FlagBase Clone(FlagBase target = null)
            {
                if (target is not Vector3 t) t = new Vector3();
                t.value = value;
                return t;
            }

            public override JToken SaveToJson()
            {
                return new JArray(value.x, value.y, value.z);
            }
            public override void LoadFromJson(JToken input)
            {
                JToken t = input;
                value.x = t[0].ToObject<float>();
                value.y = t[1].ToObject<float>();
                value.z = t[2].ToObject<float>();
            }
        }
        [Serializable]
        public class String : FlagBase
        {
            public string value;

            public override object valueObject
            {
                get => value;
                set { if (value is string B) this.value = B; }
            }

            public override Type type => Type.String;
            public override FlagBase Clone(FlagBase target = null)
            {
                if (target is not String t) t = new String();
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
        }*/
    }

}

/*
public class Vector3Object
{
    public float x, y, z;
    public Vector3Object(float x, float y, float z)
    {
        this.x = x;
        this.y = y;
        this.z = z;
    }
    public Vector3Object(Vector3 s)
    {
        this.x = s.x;
        this.y = s.y;
        this.z = s.z;
    }
    public static implicit operator Vector3Object(Vector3 v) => new(v);
    public static implicit operator Vector3(Vector3Object o) => new(o.x, o.y, o.z);
}
*/