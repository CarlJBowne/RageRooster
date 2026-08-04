using RageRooster.Player;
using RageRooster.SaveSystem;
using RageRooster.World;
using SLS.Singletons;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RageRooster.SaveSystem
{
    /// <summary>
    /// A Globally acessible registry for all saved values in the game. <br/>
    /// An Asset where defaults are defined and cloned from. DO NOT DELETE.
    /// </summary>
    [CreateAssetMenu(fileName = "SavedValueManager", menuName = "ScriptableObjects/SavedValueManager")]
    public class SavedValueRegistry : GlobalAsset<SavedValueRegistry>
    {
        [SerializeField] private PlayerStats playerStatsDef = new();
        [SerializeField] private List<string> powerEggs = new();
        [SerializeField] private List<string> wishbones = new();
        [SerializeField] private List<string> hensRescued = new();
        [SerializeField] private Flags.SavedFlagSet globalFlagDefs;

        public static List<string> PowerEggs => Get.powerEggs;
        public static List<string> Wishbones => Get.wishbones;
        public static List<string> HensRescued => Get.hensRescued;
        public static Flags.SavedFlagSet GlobalFlagDefs => Get.globalFlagDefs;

        public override void OnInit()
        {
            SaveData defs = new()
            {
                playerStats = playerStatsDef,
                progress = new()
                {
                    powerEggs = { isCollected = new bool[powerEggs.Count].ToList() },
                    wishbones = { isCollected = new bool[wishbones.Count].ToList() },
                    hensRescued = { isCollected = new bool[hensRescued.Count].ToList() },
                },
                globalChanges = globalFlagDefs,
                areaChanges = AreaRegistry.SavedFlagsDictionary()
            };
            SaveData.InitializeSystem(defs);
        }
    }
}