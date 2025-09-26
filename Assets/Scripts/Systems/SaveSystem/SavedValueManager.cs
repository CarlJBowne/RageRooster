using AYellowpaper.SerializedCollections;
using RageRooster.Systems.SaveSystem;
using SLS.ISingleton;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RageRooster.Systems.SaveSystem
{

    [CreateAssetMenu(fileName = "SavedValueManager", menuName = "ScriptableObjects/SavedValueManager")]
    public class SavedValueManager : SingletonAsset<SavedValueManager>
    {
        [SerializeField] private List<string> powerEggs = new();
        [SerializeField] private List<string> wishbones = new();
        [SerializeField] private List<string> hensRescued = new();
        [SerializeField] private Upgrades upgradeDefaults = new();
        [SerializeField] private Flags.SavedFlagSet globalFlagDefaults;

        public static List<string> PowerEggs => Get().powerEggs;
        public static List<string> Wishbones => Get().wishbones;
        public static List<string> HensRescued => Get().hensRescued;
        public static Flags.SavedFlagSet GlobalFlagDefaults => Get().globalFlagDefaults;
        public static Upgrades Upgrades => Get().upgradeDefaults;

        [EditorAttributes.Button("TestSaveSystem")]
        public void TestSaveSystem()
        {
            SaveFile.IO.SetFileTarget(6);
            SaveFile S = new();
            SaveFile.IO.Save();
        }

    }
}