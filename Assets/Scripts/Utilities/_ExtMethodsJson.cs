using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using JToken = Newtonsoft.Json.Linq.JToken;

public static class _ExtMethodsJson
{
    /*
    /// <summary>
    /// Loads a JToken from a file with the specified path and filename.
    /// </summary>
    /// <param name="path">The path of the file.</param>
    /// <param name="filename">The filename.</param>
    /// <returns>The loaded JToken.</returns>
    public static JToken LoadJsonFromFile(string path, string filename)
    {
        if (!Directory.Exists(path)) return null;
        if (!File.Exists(new FilePath(path, filename, "json"))) return null;
        using StreamReader load = File.OpenText($"{path}/{filename}.json");
        return JObject.Parse(load.ReadToEnd());
    }
    /// <summary>
    /// Saves this JToken to a file with the specified path and filename.
    /// </summary>
    /// <param name="path">The path of the file.</param>
    /// <param name="filename">The filename.</param>
    public static void SaveToFile(this JToken THIS, string path, string filename)
    {
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        using StreamWriter file = File.CreateText(new FilePath(path, filename, "json"));
        file.WriteLine(THIS.ToString());
    }
    */

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
    public static T Deserialize<T>(this JToken THIS)
    {
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
    public static void DeserializeInto(this JToken THIS, object target)
    {
        var Custom = target as ICustomSerialized;
        if (Custom != null) Custom.Deserialize(THIS);
        else
            using (JsonReader sr = THIS.CreateReader())
                JsonSerializer.CreateDefault().Populate(sr, target);
    }

    /// <summary>
    /// Converts and Returns the Value as the desired Type. ToObject is more efficient, really just kept for those who are lazy.
    /// </summary>
    /// <typeparam name="T">The Type to Convert to.</typeparam>
    /// <returns>A Converted Value.</returns>
    public static T As<T>(this JToken THIS) => THIS.ToObject<T>();

}



/// <summary>
/// A Json File representation. Stores a JToken. Includes simple functionality for Saving and Loading from file.
/// </summary>
public class JsonFile
{
    /// <summary>
    /// The directory path of the JSON file.
    /// </summary>
    public readonly string path;

    /// <summary>
    /// The name of the JSON file (without extension).
    /// </summary>
    public readonly string filename;

    /// <summary>
    /// The JToken representation of the JSON file's content.
    /// <br />Set
    /// </summary>
    public JToken Data;

    /// <summary>
    /// Gets the full path of the JSON file, including the filename and extension.
    /// </summary>
    public string FullPath => Path.Combine(path, $"{filename}.json");

    /// <summary>
    /// Implicitly accesses a JsonFile's JToken Data.
    /// </summary>
    /// <param name="THIS">The JsonFile instance.</param>
    public static implicit operator JToken(JsonFile THIS) => THIS.Data;

    /// <summary>
    /// Checks the state of the JSON file based on its content and path validity.
    /// </summary>
    public FileState State => Data == null
                                ? FileState.Null
                                : string.IsNullOrEmpty(path) || string.IsNullOrEmpty(filename)
                                    ? FileState.NoPath
                                    : FileState.Valid;

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonFile"/> class with the specified path and filename.
    /// </summary>
    /// <param name="path">The directory path of the JSON file.</param>
    /// <param name="filename">The name of the JSON file (without extension).</param>
    public JsonFile(string path, string filename)
    {
        this.path = path;
        this.filename = filename;
        Data = null;
    }


    /// <summary>
    /// Loads Json Data from the File specified by this JsonFile's path and filename.
    /// </summary>
    /// <returns>A <see cref="LoadResult"/> indicating the result of the load operation.</returns>
    public LoadResult LoadFromFile()
    {
        if (State == FileState.NoPath || !Directory.Exists(path)) return LoadResult.DirectoryNotFound;
        if (!File.Exists(FullPath)) return LoadResult.FileNotFound;

        using StreamReader load = File.OpenText(FullPath);
        string fileContent = load.ReadToEnd();

        if (string.IsNullOrWhiteSpace(fileContent)) return LoadResult.FileEmpty;

        try { Data = JObject.Parse(fileContent); }
        catch (JsonReaderException) { return LoadResult.FileCorrupted; }

        return LoadResult.Success;
    }

    /// <summary>
    /// Saves the current <see cref="Data"/> content to the file specified by this JsonFile's path and filename.
    /// </summary>
    /// <returns>A <see cref="FileState"/> indicating the result of the operation.</returns>
    public FileState SaveToFile()
    {
        FileState state = State;
        if (State != FileState.Valid) return state;
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        using StreamWriter file = File.CreateText(FullPath);
        file.WriteLine(Data);
        return state;
    }

    /// <summary>  
    /// Saves the specified <see cref="NewData"/> content to the file specified by this JsonFile's path and filename.  
    /// </summary>  
    /// <param name="NewData">Quick override to input new/changed data before save.</param>  
    /// <returns>A <see cref="FileState"/> indicating the result of the operation.</returns>  
    public FileState SaveToFile(JToken NewData)
    {
        Data = NewData;
        FileState state = State;
        if (State != FileState.Valid) return state;
        if (!Directory.Exists(path)) Directory.CreateDirectory(path);
        using StreamWriter file = File.CreateText(FullPath);
        file.WriteLine(Data);
        return state;
    }


    /// <summary>
    /// Deletes the file specified by this JsonFile's path and filename.
    /// </summary>
    public void DeleteFile()
    {
        if (State == FileState.NoPath || !Directory.Exists(path)) return;
        if (!File.Exists(FullPath)) return;
        File.Delete(FullPath);
    }


    /// <summary>
    /// Represents the state of the JsonFile.
    /// </summary>
    public enum FileState
    {
        /// <summary>
        /// The file is valid and ready for operations.
        /// </summary>
        Valid,

        /// <summary>
        /// The file content is null.
        /// </summary>
        Null,

        /// <summary>
        /// The file path or filename is invalid.
        /// </summary>
        NoPath
    }

    /// <summary>
    /// Represents the result of a load operation from a file.
    /// </summary>
    public enum LoadResult
    {
        /// <summary>
        /// The file was successfully loaded.
        /// </summary>
        Success,

        /// <summary>
        /// The file was not found at the specified path.
        /// </summary>
        FileNotFound,

        /// <summary>
        /// The directory containing the file was not found.
        /// </summary>
        DirectoryNotFound,

        /// <summary>
        /// The file is empty.
        /// </summary>
        FileEmpty,

        /// <summary>
        /// The file content is corrupted and could not be parsed.
        /// </summary>
        FileCorrupted
    }
}

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


//FAILED PRIOR IMPLEMENTATION, PAY NO MIND.
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