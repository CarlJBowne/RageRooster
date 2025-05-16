using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class GlobalState : Singleton<GlobalState>, ICustomSerialized
{

    public ScriptableCollection worldChanges;
    public ScriptableCollection upgrades;
    public WorldChange useIndoorSky;
    public WorldChange useSunsetSky;
    public Material daySkybox;
    public Material sunsetSkybox;
    public Material indoorSkybox;


    public static int currency = 0;
    public static int maxAmmo = 1;
    public static int maxHealth = 3;
    public static int activeSaveFile = 0;
    private static JsonFile File;

    public double saveFileTime;
    public static ScriptableCollection WorldChanges => Get().worldChanges;
    public static ScriptableCollection Upgrades => Get().upgrades;

    public static string SaveFilePath => Application.persistentDataPath + "/Saves";
    public static string SaveFileName => $"SaveFile{activeSaveFile}";

    private double lastLoadTime;

    public static System.Action maxAmmoUpdateCallback;
    public static System.Action currencyUpdateCallback;

    protected override void OnAwake()
    {
        useIndoorSky.Action += SetSkybox;
        useIndoorSky.deAction += SetSkybox;
    }

    protected override void OnDestroyed()
    {
        useIndoorSky.Action -= SetSkybox;
        useIndoorSky.deAction -= SetSkybox;
    }

    public static void InitializeSaveFile(int ID)
    {
        activeSaveFile = ID;
        File = new JsonFile(SaveFilePath, SaveFileName);
    }

    public static void Save() => File.SaveToFile(Get());

    public static void Load()
    {
        ResetData();

        if (File == null) InitializeSaveFile(0);
        if (File.LoadFromFile() == JsonFile.LoadResult.Success) Get().Deserialize(File.Data);

        PlayerHealth.Global.UpdateMax(maxHealth);
        PlayerRanged.Ammo.UpdateMax(maxAmmo);
        PlayerRanged.Ammo.Update(maxAmmo);
        UIHUDSystem.SetCurrencyText(currency.ToString());
        Get().SetSkybox(); 
    }

    public void Deserialize(JToken Data)
    {
        string savedZone = Data["CurrentZone"].As<string>();
        if(savedZone != null && savedZone != "") Gameplay.spawnSceneName = savedZone;

        Gameplay.spawnPointID = Data["SpawnPoint"].As<int>();

        currency = Data[nameof(currency)].As<int>();
        maxAmmo = Data[nameof(maxAmmo)].As<int>();
        maxHealth = Data[nameof(maxHealth)].As<int>();

        JToken timeToken = Data["Time"];
        saveFileTime = timeToken != null ? timeToken.As<double>() : 0;
        lastLoadTime = Time.time;

        worldChanges.Deserialize(Data[nameof(worldChanges)]);
        upgrades.Deserialize(Data[nameof(upgrades)]);
    }
    public JToken Serialize(string name = null) => new JObject(
        new JProperty("CurrentZone", Gameplay.spawnSceneName),
        new JProperty("SpawnPoint", Gameplay.spawnPointID),
        new JProperty("Time", saveFileTime + (Time.time - lastLoadTime)),
        new JProperty(nameof(currency), currency),
        new JProperty(nameof(maxAmmo), maxAmmo),
        new JProperty(nameof(maxHealth), maxHealth),
        new JProperty(nameof(worldChanges), worldChanges.Serialize()),
        new JProperty(nameof(upgrades), upgrades.Serialize())
        );
    public static implicit operator JToken(GlobalState THIS) => THIS.Serialize();

    public static void ResetData()
    {
        Gameplay.spawnSceneName = null;
        Gameplay.spawnPointID = -1;
        maxHealth = 3;
        maxAmmo = 1;
        currency = 0;

        foreach (Upgrade item in Upgrades.List) item.value = item.defaultValue;
        foreach (WorldChange item in WorldChanges.List) item.Enabled = item.defaultValue;
    }

    public static void AddCurrency(int currency)
    {
        GlobalState.currency += currency;
        if (GlobalState.currency < 0) GlobalState.currency = 0;
        UIHUDSystem.SetCurrencyText(GlobalState.currency > 0 ? GlobalState.currency.ToString() : "Broke.");
        currencyUpdateCallback?.Invoke();
    }

    [System.Obsolete]
    public static void AddMaxAmmo(int offset)
    {
        maxAmmo += offset;
        maxAmmoUpdateCallback?.Invoke();
    }

    public static void DeleteSaveFile(int id)
    {
        if(System.IO.File.Exists($"{SaveFilePath}/SaveFile{id}.json")) System.IO.File.Delete($"{SaveFilePath}/SaveFile{id}.json");
    }

    public void SetSkybox()
    {
        RenderSettings.skybox = useIndoorSky 
            ? indoorSkybox 
            : useSunsetSky 
                ? sunsetSkybox
                : daySkybox;
        DynamicGI.UpdateEnvironment();
    }

}
