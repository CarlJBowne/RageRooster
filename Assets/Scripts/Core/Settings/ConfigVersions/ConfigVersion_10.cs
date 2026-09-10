using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json.Linq;
using SLS.SaveFileCore;
using static RageRooster.Settings.GameSettings;

namespace RageRooster.Settings
{
    public class ConfigVersion_10 : GameSettings.FileVersion
    {
        public const string NUM = "Unleashed 1.0.0";
        public override string version => NUM;

        public override FileOpMessage LoadFromFile()
        {
            if (RootFile.LoadFromFile(out JObject Data).IfFail(out FileOpMessage result)) return result;

            if (Data.TryGetValue("Volume", out JToken VolumeJ))
            {
                Volume.Master &= (float)VolumeJ["Master"];
                Volume.Music &= (float)VolumeJ["Music"];
                Volume.SFX &= (float)VolumeJ["SFX"];
                Volume.Ambience &= (float)VolumeJ["Ambience"];
            }

            if (Data.TryGetValue("Graphics", out JToken GraphicsJ))
                Graphics.Brightness &= (float)GraphicsJ["Brightness"];

            if (Data.TryGetValue("Controls", out JToken ControlsJ))
                Remapping.Deserialize(ControlsJ);

            return result;
        }
        public override FileOpMessage SaveToFile() => RootFile.SaveToFile(new()
        {
            ["FileVersion"] = 2.0f,
            ["Volume"] = new JObject()
            {
                ["Master"] = Volume.Master.Value,
                ["Music"] = Volume.Music.Value,
                ["SFX"] = Volume.SFX.Value,
                ["Ambience"] = Volume.Ambience.Value,
            },
            ["Graphics"] = new JObject()
            {
                ["Brightness"] = Graphics.Brightness.Value
            },
            ["Controls"] = Remapping.Serialized(),
        });

    }
}
