using RageRooster.Player;
using RageRooster.Core.Save;
using RageRooster.World;
using SLS.Singletons;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RageRooster.Core.Save;

namespace RageRooster.TOP.Save
{
    /// <summary>
    /// A Globally acessible registry for all saved values in the game. <br/>
    /// An Asset where defaults are defined and cloned from. DO NOT DELETE.
    /// </summary>
    [CreateAssetMenu(fileName = "SavedValueManager", menuName = "ScriptableObjects/SavedValueManager")]
    public class SavedValueRegistry : GlobalAsset<SavedValueRegistry>
    {
        [SerializeField] private PlayerStats playerStatsDef = new();
        [SerializeField] private SavedCollectible powerEggs = new();
        [SerializeField] private SavedCollectible wishbones = new();
        [SerializeField] private SavedCollectible hensRescued = new();
        [SerializeField] private SavedFlagSet globalFlagDefs;


        public override void OnInit()
        {
            SaveData.SavedValueManagerAsset = this;
            SaveData defs = new()
            {
                playerStats = playerStatsDef,
                progress = new()
                {
                    powerEggs = powerEggs,
                    wishbones = wishbones,
                    hensRescued = hensRescued,
                },
                globalChanges = globalFlagDefs,
                areaChanges = AreaRegistry.SavedFlagsDictionary()
            };
            this.GetExecutionDetails(out bool gameIsEditor, out bool gameIsPlaying);
            SaveData.InitializeSystem(defs, gameIsEditor && !gameIsPlaying);
        }
    }
}