using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EditorAttributes;
using RageRooster.Core.Save;
using RageRooster.Player;
using RageRooster.World;
using SLS.SaveData;
using SLS.Singletons;
using UnityEngine;

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
        [SerializeField] private Flag.Collection globalFlagDefs = new();
        [SerializeField] private Flag.BoolOnlyCollection storyFlagDefs = new();


        public override void OnInit()
        {
            SaveData.SavedValueManagerAsset = this;
            SaveData defs = new()
            {
                playerStats = playerStatsDef,
                progress = new()
                {
                    storyFlags = storyFlagDefs,
                    powerEggs = powerEggs,
                    wishbones = wishbones,
                    hensRescued = hensRescued,
                },
                flags = AreaRegistry.SavedFlagsDictionary()
            };
            defs.flags.Add("Global", globalFlagDefs);
            SaveData.InitializeDefaults(defs);
            Attack.InitGlobalData(attackTagNames);
        }

        public List<string> attackTagNames;
        [Button]
        public void GetFromTagsEnum()
        {
            attackTagNames = new List<string>();
            for (int i = 0; i < 27; i++) attackTagNames.Add(((Attack.Tags)i).ToString());
            Attack.InitGlobalData(attackTagNames);
        }
    }
}