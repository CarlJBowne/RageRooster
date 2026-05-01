using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JToken = Newtonsoft.Json.Linq.JToken;

namespace Utilities.JSON
{
    public static class XtensionsJson
    {
        /// <summary>
        /// Serializes the input object into a JToken. 
        /// <br />For when you're unsure if the target object is an ICustomSerialized or not.
        /// <br />*Must be used with one a newly constructed JToken via new().
        /// </summary>
        /// <param name="OBJ">The Source Object to be Serialized.</param>
        /// <returns></returns>
        public static JToken Serialize(this JToken THIS, object OBJ)
        {
            THIS = typeof(ICustomSerialized).IsAssignableFrom(OBJ.GetType())
                ? (OBJ as ICustomSerialized).Serialize()
                : JObject.FromObject(OBJ);
            return THIS;
        }

        /// <summary>
        /// Deserializes this Token into the desired Type.
        /// <br />For when you're unsure if the target object is an ICustomSerialized or not.
        /// </summary>
        /// <typeparam name="T">The Type to Deserialize into.</typeparam>
        /// <returns>The Deserialized Value.</returns>
        public static T Deserialized<T>(this JToken THIS)
        {
            if (THIS == null) return default;
            if (typeof(ICustomSerialized).IsAssignableFrom(typeof(T)))
            {
                var Result = Activator.CreateInstance<T>() as ICustomSerialized;
                Result.Deserialize(THIS);
                return (T)Result;
            }
            else return THIS.ToObject<T>();
        }
        /// <summary>
        /// Attempts to Deserialize this Token into the desired Type.
        /// <br />For when you're unsure if the target object is an ICustomSerialized or not.
        /// </summary>
        /// <typeparam name="T">The Type to Deserialize into.</typeparam>
        /// <param name="result"></param>
        /// <returns>Whether the Deserialization was succesful.</returns>
        public static bool TryDeserialize<T>(this JToken THIS, out T result)
        {
            if (THIS == null) { result = default; return false; }
            if (typeof(ICustomSerialized).IsAssignableFrom(typeof(T)))
            {
                var IResult = Activator.CreateInstance<T>() as ICustomSerialized;
                IResult.Deserialize(THIS);
                result = (T)IResult;
            }
            else result = THIS.Value<T>();
            return result != null;
        }

        /// <summary>
        /// Populates an existing object using this Token.
        /// <br />For when you're unsure if the target object is an ICustomSerialized or not.
        /// </summary>
        /// <param name="target">The Target object.</param>
        public static bool DeserializeInto<T>(this JToken THIS, ref T target)
        {
            if (THIS == null) return false;
            target ??= Activator.CreateInstance<T>();
            if (target is ICustomSerialized Custom) Custom.Deserialize(THIS);
            else
                using (JsonReader sr = THIS.CreateReader())
                    JsonSerializer.CreateDefault().Populate(sr, target);
            return true;
        }

        /// <summary>
        /// Deserializes the JToken to the specified type and invokes the provided action with the result.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the JToken to.</typeparam>
        /// <param name="THIS">The JToken to deserialize.</param>
        /// <param name="Process">The action to invoke with the deserialized object.</param>
        /// <returns>true if deserialization was successful; otherwise, false.</returns>
        public static bool Deserializer<T>(this JToken THIS, Action<T> Process)
        {
            if (THIS != null)
            {
                Process(THIS.ToObject<T>());
                return true;
            }
            else return false;
        }
        /// <summary>
        /// Deserializes the JToken to the specified type and invokes the provided action with the result, or with the
        /// default value if the token is null.
        /// </summary>
        /// <typeparam name="T">The type to deserialize the JToken to.</typeparam>
        /// <param name="THIS">The JToken to deserialize.</param>
        /// <param name="Process">The action to invoke with the deserialized value.</param>
        /// <param name="defaultValue">The value to use if the JToken is null.</param>
        /// <returns>true if deserialization was successful; otherwise, false.</returns>
        public static bool Deserializer<T>(this JToken THIS, Action<T> Process, T defaultValue)
        {
            if (THIS != null)
            {
                Process(THIS.ToObject<T>());
                return true;
            }
            else
            {
                Process(defaultValue);
                return false;
            }
        }
        /// <summary>
        /// Executes the specified action on the object if it is not null.
        /// </summary>
        /// <typeparam name="T">The type of JToken to process.</typeparam>
        /// <param name="THIS">The JToken instance to process.</param>
        /// <param name="Process">The action to perform on the JToken.</param>
        /// <returns>true if the action was executed; otherwise, false.</returns>
        public static bool Process<T>(this T THIS, Action<T> Process) where T : JToken
        {
            if (THIS != null)
            {
                Process(THIS.ToObject<T>());
                return true;
            }
            else return false;
        }
    }

    public interface ICustomSerialized
    {

