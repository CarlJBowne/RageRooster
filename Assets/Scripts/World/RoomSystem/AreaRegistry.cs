using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.Singletons;
using System.Linq;
using RageRooster.Core.Save;
using SLS.SaveData;

namespace RageRooster.World
{
    /// <summary>
    /// A global registry asset of all <see cref="AreaAsset"/>s in the project.
    /// <br/> Used to access <see cref="AreaAsset"/>s at runtime by name or all at once.
    /// </summary>
    public class AreaRegistry : GlobalAsset<AreaRegistry>
    {
        [SerializeField] private AreaAsset[] areaAssets;



        private static Dictionary<string, AreaAsset> dictionary;
        private static Dictionary<string, Flag.Collection> savedFlagDictionary;

        public override void OnInit()
        {
            dictionary = new();
            foreach (AreaAsset item in areaAssets) dictionary.Add(item.name, item);
            savedFlagDictionary = new();
            foreach (AreaAsset item in areaAssets) savedFlagDictionary.Add(item.name, item.flagDefaults);
            DestinationMap.Default = (DestinationMap)new Destination();
            IDestination.AllAreas = areaAssets.Select(x => x.name).ToArray();
        }

        /// <summary>
        /// Get an area in the registry by name.
        /// </summary>
        public static AreaAsset GetArea(string name)
        {
            if(dictionary is null) Get.OnInit();
            return dictionary[name];
        }

        public static AreaAsset GetArea(int i) => Get.areaAssets[i];

        /// <returns>All areas in the registry.</returns>
        public static AreaAsset[] GetAll() => Get.areaAssets;

        public static Dictionary<string, Flag.Collection> SavedFlagsDictionary()
        {
            if (savedFlagDictionary is null) Get.OnInit();
            return savedFlagDictionary;
        }


        private Destination editorDestination = null;
        public static Destination EditorDestination
        {
            get => Get.editorDestination;
            set => Get.editorDestination = value;
        }


#if UNITY_EDITOR
        /// <summary>
        /// Adds an <see cref="AreaAsset"/> to the registry asset. EDITOR ONLY.
        /// </summary>
        /// <param name="area"></param>
        public static void Editor_AddArea(AreaAsset area)
        {
            AreaRegistry This = Get;
            var areas = new List<AreaAsset>(This.areaAssets)
            {area};
            This.areaAssets = areas.ToArray();
            UnityEditor.EditorUtility.SetDirty(This);
        }
#endif

    }

}