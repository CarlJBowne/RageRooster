using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using EditorAttributes;
using Newtonsoft.Json;
using System.Linq;
using JLinq = Newtonsoft.Json.Linq;
using static UnityEngine.Rendering.DebugUI;
using Unity.Jobs;

[CreateAssetMenu(fileName = "SaveTester", menuName = "ScriptableObjects/SaveTester")]
public class SaveTester : ScriptableObject
{
    public string path;
    public string fileName;
    public ScriptableCollection coll;

    [Button]
    public void Save()
    {
        coll.Serialize().SaveToFile(Application.dataPath + path, fileName);
        //SaveHelper.SaveSavable(coll, Application.dataPath + path, fileName);
    }
    [Button]
    public void Load()
    {
        coll.Deserialize(Json.LoadJsonFromFile(Application.dataPath + path, fileName));
        //SaveHelper.LoadObject(ref coll, Application.dataPath + path, fileName);
    }
}
public class Json
{
    public string value;

    public Json() => value = "";
    public Json(string input) => value = input;
    public static implicit operator string(Json obj) => obj.value;
    public static implicit operator Json(string input) => new(input);
    public static implicit operator Json(JLinq.JToken input) => new(input.ToString());

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
        file.WriteLine(value);
    }

    //Improve more later (Add Indenting, make more Add functions, refactor to use "Begin/End Field" praxis, etc.)
    public class Builder : Json
    {
        public Builder() { value = "{\n"; }

        public static Builder operator +(Builder B, string S)
        {
            B.value += S;
            return B;
        }

        public void AddFieldHeader(string name) => this.value += $"\"{name}\":";
        public void AddField(string name, object value, bool last = false) => this.value += $"\"{name}\": {Serialize(value).value}" + IsLast(last);
        public void AddString(string name, object value, bool last = false) => this.value += $"\"{name}\": \"{value}\"" + IsLast(last);
        public void AddList<T>(string name, List<T> value, bool last = false)
        {
            string result = $"\"{name}\":\n[\n";
            for (int i = 0; i < value.Count; i++) result += Serialize(value[i]) + IsLast(i == value.Count-1);
            this.value += result + "\n]" + IsLast(last) + "\n";
        }

        public Json Result()
        {
            value += "\n}";
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
        else return JsonConvert.DeserializeObject<T>(Data);
    }

    public Dictionary<string, object> DeserializeAll() => Deserialize<Dictionary<string, object>>();
    public static Dictionary<string, object> DeserializeAll(Json Data) => Data.Deserialize<Dictionary<string, object>>();


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
        else JsonConvert.PopulateObject(value, target);
    }
    /// <summary>
    /// Deserializes a Json representation and populates an object with its data.
    /// </summary>
    /// <param name="input">The Json representation to be Deseralized.</param>
    /// <param name="target">The Object to be Populated.</param>
    public static void DeserializeInto(Json input, object target)
    {
        var Custom = target as ICustomSerialized;
        if (Custom != null) Custom.Deserialize(input);
        else JsonConvert.PopulateObject(input, target);
    }

}

public interface ICustomSerialized
{
    /// <summary>
    /// Serializes the object into a Json representation. (Can be overridden along with its Deserialize Counterpart.)
    /// </summary>
    /// <returns> The Json representation.</returns>
    public Json Serialize();
    /// <summary>
    /// Deserializes a Json representation and populates this object with its data. (Can be overridden along with its Serialize Counterpart.)
    /// </summary>
    /// <param name="Data">The Json representation to be Deserialized.</param>
    public void Deserialize(Json Data);

}