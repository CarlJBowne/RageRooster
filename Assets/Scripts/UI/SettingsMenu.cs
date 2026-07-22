using UnityEngine.UI;
using RageRooster.Settings;
using SLS.Singletons;

public class SettingsMenu : MenuSingleton<SettingsMenu>, IGlobalPrefab
{
    public RemappingMenu remap;

    public Slider volumeMasterSlider;
    public Slider volumeMusicSlider;
    public Slider volumeSFXSlider;
    public Slider volumeAmbSlider;
    public Slider brightnessSlider;

    protected override void OnInitialize()
    {
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
        TrueClose();
    }

    // Reverts any changes made to the settings and reloads the saved settings from a file
    public void RevertChanges()
    {
        GameSettings.LoadSettings();
        remap.UpdateAllIcons();

        TrueClose();
    }

}
