using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EditorAttributes;

public class SettingsMenu : MenuSingleton<SettingsMenu>, ICustomSerialized
{
    public RemappingMenu remap;

    public static string SaveFilePath => Application.persistentDataPath;
    public static string SaveFileName => "Config";

    [SerializeField, DisableInEditMode, DisableInPlayMode] float currentMasterVolume;
    [SerializeField, DisableInEditMode, DisableInPlayMode] float currentMusicVolume;
    [SerializeField, DisableInEditMode, DisableInPlayMode] float currentSFXVolume;
    [SerializeField, DisableInEditMode, DisableInPlayMode] float currentAmbienceVolume;

    protected override void Awake()
    {
        base.Awake();
        RevertChanges();
    }


    public void SetMaster(float input)
    {
        currentMasterVolume = input;
        AudioManager.Get().masterVolume = input;
    }
    public void SetMusic(float input)
    {
        currentMusicVolume = input;
        AudioManager.Get().musicVolume = input;
        if (AudioManager.Get().musicEventInstance.isValid()) 
            AudioManager.Get().musicEventInstance.setVolume(input);
    }
    public void SetSFX(float input)
    {
        currentSFXVolume = input;
        AudioManager.Get().SFXVolume = input;
    }
    public void SetAmbience(float input)
    {
        currentAmbienceVolume = input;
        AudioManager.Get().ambienceVolume = input;
    }

    public void ConfirmChanges()
    {
        Serialize().SaveToFile(SaveFilePath, SaveFileName);
        TrueClose();
    }
    public void RevertChanges()
    {
        remap.ClearAllOverrides();
        var loadAttempt = new JObject().LoadJsonFromFile(SaveFilePath, SaveFileName);
        if(loadAttempt != null) Deserialize(loadAttempt);
        remap.UpdateAllIcons();
        TrueClose();
    }


    public JToken Serialize() => new JObject(
        new JProperty("V_Master", currentMasterVolume),
        new JProperty("V_Music", currentMusicVolume),
        new JProperty("V_SFX", currentSFXVolume),
        new JProperty("V_Amb", currentAmbienceVolume),
        new JProperty("Controls", remap.Serialize())
        );
    public void Deserialize(JToken Data)
    {
        SetMaster(Data["V_Master"].As<float>());
        SetMusic(Data["V_Music"].As<float>());
        SetSFX(Data["V_SFX"].As<float>());
        SetAmbience(Data["V_Amb"].As<float>());
        remap.Deserialize(Data["Controls"]);
    }
}
