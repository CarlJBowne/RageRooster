using UnityEngine.UI;
using RageRooster.Settings;
using SLS.Singletons;
using SLS.MenuCore;
using SLS.GeneralUtilities.EventTickets;
using System.Collections.Generic;

public class SettingsMenu : Menu
{
    static Singleton<SettingsMenu> S;
    public static SettingsMenu Get => S.Get;
    public static bool TryGet(out SettingsMenu instance) => S.TryGet(out instance);
    public static bool Present => S.Active;

    static List<EventTicket> events;

    public RemappingMenu remap;

    public Slider volumeMasterSlider;
    public Slider volumeMusicSlider;
    public Slider volumeSFXSlider;
    public Slider volumeAmbSlider;
    public Slider brightnessSlider;

    protected override void Awake()
    {
        S.Register(this);
        base.Awake();

        GameSettings.Graphics.EstablishBrightnessOverlay();

        events = new()
        {
            GameSettings.Volume.Master.Subscribe(AudioManager.Get.SetMasterVolume),
            GameSettings.Volume.Music.Subscribe(AudioManager.Get.SetMusicVolume),
            GameSettings.Volume.SFX.Subscribe(AudioManager.Get.SetSFXVolume),
            GameSettings.Volume.Ambience.Subscribe(AudioManager.Get.SetAmbienceVolume),
            GameSettings.Graphics.Brightness.Subscribe
            (value => GameSettings.Graphics.brightnessOverlay.color = new(0, 0, 0, 1 - value))
        };

        GameSettings.Volume.Master.SetupSlider(volumeMasterSlider);
        GameSettings.Volume.Music.SetupSlider(volumeMusicSlider);
        GameSettings.Volume.SFX.SetupSlider(volumeSFXSlider);
        GameSettings.Volume.Ambience.SetupSlider(volumeAmbSlider);
        GameSettings.Graphics.Brightness.SetupSlider(brightnessSlider);


        remap.UpdateAllIcons();
    }


    // Confirms the changes made to the settings and saves them to a file
    public void ConfirmChanges()
    {
        GameSettings.SaveSettings();
        Close(false);
    }

    // Reverts any changes made to the settings and reloads the saved settings from a file
    public void RevertChanges()
    {
        GameSettings.LoadSettings();
        remap.UpdateAllIcons();

        Close(false);
    }

    protected override void OnDestroy()
    {
        S.Deregister(this);
        base.OnDestroy();
        events.DestroyAll();
    }

}
