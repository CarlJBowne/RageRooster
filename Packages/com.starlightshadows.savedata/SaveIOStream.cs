using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RageRooster.Core.Save;
using RageRooster.TOP.Save.Streams;
using RageRooster.World;
using SLS.SaveData;
using Unity.VisualScripting;
using UnityEngine;
using Utilities.JSON;

namespace SLS.SaveData
{
    /// <summary>
    /// An abstract SaveFile Saving/Loading System. One of these is kept in <see cref="SaveManager"/> for Saving, as well as one for every <see cref="SaveFile"/>
    /// </summary>
    public abstract class SaveIOStream : JsonStream
    {
        public abstract float version { get; }
        public abstract SaveableGeneric Transfer_Generic { get; }
        public abstract string filePath { get; }
        public abstract string fileName { get; }

        protected int fileID;
        protected JsonFile File;
        public SaveIOStream(int id, out JsonFile.FileState state)
        {
            fileID = id;
            File = new(filePath, fileName);
            JsonFile.FileState loadResult = File.LoadFromFile();
            state = loadResult;
            if(File["Version"].ToObject<float>() != version)
                state = JsonFile.FileState.WrongVersion;
        }

        public abstract void ExportMenuDisplayData(out object result);

    }

}