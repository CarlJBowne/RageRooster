using System;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Utilities.JSON
{
    /// <summary>
    /// An Input Output stream for Saving/Loading Save Data to/from disk. Also used to display save files in UI.
    /// </summary>
    public abstract class JsonStream<T> where T : class, new()
    {
        public virtual void Init()
        {
            saveRootPath = UnityEngine.Application.persistentDataPath;
            RootFile = new(saveRootPath, $"Save");
            SecondaryFiles = new JsonFile[0];
        }
        protected JsonFile[] SecondaryFiles;
        protected JsonFile RootFile;

        public string saveRootPath;
        public bool filesDoExist
        {
            get
            {
                for (int i = 0; i < SecondaryFiles.Length; i++)
                    if (!SecondaryFiles[i].FileExists)
                        return false;
                return true;
            }
        }

        public JsonFile.LoadResult LoadFromFile(T ResultingData)
        {
            PreCheck();
            if (!RootFile.FileExists) return JsonFile.LoadResult.FileNotFound;
            for (int i = 0; i < SecondaryFiles.Length; i++)
                if (!SecondaryFiles[i].FileExists)
                    return JsonFile.LoadResult.FileNotFound;

            JsonFile.LoadResult rootFileLoadResult = RootFile.LoadFromFile();
            if (rootFileLoadResult != JsonFile.LoadResult.Success) return rootFileLoadResult;

            var fileVersionBehavior = FileVersionBehavior();
            if (fileVersionBehavior != JsonFile.LoadResult.Success) return fileVersionBehavior;

            for (int i = 0; i < SecondaryFiles.Length; i++)
            {
                JsonFile.LoadResult iFileResult = SecondaryFiles[i].LoadFromFile();
                if (iFileResult != JsonFile.LoadResult.Success) return iFileResult;
            }

            ResultingData = new();

            return ReadData(ResultingData);
        }

        public virtual JsonFile.LoadResult FileVersionBehavior()
        {
            //if ((string)RootFile.Data["FileVersion"] != targetFileVersion)
            //{
            //    UnityEngine.Debug.LogWarning($"Save file version mismatch. Expected {targetFileVersion}, found {(string)RootFile.Data/["FileVersion"]}. /Attempting to load anyway.");
            //}
            return JsonFile.LoadResult.Success;
        }

        protected abstract JsonFile.LoadResult ReadData(T ResultingData);


        public JsonFile.FileState SaveToFile(T sourceData)
        {
            PreCheck();

            JsonFile.FileState writeResult = WriteData(sourceData);
            if (writeResult != JsonFile.FileState.Valid) return writeResult;

            JsonFile.FileState resultState;

            resultState = RootFile.SaveToFile();
            if (resultState != JsonFile.FileState.Valid) return resultState;

            for (int i = 0; i < SecondaryFiles.Length; i++)
            {
                resultState = SecondaryFiles[i].SaveToFile();
                if (resultState != JsonFile.FileState.Valid) return resultState;
            }
            return resultState;
        }

        protected abstract JsonFile.FileState WriteData(T sourceData);

        public JsonFile.FileState DeleteFile()
        {
            PreCheck();
            RootFile.DeleteFile();
            for (int i = 0; i < SecondaryFiles.Length; i++) SecondaryFiles[i].DeleteFile();
            return JsonFile.FileState.Null;
        }

        public float GetCompletionPercentage()
        {
            PreCheck();
            int totalCollectibles = 1; // Replace with actual total collectible count later
            if (totalCollectibles == 0) return 100f;
            int collected = 0;

            return (collected / (float)totalCollectibles) * 100f;
        }

        public virtual JsonFile.FileState PreCheck() => JsonFile.FileState.Valid;
    }
}