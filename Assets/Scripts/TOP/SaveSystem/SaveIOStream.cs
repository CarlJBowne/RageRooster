using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RageRooster.Core.Save;
using RageRooster.TOP.Save.Streams;
using RageRooster.World;
using Unity.VisualScripting;
using UnityEngine;
using Utilities.JSON;

namespace RageRooster.TOP.Save
{
    /// <summary>
    /// An abstract SaveFile Saving/Loading System. One of these is kept in <see cref="SaveManager"/> for Saving, as well as one for every <see cref="SaveFile"/>
    /// </summary>
    public abstract class SaveIOStream : JsonStream
    {
        public abstract float version { get; }
        public static SaveData Transfer => SaveManager.TransferSnapshot;

        protected int fileID;
        public SaveIOStream(int id, out JsonFile.FileState state)
        {
            fileID = id;
            state = JsonFile.FileState.FileEmpty;
        }

        public abstract void ExportMenuDisplayData(out SaveData.MenuDisplayData result);

    }

}