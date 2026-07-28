using UnityEngine.UI;
using RageRooster.Settings;
using SLS.Singletons;
using SLS.MenuCore;

public class SettingsMenu : Menu
{
    static Singleton<SettingsMenu> S;
    public static SettingsMenu Get => S.Get;
    public static bool TryGet(out SettingsMenu instance) => S.TryGet(out instance);
    public static bool Present => S.Active;

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

        GameSettings.Volume.Master.SetupSlider(volumeMasterSlider);
        GameSettings.Volume.Music.SetupSlider(volumeMusicSlider);
        GameSettings.Volume.SFX.SetupSlider(volumeSFXSlider);
        GameSettings.Volume.Ambience.SetupSlider(volumeAmbSlider);

        GameSettings.Graphics.Brightness.SetupSlider(brightnessSlider);
        GameSettings.Graphics.EstablishBrightnessOverlay();


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
    }

}
