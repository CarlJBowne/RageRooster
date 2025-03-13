using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EditorAttributes;
using UnityEngine.UI;

public class SettingsMenu : MenuSingleton<SettingsMenu>, ICustomSerialized
{
    public RemappingMenu remap;

    public static string SaveFilePath => Application.persistentDataPath;
    public static string SaveFileName => "Config";

    // Sliders for adjusting volume levels
    [SerializeField] Slider masterSlider;
    [SerializeField] Slider musicSlider;
    [SerializeField] Slider SFXSlider;
    [SerializeField] Slider AmbSlider;

    // Current volume levels
    [SerializeField, DisableInEditMode, DisableInPlayMode] float currentMasterVolume;
    [SerializeField, DisableInEditMode, DisableInPlayMode] float currentMusicVolume;
    [SerializeField, DisableInEditMode, DisableInPlayMode] float currentSFXVolume;
    [SerializeField, DisableInEditMode, DisableInPlayMode] float currentAmbienceVolume;

    // Initializes the menu and reverts any changes
    protected override void Awake()
    {
        base.Awake();

        masterSlider.minValue = 0f;
        masterSlider.maxValue = 1f;
        masterSlider.value = 0.5f;

        musicSlider.minValue = 0f;
        musicSlider.maxValue = 1f;
        musicSlider.value = 0.5f;

        SFXSlider.minValue = 0f;
        SFXSlider.maxValue = 1f;
        SFXSlider.value = 0.5f;

        AmbSlider.minValue = 0f;
        AmbSlider.maxValue = 1f;
        AmbSlider.value = 0.5f;

        remap.TargetInput();
        RevertChanges();
    }

    // Sets the master volume to the specified input value
    public void SetMaster(float input)
    {
        currentMasterVolume = input;
        AudioManager.Get().masterVolume = input;
    }

    // Sets the music volume to the specified input value
    public void SetMusic(float input)
    {
        currentMusicVolume = input;
        AudioManager.Get().musicVolume = input;
        //if (AudioManager.Get().musicEventInstance.isValid())
        //    AudioManager.Get().musicEventInstance.setVolume(input);
    }

    // Sets the sound effects (SFX) volume to the specified input value
    public void SetSFX(float input)
    {
        currentSFXVolume = input;
        AudioManager.Get().SFXVolume = input;
    }

    // Sets the ambience volume to the specified input value
    public void SetAmbience(float input)
    {
        currentAmbienceVolume = input;
        AudioManager.Get().ambienceVolume = input;
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

        masterSlider.value = currentMasterVolume;
        musicSlider.value = currentMusicVolume;
        SFXSlider.value = currentSFXVolume;
        AmbSlider.value = currentAmbienceVolume;

        TrueClose();
    }

    // Serializes the current settings into a JSON token
    public JToken Serialize() => new JObject(
        new JProperty("V_Master", currentMasterVolume),
        new JProperty("V_Music", currentMusicVolume),
        new JProperty("V_SFX", currentSFXVolume),
        new JProperty("V_Amb", currentAmbienceVolume),
        new JProperty("Controls", remap.Serialize())
        );

    // Deserializes the settings from a JSON token and applies them
    public void Deserialize(JToken Data)
    {
        SetMaster(Data["V_Master"].As<float>());
        SetMusic(Data["V_Music"].As<float>());
        SetSFX(Data["V_SFX"].As<float>());
        SetAmbience(Data["V_Amb"].As<float>());
        remap.Deserialize(Data["Controls"]);
    }
}
