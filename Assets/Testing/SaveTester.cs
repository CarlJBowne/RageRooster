using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EditorAttributes;
using Newtonsoft.Json.Linq;
using static UnityEngine.Rendering.DebugUI;

[CreateAssetMenu(fileName = "SaveTester", menuName = "ScriptableObjects/SaveTester")]
public class SaveTester : ScriptableObject
{
    public string path;
    public string fileName;
    public ScriptableCollection coll;

    [Button]
    public void Save() => coll.Serialize().SaveToFile(Application.dataPath + path, fileName);
    [Button]
    public void Load() => coll.Deserialize(new JObject().LoadJsonFromFile(Application.dataPath + path, fileName));
}

/*
public class Json
{
    public string raw;

    public Json() => raw = "";
    public Json(string input) => raw = input;
    public static implicit operator string(Json obj) => obj.raw;
    public static implicit operator Json(string input) => new(input);
    public static implicit operator Json(JToken input) => new(input.ToString());
    //public static explicit operator Json(object input) => new(input as string);

    public static Json LoadJsonFromFile(string path, string filename)
    {
        if (!Directory.Exists(path)) return null;
        if (!File.Exists($"{path}/{filename}.json")) return null;
        using StreamReader load = File.OpenText($"{path}/{filename}.json");
        return new(load.ReadToEnd());
    }
    public void SaveToFile(string path, string filename)
    {
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        using StreamWriter file = File.CreateText($"{path}/{filename}.json");
        file.WriteLine(raw);
    }

    //Improve more later (Add Indenting, make more Add functions, refactor to use "Begin/End Field" praxis, etc.)
    public class Builder : Json
    {
        public Builder() { raw = "{\n"; }

        public static Builder operator +(Builder B, string S)
        {
            B.raw += S;
            return B;
        }

        public void AddFieldHeader(string name) => this.raw += $"\"{name}\":";
        public void AddField(string name, object value, bool last = false) => this.raw += $"\"{name}\": {Serialize(value).raw}" + IsLast(last);
        public void AddString(string name, object value, bool last = false) => this.raw += $"\"{name}\": \"{value}\"" + IsLast(last);
        public void AddList<T>(string name, List<T> value, bool last = false)
        {
            string result = $"\"{name}\":\n[\n";
            for (int i = 0; i < value.Count; i++) result += Serialize(value[i]) + IsLast(i == value.Count-1);
            this.raw += result + "\n]" + IsLast(last) + "\n";
        }

        public Json Result()
        {
            raw += "\n}";
            return this;
        }

        public static string IsLast(bool yes) => yes ? "" : ",\n";
    }

    /// <summary>
    /// Serializes any object fed to it, including ones inheriting AND not inheriting from IJson.
    /// </summary>
    /// <param name="OBJ">The Object to be Serialized.</param>
    /// <returns>The Json representation.</returns>
    public static Json Serialize(object OBJ) => 
        typeof(ICustomSerialized).IsAssignableFrom(OBJ.GetType()) ? 
            (OBJ as ICustomSerialized).Serialize() : 
            (Json)JsonConvert.SerializeObject(OBJ);

    /// <summary>
    /// Deserializes this Json representation into the type desired. (Better know what you're doing.)
    /// </summary>
    /// <typeparam name="T">The Type of Object you're trying to obtain.</typeparam>
    /// <returns>The Deserialized Object.</returns>
    public T Deserialize<T>()
    {
        if (typeof(ICustomSerialized).IsAssignableFrom(typeof(T)))
        {
            var Result = Activator.CreateInstance<T>() as ICustomSerialized;
            Result.Deserialize(this);
            return (T)Result;
        }
        else if (raw[0] != '{' && raw[0] != '[') return new JRaw(raw).Value<T>();
        else return JsonConvert.DeserializeObject<T>(this);
    }
    /// <summary>
    /// Deserializes a Json representation into the type desired. (Better know what you're doing.)
    /// </summary>
    /// <typeparam name="T">The Type of Object you're trying to obtain.</typeparam>
    /// <param name="Data">The Json representation you're trying to Deserialize.</param>
    /// <returns>The Deserialized Object.</returns>
    public static T Deserialize<T>(Json Data)
    {
        if (typeof(ICustomSerialized).IsAssignableFrom(typeof(T)))
        {
            var Result = Activator.CreateInstance<T>() as ICustomSerialized;
            Result.Deserialize(Data);
            return (T)Result;
        }
        else if (Data.raw[0] != '{' && Data.raw[0] != '[') return new JRaw(Data).Value<T>();
        else return JsonConvert.DeserializeObject<T>(Data);
    }

    /// <summary>
    /// Attempts to Deserialize this Json representation into the desired type.
    /// </summary>
    /// <typeparam name="T">The Type of Object you're trying to obtain.</typeparam>
    /// <param name="result">The Deserialized Object.</param>
    /// <returns>Whether the Deserialization was successful.</returns>
    public bool TryDeserialize<T>(out T result)
    {
        if (typeof(ICustomSerialized).IsAssignableFrom(typeof(T)))
        {
            result = Activator.CreateInstance<T>();
            (result as ICustomSerialized).Deserialize(this);
        }
        else if (raw[0] != '{' && raw[0] != '[') result = new JRaw(raw).Value<T>();
        else result = JsonConvert.DeserializeObject<T>(this);
        return result != null;
    }
    /// <summary>
    /// Attempts to Deserialize a Json representation into the desired type.
    /// </summary>
    /// <typeparam name="T">The Type of Object you're trying to obtain.</typeparam>
    /// <param name="Data">The Json representation you're trying to Deserialize.</param>
    /// <param name="result">The Deserialized Object.</param>
    /// <returns>Whether the Deserialization was successful.</returns>
    public static bool TryDeserialize<T>(Json Data, out T result)
    {
        if(typeof(ICustomSerialized).IsAssignableFrom(typeof(T)))
        {
            result = Activator.CreateInstance<T>();
            (result as ICustomSerialized).Deserialize(Data);
        }
        else if (Data.raw[0] != '{' && Data.raw[0] != '[') result = new JRaw(Data).Value<T>();
        else result = JsonConvert.DeserializeObject<T>(Data);
        return result != null;
    }

    /// <summary>
    /// Deserializes this Json representation and populates an object with its data.
    /// </summary>
    /// <param name="target">The Object to be Populated.</param>
    public void DeserializeInto(object target)
    {
        var Custom = target as ICustomSerialized;
        if(Custom != null) Custom.Deserialize(this);
        else JsonConvert.PopulateObject(raw, target);
    }
    /// <summary>
    /// Deserializes a Json representation and populates an object with its data.
    /// </summary>
    /// <param name="input">The Json representation to be Deseralized.</param>
    /// <param name="target">The Object to be Populated.</param>
    public static void DeserializeInto(Json input, object target)
    {
        if (target is ICustomSerialized Custom) Custom.Deserialize(input);
        else JsonConvert.PopulateObject(input, target);
    }

    /// <summary>
    /// Deserializes a Json representation and populates an object with its data.
    /// </summary>
    /// <param name="input">The Json representation to be Deseralized.</param>
    /// <param name="target">The Object to be Populated.</param>
    public static void DeserializeInto(JToken input, object target)
    {
        if (target is ICustomSerialized Custom) Custom.Deserialize(input);
        else JsonConvert.PopulateObject(input.ToString(), target);
    }


    public Dictionary<string, Json> ToDictionary()
    {
        JObject X = JsonConvert.DeserializeObject<JObject>(this);
        Dictionary<string, Json> result = new();

        foreach (KeyValuePair<string, JToken> item in X) result.Add(item.Key, item.Value.ToString());
        return result;
    }
    public Json[] ToArray()
    {
        JToken[] X = JsonConvert.DeserializeObject<JToken[]>(this);
        Json[] result = new Json[X.Length];
        for (int i = 0; i < result.Length; i++) result[i] = X[i].ToString();
        return result;
    }
    public JToken ToJToken() => Deserialize<JToken>();

    public T Value<T>() => new JRaw(raw).Value<T>();
}*/