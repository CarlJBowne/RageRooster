using Newtonsoft.Json.Linq;
using System;
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
            stream = new(0);
            {
                Volume.Master.Value = 1f;
                Volume.Music.Value = 1f;
                Volume.SFX.Value = 1f;
                Volume.Ambience.Value = 1f;
                Graphics.Brightness.Value = 1f;

            }
            stream.LoadFromFile(null);
        }

        [System.Serializable]
        public class Setting<T>
        {
            private T _value;
            public T defaultValue;
            public Action<T> onChanged;
            public Action<T> updateUI;
            public Func<T, T> validate;

            public Setting(T defaultValue, Action<T> onChanged = null, Action<T> updateUI = null, Func<T, T> validate = null)
            {
                _value = defaultValue;
                this.defaultValue = defaultValue;
                this.onChanged = onChanged;
                this.updateUI = updateUI;
            }

            public T Value
            {
                get => _value;
                set
                {
                    _value = value;
                    onChanged(value);
                    updateUI(value);
                }
            }
            public T ValueFromUI
            {
                get => _value;
                set
                {
                    _value = value;
                    onChanged(value);
                }
            }
            public static implicit operator T(Setting<T> This) => This._value;
        }

        public static class Volume
        {
            public static Setting<float> Master = new(1f)
            {
                onChanged = value => { AudioManager.Get.masterVolume = value; },
                validate = inValue => Mathf.Clamp(inValue, 0f, 1f)
            };
            public static Setting<float> Music = new(1f)
            {
                onChanged = value => { AudioManager.Get.musicVolume = value; },
                validate = inValue => Mathf.Clamp(inValue, 0f, 1f)
            };
            public static Setting<float> SFX = new(1f)
            {
                onChanged = value => { AudioManager.Get.SFXVolume = value; },
                validate = inValue => Mathf.Clamp(inValue, 0f, 1f)
            };
            public static Setting<float> Ambience = new(1f)
            {
                onChanged = value => { AudioManager.Get.ambienceVolume = value; },
                validate = inValue => Mathf.Clamp(inValue, 0f, 1f)
            };
        }

        public static class Graphics
        {
            public static Setting<float> Brightness = new(1f)
            {
                onChanged = value =>
                {
                    if(brightnessOverlay == null) brightnessOverlay = Overlay.OverMenus.transform.Find("BrightnessOverlay").GetComponent<Image>();
                    brightnessOverlay.color = brightnessOverlay.color.WithAlpha(1 - value);
                },
                validate = inValue => Mathf.Clamp(inValue, 0f, 1f)
            };
            private static Image brightnessOverlay;
        }

        public static class Remapping
        {

        }

        static IOStream stream;
        public class IOStream : JsonSaveStream<GameSettings>
        {
            public IOStream(int fileID) : base(fileID)
            {
            }

            protected override JsonFile.LoadResult ReadToData(JObject RootFileJ, GameSettings ResultingData)
            {
                if (RootFileJ.TryGetValue("Volume", out JToken VolumeJ))
                {
                    VolumeJ["Master"].Deserializer<float>(value => Volume.Master.Value = value);
                    VolumeJ["Music"].Deserializer<float>(value => Volume.Music.Value = value);
                    VolumeJ["SFX"].Deserializer<float>(value => Volume.SFX.Value = value);
                    VolumeJ["Ambience"].Deserializer<float>(value => Volume.Ambience.Value = value);
                }
                else
                {
                    RootFileJ["V_Master"].Deserializer<float>(value => Volume.Master.Value = value);
                    RootFileJ["V_Music"].Deserializer<float>(value => Volume.Music.Value = value);
                    RootFileJ["V_SFX"].Deserializer<float>(value => Volume.SFX.Value = value);
                    RootFileJ["V_Amb"].Deserializer<float>(value => Volume.Ambience.Value = value);
                }

                if(RootFileJ.TryGetValue("Graphics", out JToken GraphicsJ))
                {
                    GraphicsJ.Deserializer<float>(value => Graphics.Brightness.Value = value);
                }
                else RootFileJ["G_Brightness"].Deserializer<float>(value => Graphics.Brightness.Value = value);                    

                return JsonFile.LoadResult.Success;
            }
            protected override JsonFile.FileState WriteFromData(GameSettings sourceData)
            {

            }
        }

    }
}
