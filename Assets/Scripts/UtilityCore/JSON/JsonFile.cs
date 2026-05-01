using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Utilities.JSON
{
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
        public JObject Data;

        /// <summary>
        /// Gets the full path of the JSON file, including the filename and extension.
        /// </summary>
        public string FullPath => Path.Combine(path, $"{filename}.json");

        /// <summary>
        /// Implicitly accesses a JsonFile's JToken Data.
        /// </summary>
        /// <param name="THIS">The JsonFile instance.</param>
        public static implicit operator JObject(JsonFile THIS) => THIS.Data;

        public JToken this[string key]
        {
            get => Data[key];
            set => Data[key] = value;
        }


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
        public FileState SaveToFile(JObject NewData)
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
            Data = null;
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
            NoPath,

            /// <summary>
            /// Some other error occurred.
            /// </summary>
            Error
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

        public bool FileExists => Directory.Exists(path) && File.Exists(FullPath);
    }

    /// <summary>
    /// A derivable class representing a stream of one or more JsonFiles used to save and load a specific type of data. Contains abstract functions for reading and writing data to and from the files in the stream, built-in functionality for checking the presence of all files in the stream, as well as virtual functions for handling version mismatches. <br/>
    /// The Provided Type Parameter allows for the derived class to interlace itself with a given class type so instances of that class can be used as input and output for the stream's read and write functions.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class JsonSaveStream<T> where T : class, new()
    {
        public JsonSaveStream() => InitBasicSingleFile("File");
        protected void InitBasicSingleFile(string filename)
        {
            savePath = UnityEngine.Application.persistentDataPath;
            File = new(savePath, filename);
            SecondaryFiles = new JsonFile[0];
        }

        /// <summary>
        /// The Primary <see cref="JsonFile"/> for this stream. If a derived class uses multiple files, consider this the "root."
        /// </summary>
        protected JsonFile File;
        /// <summary>
        /// The Primary file path of this <see cref="JsonFileStream{T}"/>. If a derived class uses multiple files, this should point to the directory containing all files, and the individual <see cref="JsonFile"/> instances should point to their respective files within that directory.
        /// </summary>
        public string savePath;
        /// <summary>
        /// A collection of all Secondary Files used by this stream. Needs to be initialized in the Constructor to point to all <see cref="JsonFile"/>s used by this stream beyond the primary, as otherwise the stream will be unable to check for their presence prior to operations.
        /// </summary>
        protected JsonFile[] SecondaryFiles;

        public bool filesDoExist
        {
            get
            {
                if (!File.FileExists) return false;
                for (int i = 0; i < SecondaryFiles.Length; i++)
                    if (!SecondaryFiles[i].FileExists)
                        return false;
                return true;
            }
        }

        public JsonFile.LoadResult LoadFromFile(T ResultingData)
        {
            if (!File.FileExists) return JsonFile.LoadResult.FileNotFound;
            for (int i = 0; i < SecondaryFiles.Length; i++)
                if (!SecondaryFiles[i].FileExists)
                    return JsonFile.LoadResult.FileNotFound;

            JsonFile.LoadResult rootFileLoadResult = File.LoadFromFile();
            if (rootFileLoadResult != JsonFile.LoadResult.Success) return rootFileLoadResult;

            var fileVersionBehavior = FileVersionBehavior();
            if (fileVersionBehavior != JsonFile.LoadResult.Success) return fileVersionBehavior;

            for (int i = 0; i < SecondaryFiles.Length; i++)
            {
                JsonFile.LoadResult iFileResult = SecondaryFiles[i].LoadFromFile();
                if (iFileResult != JsonFile.LoadResult.Success) return iFileResult;
            }

            return ReadToData(ResultingData);
        }
        protected abstract JsonFile.LoadResult ReadToData(T ResultingData);


        public JsonFile.FileState SaveToFile(T sourceData)
        {
            JsonFile.FileState writeResult = WriteFromData(sourceData);
            if (writeResult != JsonFile.FileState.Valid) return writeResult;

            JsonFile.FileState resultState;

            resultState = File.SaveToFile();
            if (resultState != JsonFile.FileState.Valid) return resultState;

            for (int i = 0; i < SecondaryFiles.Length; i++)
            {
                resultState = SecondaryFiles[i].SaveToFile();
                if (resultState != JsonFile.FileState.Valid) return resultState;
            }
            return resultState;
        }
        protected abstract JsonFile.FileState WriteFromData(T sourceData);


        public virtual JsonFile.LoadResult FileVersionBehavior() =>
            //if ((string)RootFile.Data["FileVersion"] != targetFileVersion)
            //{
            //    UnityEngine.Debug.LogWarning($"Save file version mismatch. Expected {targetFileVersion}, found {(string)RootFile.Data/["FileVersion"]}. /Attempting to load anyway.");
            //}
            JsonFile.LoadResult.Success;
        public JsonFile.FileState DeleteFile()
        {
            File.DeleteFile();
            for (int i = 0; i < SecondaryFiles.Length; i++) SecondaryFiles[i].DeleteFile();
            return JsonFile.FileState.Null;
        }
    }
}