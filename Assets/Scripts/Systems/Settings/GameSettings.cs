using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Utilities.JSON;
using Utilities.Xtensions.Input;

namespace RageRooster.Settings
{
    /// <summary>
    /// A more centrally stable and less error-prone way to manage settings. Untethered from the The <see cref="SettingsMenu"/> class and <see cref="RemappingMenu"/> classes, which now merely handle the UI. <br/>
    /// Note: Creating a new instance of this class won't do anything, as everything in this class is static.
    /// </summary>
    public class GameSettings
    {
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
            public static FloatSetting Master = new(1f, value => AudioManager.Get.masterVolume = value);
            public static FloatSetting Music = new(1f, value => AudioManager.Get.musicVolume = value);
            public static FloatSetting SFX = new(1f, value => AudioManager.Get.SFXVolume = value);
            public static FloatSetting Ambience = new(1f, value => AudioManager.Get.ambienceVolume = value);
        }

        public static class Graphics
        {
            public static FloatSetting Brightness = new(1f);
            static Image brightnessOverlay;
            public static void EstablishBrightnessOverlay()
            {
                if (brightnessOverlay != null) return;
                //if (Overlay.ActiveOverlays.ContainsKey(Overlay.OverlayLayer.OverMenus))
                //    brightnessOverlay = Overlay.OverALL.transform.Find("BrightnessOverlay").GetComponent<Image>();
                //Brightness.onChanged = value => brightnessOverlay.color = new(0, 0, 0, 1 - value);
            }
        }

        static IOStream stream;
        public class IOStream : JsonStream<GameSettings>
        {
            public IOStream()
            {
                saveRootPath = $"{Application.persistentDataPath}";
                base.RootFile = new JsonFile(saveRootPath, "Config");
                SecondaryFiles = new JsonFile[0];
            }

            protected override JsonFile.LoadResult ReadData(GameSettings ResultingData)
            {
                Debug.Log("Reading Config Data");
                float version = RootFile.Data["FileVersion"] != null ? RootFile.Data["FileVersion"].ToObject<float>() : 1.0f;
                JToken ControlsJ;

                if (version < 2.0f)
                {
                    Volume.Master.TakeSaveInput(RootFile["V_Master"]);
                    Volume.Music.TakeSaveInput(RootFile["V_Music"]);
                    Volume.SFX.TakeSaveInput(RootFile["V_SFX"]);
                    Volume.Ambience.TakeSaveInput(RootFile["V_Amb"]);
                    Graphics.Brightness.TakeSaveInput(RootFile["G_Brightness"]);
                    if (RootFile.Data.TryGetValue("Controls", out ControlsJ))
                        Remapping.Deserialize(ControlsJ);
                    return JsonFile.LoadResult.Success;
                }

                if (RootFile.Data.TryGetValue("Volume", out JToken VolumeJ))
                {
                    Volume.Master.TakeSaveInput(VolumeJ["Master"]);
                    Volume.Music.TakeSaveInput(VolumeJ["Music"]);
                    Volume.SFX.TakeSaveInput(VolumeJ["SFX"]);
                    Volume.Ambience.TakeSaveInput(VolumeJ["Ambience"]);
                }

                if (RootFile.Data.TryGetValue("Graphics", out JToken GraphicsJ))
                    Graphics.Brightness.TakeSaveInput(GraphicsJ["Brightness"]);

                if (RootFile.Data.TryGetValue("Controls", out ControlsJ))
                    Remapping.Deserialize(ControlsJ);

                return JsonFile.LoadResult.Success;
            }
            protected override JsonFile.FileState WriteData(GameSettings sourceData)
            {
                Debug.Log("Writing Config Data");

                RootFile.Data = new()
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
                };

                return JsonFile.FileState.Valid;
            }
        }

        public static class Remapping
        {
            public static JObject Serialized() => new()
            {
                ["Jump"] = SerializeReboundAction(Input.Jump),
                ["Attack"] = SerializeReboundAction(Input.Attack),
                ["Grab/Throw"] = SerializeReboundAction(Input.Grab),
                ["Parry"] = SerializeReboundAction(Input.Parry),
                ["Aim"] = SerializeReboundAction(Input.Aim),
                ["Sprint"] = SerializeReboundAction(Input.Charge1),
                ["Sprint Alt"] = SerializeReboundAction(Input.Charge2),
                ["Interact"] = SerializeReboundAction(Input.Interact),
            };
            public static JObject SerializeReboundAction(InputAction action) => new()
            {
                new JProperty("Gamepad", action.GetBindingOverridePath(group: "Gamepad")),
                new JProperty("Keyboard", action.GetBindingOverridePath(group: "Keyboard"))
            };
            public static void Deserialize(JToken Data)
            {
                DeserializeReboundAction(Input.Jump, Data["Jump"]);
                DeserializeReboundAction(Input.Attack, Data["Attack"]);
                DeserializeReboundAction(Input.Grab, Data["Grab/Throw"]);
                DeserializeReboundAction(Input.Parry, Data["Parry"]);
                DeserializeReboundAction(Input.Aim, Data["Aim"]);
                DeserializeReboundAction(Input.Charge1, Data["Sprint"]);
                DeserializeReboundAction(Input.Charge2, Data["Sprint Alt"]);
                DeserializeReboundAction(Input.Interact, Data["Interact"]);
            }
            public static void DeserializeReboundAction(InputAction action, JToken Data)
            {
                string gamepadPath = Data["Gamepad"]?.ToString();
                string keyboardPath = Data["Keyboard"]?.ToString();
                if (!string.IsNullOrEmpty(gamepadPath))
                    action.ApplyBindingOverride(gamepadPath, group: "Gamepad");
                if (!string.IsNullOrEmpty(keyboardPath))
                    action.ApplyBindingOverride(keyboardPath, group: "Keyboard");
            }

            public static void ClearAllBindingOverrides()
            {
                Input.Jump.RemoveAllBindingOverrides();
                Input.Attack.RemoveAllBindingOverrides();
                Input.Grab.RemoveAllBindingOverrides();
                Input.Parry.RemoveAllBindingOverrides();
                Input.Aim.RemoveAllBindingOverrides();
                Input.Charge1.RemoveAllBindingOverrides();
                Input.Charge2.RemoveAllBindingOverrides();
                Input.Interact.RemoveAllBindingOverrides();
            }

            public static void Remap(InputAction action, Action<bool> result)
            {
                InputActionRebindingExtensions.RebindingOperation rebind = action.PerformInteractiveRebinding()
                        .WithCancelingThrough("Escape")
                        .WithCancelingThrough("<Gamepad>/start")
                        .OnMatchWaitForAnother(.01f)
                        .WithTimeout(10f)
                        .SplitAcrossControlSchemes()
                        .OnComplete(op =>
                        {
                            op.Dispose();
                            result?.Invoke(true);
                        })
                        .OnCancel(op =>
                        {
                            op.Dispose();
                            result.Invoke(false);
                        })
                        .WithMatchingEventsBeingSuppressed()
                        .Start();
            }
        }
    }
}
