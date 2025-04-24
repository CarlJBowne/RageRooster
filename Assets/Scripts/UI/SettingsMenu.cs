using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EditorAttributes;
using UnityEngine.UI;
using System;
using static UnityEngine.Rendering.DebugUI;

public class SettingsMenu : MenuSingleton<SettingsMenu>, ICustomSerialized
{
    public RemappingMenu remap;

    public static string SaveFilePath => Application.persistentDataPath;
    public static string SaveFileName => "Config";

    public static Image brightnessOverlay;

    public FloatSetting masterVolume;
    public FloatSetting musicVolume;
    public FloatSetting SFXVolume;
    public FloatSetting ambienceVolume;
    public FloatSetting brightness;

    //public static bool skipIntro;

    // Initializes the menu and reverts any changes
    protected override void Awake() => Init();

    public void Init()
    {
        base.Awake();

        masterVolume.Init(value => { AudioManager.Get().masterVolume = value; });
        musicVolume.Init(value => { AudioManager.Get().musicVolume = value; });
        SFXVolume.Init(value => { AudioManager.Get().SFXVolume = value; });
        ambienceVolume.Init(value => { AudioManager.Get().ambienceVolume = value; });

        if (Overlay.ActiveOverlays.ContainsKey(Overlay.OverlayLayer.OverMenus))
        {
            if(brightnessOverlay == null) brightnessOverlay = Overlay.OverMenus.transform.Find("BrightnessOverlay").GetComponent<Image>();
            brightness.Init(value => { brightnessOverlay.color = new(0, 0, 0, value); });
        }

        //remap.TargetInput();
        RevertChanges();
    }

    // Confirms the changes made to the settings and saves them to a file
    public void ConfirmChanges()
    {
        Serialize().SaveToFile(SaveFilePath, SaveFileName);
        TrueClose();
    }

    // Reverts any changes made to the settings and reloads the saved settings from a file
    public void RevertChanges()
    {
        remap.ClearAllOverrides();
        var loadAttempt = new JObject().LoadJsonFromFile(SaveFilePath, SaveFileName);
        if (loadAttempt != null) Deserialize(loadAttempt);
        remap.UpdateAllIcons();

        TrueClose();
    }

    // Serializes the current settings into a JSON token
    public JToken Serialize(string name = null) => new JObject(
        masterVolume.Serialize("V_Master"), 
        musicVolume.Serialize("V_Music"),
        SFXVolume.Serialize("V_SFX"),
        ambienceVolume.Serialize("V_Amb"),
        brightness.Serialize("G_Brightness"),
        remap.Serialize("Controls")//,
        //new JProperty("SkipIntro", skipIntro)
        );

    // Deserializes the settings from a JSON token and applies them
    public void Deserialize(JToken Data)
    {
        masterVolume.Deserialize(Data["V_Master"]);
        musicVolume.Deserialize(Data["V_Music"]);
        SFXVolume.Deserialize(Data["V_SFX"]);
        ambienceVolume.Deserialize(Data["V_Amb"]);
        if (Data["G_Brightness"] != null) brightness.Deserialize(Data["G_Brightness"]);
        //if (Data["SkipIntro"] != null) skipIntro = Data["SkipIntro"].As<bool>();

        remap.Deserialize(Data["Controls"]);
    }



    public static JToken GetTempSettings() => new JObject().LoadJsonFromFile(SaveFilePath, SaveFileName);

    [System.Serializable]
    public class FloatSetting : ICustomSerialized
    {
        public Slider UISlider;
        public float defaultValue;
        public float currentValue { get; private set; }
        public float minValue;
        public float maxValue;
        public Action<float> ChangeAction;

        public void Init(Action<float> changeAction)
        {
            UISlider.maxValue = maxValue;
            UISlider.minValue = minValue;
            UISlider.onValueChanged.AddListener(Set);
            ChangeAction = changeAction;
            Set(defaultValue);
        }

        public void Set(float value)
        {
            currentValue = Mathf.Clamp(value, minValue, maxValue);
            UISlider.value = currentValue;
            ChangeAction?.Invoke(value);
        }

        public JToken Serialize(string name = null) => new JProperty(name, currentValue);
        public void Deserialize(JToken Data) => Set(Data.As<float>());
    }
}
