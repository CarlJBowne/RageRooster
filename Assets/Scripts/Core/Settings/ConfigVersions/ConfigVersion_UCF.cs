using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using SLS.SaveFileCore;
using static RageRooster.Settings.GameSettings;

namespace RageRooster.Settings
{
    public class ConfigVersion_UCF : GameSettings.FileVersion
    {
        public const string NUM = "UCF Release";
        public override string version => NUM;


        public override FileOpMessage LoadFromFile()
        {
            if(RootFile.LoadFromFile(out JObject Data).IfFail(out FileOpMessage result)) return result;

            Volume.Master &= (float)Data["V_Master"];
            Volume.Music &= (float)Data["V_Music"];
            Volume.SFX &= (float)Data["V_SFX"];
            Volume.Ambience &= (float)Data["V_Amb"];
            Graphics.Brightness &= (float)Data["G_Brightness"];
            if (Data.TryGetValue("Controls", out JToken ControlsJ))
                Remapping.Deserialize(ControlsJ);
            return result;
        }
        public override FileOpMessage SaveToFile() => throw new InvalidOperationException();

        public override FileOpMessage MatchVersion(out string version)
        {
            FileOpMessage fileLoadResult = RootFile.LoadFromFile(out JObject data);
            if (fileLoadResult != FileOpMessage.Success)
            {
                version = $"FILE LOAD ERROR : {fileLoadResult}";
                return fileLoadResult;
            }
            version = "Irrelevant";
            return FileOpMessage.Success;
        }

    }
}
