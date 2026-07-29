using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.Singletons;
using System.Linq;

namespace RageRooster.World
{
    /// <summary>
    /// A global registry asset of all <see cref="AreaAsset"/>s in the project.
    /// <br/> Used to access <see cref="AreaAsset"/>s at runtime by name or all at once.
    /// </summary>
    public class AreaRegistry : GlobalAsset<AreaRegistry>
    {
        [SerializeField] private AreaAsset[] areaAssets;



        private static bool dictionarybuilt = false;
        private static Dictionary<string, AreaAsset> dictionary;

        public override void OnInit()
        {
            if (Application.isPlaying && !dictionarybuilt) BuildDictionary();
            DestinationMap.Default = (DestinationMap)new Destination();
            DestinationMap.AllAreas = GetAll();
        }

        static void BuildDictionary()
        {
            dictionary = new Dictionary<string, AreaAsset>();
            foreach (var item in Get.areaAssets) dictionary.Add(item.name, item);
            dictionarybuilt = true;
        }

        /// <summary>
        /// Get an area in the registry by name.
        /// </summary>
        public static AreaAsset GetArea(string name)
        {
            if (!dictionarybuilt) BuildDictionary();
            return dictionary[name];
        }

        public static AreaAsset GetArea(int i) => Get.areaAssets[i];

        /// <returns>All areas in the registry.</returns>
        public static AreaAsset[] GetAll() => Get.areaAssets;


        private IDestination editorDestination = null;
        public static IDestination EditorDestination
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