        /// <summary>
        /// Serializes the object into a JToken.
        /// <br />HEAVILY encouraged to create an implicit JToken operator redirecting to this for easier/faster conversion.
        /// </summary>
        /// <param name="name">Optional name for if serialized as a full Json Property</param>
        /// <returns>The Json representation.</returns>
        public JToken Serialize(string name = null);
        /// <summary>
        /// Deserializes a JToken and populates this object with its data.
        /// </summary>
        /// <param name="Data">The Json representation to be Deserialized.</param>
        public void Deserialize(JToken Data);

    }

    #region SerializableStructs

    public static class SerializableStructs
    {
        
        public static object Serializable(object input)
        {
            object result = input;

            if (input.GetType() == typeof(UnityEngine.Vector2)) result = (Vector2)(UnityEngine.Vector2)input;
            else if (input.GetType() == typeof(UnityEngine.Vector3)) result = (Vector3)(UnityEngine.Vector3)input;
            else if (input.GetType() == typeof(UnityEngine.Vector4)) result = (Vector4)(UnityEngine.Vector4)input;

            return result;
        }

        [System.Serializable]
        public struct Vector2
        {
            public float x;
            public float y;
            public Vector2(float x, float y)
            {
                this.x = x;
                this.y = y;
            }
            public static implicit operator UnityEngine.Vector2(Vector2 v) => new(v.x, v.y);
            public static explicit operator Vector2(UnityEngine.Vector2 v) => new(v.x, v.y);
        }
        public static Vector2 Serializable(this UnityEngine.Vector2 v) => new(v.x, v.y);
        [System.Serializable]
        public struct Vector3
        {
            public float x;
            public float y;
            public float z;
            public Vector3(float x, float y, float z)
            {
                this.x = x;
                this.y = y;
                this.z = z;
            }
            public static implicit operator UnityEngine.Vector3(Vector3 v) => new(v.x, v.y, v.z);
            public static explicit operator Vector3(UnityEngine.Vector3 v) => new(v.x, v.y, v.z);
        }
        public static Vector3 Serializable(this UnityEngine.Vector3 v) => new(v.x, v.y, v.z);
        [System.Serializable]
        public struct Vector4
        {
            public float x;
            public float y;
            public float z;
            public float w;
            public Vector4(float x, float y, float z, float w)
            {
                this.x = x;
                this.y = y;
                this.z = z;
                this.w = w;
            }
            public static implicit operator UnityEngine.Vector4(Vector4 v) => new(v.x, v.y, v.z, v.w);
            public static explicit operator Vector4(UnityEngine.Vector4 v) => new(v.x, v.y, v.z, v.w);
        }
        public static Vector4 Serializable(this UnityEngine.Vector4 v) => new(v.x, v.y, v.z, v.w);
        

        public static JObject Serialize(this UnityEngine.Vector3 v) => new()
        {
            ["x"] = v.x,
            ["y"] = v.y,
            ["z"] = v.z,
        };
        public static UnityEngine.Vector3 Deserialize(this UnityEngine.Vector3 v, JObject input)
        {
            if (input == null)
            {
                v = UnityEngine.Vector3.zero;
                return v;
            }
            v.x = (float)input["x"];
            v.y = (float)input["y"];
            v.z = (float)input["z"];
            return v;
        }
        public static JObject Serialize(this UnityEngine.Vector2 v) => new()
        {
            ["x"] = v.x,
            ["y"] = v.y,
        };
        public static UnityEngine.Vector2 Deserialize(this UnityEngine.Vector2 v, JObject input)
        {
            if (input == null)
            {
                v = UnityEngine.Vector2.zero;
                return v;
            }
            v.x = (float)input["x"];
            v.y = (float)input["y"];
            return v;
        }
        public static JObject Serialize(this UnityEngine.Vector4 v) => new()
        {
            ["x"] = v.x,
            ["y"] = v.y,
            ["z"] = v.z,
            ["w"] = v.w,
        };
        public static UnityEngine.Vector4 Deserialize(this UnityEngine.Vector4 v, JObject input)
        {
            if (input == null)
            {
                v = UnityEngine.Vector4.zero;
                return v;
            }
            v.x = (float)input["x"];
            v.y = (float)input["y"];
            v.z = (float)input["z"];
            v.w = (float)input["w"];
            return v;
        }





    }

    #endregion

    //Generic FilePath class, intersting, but probably not useful.
    public struct FilePath
    {
        public string path;
        public string filename;
        public string extension;
        public FilePath(string path, string filename, string extension)
        {
            this.path = path;
            this.filename = filename;
            this.extension = extension;
        }
        public readonly string Fullpath => Path.Combine(path, $"{filename}.{extension}");
        public static implicit operator string(FilePath obj) => Path.Combine(obj.path, $"{obj.filename}.{obj.extension}");
    }
}