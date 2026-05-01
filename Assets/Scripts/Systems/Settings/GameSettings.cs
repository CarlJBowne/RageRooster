using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Text;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using Utilities.JSON;

namespace RageRooster.Settings
{
    public class GameSettings
    {
        //Note, creating anything of this won't help.

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void Init()
        {
            stream = new();
            {
                Volume.Master.Value = 1f;
                Volume.Music.Value = 1f;
                Volume.SFX.Value = 1f;
                Volume.Ambience.Value = 1f;
                Graphics.Brightness.Value = 1f;
            }
            stream.LoadFromFile(null);
        }
        public static void LoadSettings() => stream.LoadFromFile(null);
        public static void SaveSettings() => stream.SaveToFile(null);

        public static class Volume
        {
            public static FloatSetting Master = new(1f, value =>  AudioManager.Get.masterVolume = value);
            public static FloatSetting Music = new(1f, value => AudioManager.Get.musicVolume = value);
            public static FloatSetting SFX = new(1f, value => AudioManager.Get.SFXVolume = value);
            public static FloatSetting Ambience = new(1f, value => AudioManager.Get.ambienceVolume = value);
        }

        public static class Graphics
        {
            public static Setting<float> Brightness = new(1f)
            {
                onChanged = value =>
                {
                    if (brightnessOverlay == null) brightnessOverlay = Overlay.OverMenus.transform.Find("BrightnessOverlay").GetComponent<Image>();
                    brightnessOverlay.color = brightnessOverlay.color.WithAlpha(1 - value);
                },
            };
            public static Image brightnessOverlay;
        }

        public static class Remapping
        {

        }


        static IOStream stream;
        public class IOStream : JsonSaveStream<GameSettings>
        {
            public IOStream()
            {
                savePath = $"{Application.persistentDataPath}";
                File = new JsonFile(savePath, "Config.json");
                SecondaryFiles = new JsonFile[0];
            }

            protected override JsonFile.LoadResult ReadToData(GameSettings ResultingData)
            {
                if (File.Data.TryGetValue("Volume", out JToken VolumeJ))
                {
                    Volume.Master.TakeSaveInput(VolumeJ["Master"]);
                    Volume.Music.TakeSaveInput(VolumeJ["Music"]);
                    Volume.SFX.TakeSaveInput(VolumeJ["SFX"]);
                    Volume.Ambience.TakeSaveInput(VolumeJ["Ambience"]);
                }
                else
                {
                    Volume.Master.TakeSaveInput(File["V_Master"]);
                    Volume.Music.TakeSaveInput(File["V_Music"]);
                    Volume.SFX.TakeSaveInput(File["V_SFX"]);
                    Volume.Ambience.TakeSaveInput(File["V_Amb"]);
                }

                if (File.Data.TryGetValue("Graphics", out JToken GraphicsJ))
                    Graphics.Brightness.TakeSaveInput(GraphicsJ["Brightness"]);
                else Graphics.Brightness.TakeSaveInput(GraphicsJ["G_Brightness"]);

                return JsonFile.LoadResult.Success;
            }
            protected override JsonFile.FileState WriteFromData(GameSettings sourceData)
            {
                File.Data = new()
                {
                    new JProperty("Volume", new JObject(
                        new JProperty("Master", Volume.Master.Value),
                        new JProperty("Music", Volume.Music.Value),
                        new JProperty("SFX", Volume.SFX.Value),
                        new JProperty("Ambience", Volume.Ambience.Value)
                        )),
                    new JProperty("Graphics", new JObject(
                        new JProperty("Brightness", Graphics.Brightness.Value)
                        ))
                };

                return JsonFile.FileState.Valid;
            }
        }

    }
}
