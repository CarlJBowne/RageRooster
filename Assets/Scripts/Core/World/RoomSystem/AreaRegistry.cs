using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.Singletons;
using System.Linq;
using RageRooster.Core.Save;
using SLS.SaveFileCore;

namespace RageRooster.Core.World
{
    /// <summary>
    /// A global registry asset of all <see cref="AreaAsset"/>s in the project.
    /// <br/> Used to access <see cref="AreaAsset"/>s at runtime by name or all at once.
    /// </summary>
    public class AreaRegistry : GlobalAsset<AreaRegistry>
    {
        [SerializeField] private AreaAsset[] areaAssets;

        public static IReadOnlyDictionary<string, AreaAsset> Areas { get; private set; }
        public static IReadOnlyList<string> Names { get; private set; }
        public static IReadOnlyList<AreaAsset> All { get; private set; }

        public override void OnInit()
        {
            Dictionary<string, AreaAsset>  dictionary = new();
            foreach (AreaAsset item in areaAssets) dictionary.Add(item.name, item);
            Areas = dictionary;
            Names = dictionary.Keys.ToList();
            All = areaAssets.ToList();
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
            List<AreaAsset> areas = new List<AreaAsset>(This.areaAssets)
            {area};
            This.areaAssets = areas.ToArray();
            UnityEditor.EditorUtility.SetDirty(This);
        }
#endif

    }

}