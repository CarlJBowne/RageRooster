using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using SLS.MenuCore;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using SLS.SaveFileCore;
using Utilities.Xtensions.Input;
using SLS.GeneralUtilities.Syncables;

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
            FileVersion.Options options = new()
            {
                DesiredFileVersion = typeof(ConfigVersion_10),
                AllFileVersions =
                {
                    {ConfigVersion_10.NUM, typeof(ConfigVersion_10)},
                    {ConfigVersion_UCF.NUM, typeof(ConfigVersion_UCF)},
                }
            };
            options.Initialize(ref stream, -1);

            {
                Volume.Master.Value = 1f;
                Volume.Music.Value = 1f;
                Volume.SFX.Value = 1f;
                Volume.Ambience.Value = 1f;
                Graphics.Brightness.Value = 1f;
            }
            stream.LoadFromFile();
        }
        public static void LoadSettings() => stream.LoadFromFile();
        public static void SaveSettings() => stream.SaveToFile();

        public static class Volume
        {
            public static FloatSyncableClamped Master = new(1f);
            public static FloatSyncableClamped Music = new(1f);
            public static FloatSyncableClamped SFX = new(1f);
            public static FloatSyncableClamped Ambience = new(1f);
        }

        public static class Graphics
        {
            public static FloatSyncableClamped Brightness = new(1f);
            public static Image brightnessOverlay;
            public static void EstablishBrightnessOverlay()
            {
                if (brightnessOverlay != null) return;
                if (Overlay.ActiveOverlays > 0)
                    brightnessOverlay = Overlay.OverALL.transform.parent.Find("BrightnessOverlay").GetComponent<Image>();
                Brightness.OnValueChanged += value => brightnessOverlay.color = new(0, 0, 0, 1 - value);
            }
        }

        static SLS.SaveFileCore.FileVersion stream;
        public abstract class FileVersion : SLS.SaveFileCore.FileVersion
        {
            public FileVersion() : base(-1) { }

            public override string version { get; }

            public override void Initialize()
            {
                saveRootPath = $"{Application.persistentDataPath}";
                RootFile = new JsonFile(saveRootPath, "Config");
            }
            public override void DeleteFile() => RootFile.DeleteFile();
            public override void ExportDisplayData(out object result) => throw new NotImplementedException();

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
