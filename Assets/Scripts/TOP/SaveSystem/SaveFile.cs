using System;
using System.Collections.Generic;
using System.Text;
using RageRooster.TOP.Save.Streams;

namespace RageRooster.TOP.Save
{
    public class SaveFile : SLS.SaveFileCore.SaveFile
    {
        public override Dictionary<string, Type> AllFileVersions { get; } = new()
        {
            { FileVersion_10.NUM    ,  typeof(FileVersion_10    ) },
            { FileVersion_UCF.NUM   ,  typeof(FileVersion_UCF   ) }
        };

        public SaveFile(int fileNo) : base(fileNo) { }
    }

    public abstract class FileVersion : SLS.SaveFileCore.FileVersion
    {
        public FileVersion(int fileID) : base(fileID) => this.fileID = fileID;
        public int fileID { get; protected set; }
    }
}
