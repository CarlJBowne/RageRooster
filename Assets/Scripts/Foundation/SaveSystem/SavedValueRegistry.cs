using AYellowpaper.SerializedCollections;
using RageRooster.Systems.SaveSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities.Singletons;

namespace RageRooster.Systems.SaveSystem
{
    /// <summary>
    /// A Globally acessible registry for all saved values in the game. <br/>
    /// An Asset where defaults are defined and cloned from. DO NOT DELETE.
    /// </summary>
    [CreateAssetMenu(fileName = "SavedValueManager", menuName = "ScriptableObjects/SavedValueManager")]
    public class SavedValueRegistry : GlobalAsset<SavedValueRegistry>
    {
        [SerializeField] private List<string> powerEggs = new();
        [SerializeField] private List<string> wishbones = new();
        [SerializeField] private List<string> hensRescued = new();
        [SerializeField] private Upgrades upgradeDefaults = new();
        [SerializeField] private Flags.SavedFlagSet globalFlagDefaults;

        public static List<string> PowerEggs => Get.powerEggs;
        public static List<string> Wishbones => Get.wishbones;
        public static List<string> HensRescued => Get.hensRescued;
        public static Flags.SavedFlagSet GlobalFlagDefaults => Get.globalFlagDefaults;
        public static Upgrades Upgrades => Get.upgradeDefaults;
    }
}