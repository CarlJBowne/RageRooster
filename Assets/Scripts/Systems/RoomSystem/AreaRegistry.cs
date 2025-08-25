using SLS.ISingleton;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RageRooster.RoomSystem
{
    public class AreaRegistry : SingletonAsset<AreaRegistry>
    {
        [SerializeField]
        private AreaAsset[] areaAssets;

        private static bool dictionarybuilt = false;
        private static Dictionary<string, AreaAsset> dictionary;

        protected override void OnInitialize()
        {
            if (Application.isPlaying && !dictionarybuilt) BuildDictionary();
        }

        static void BuildDictionary()
        {
            AreaRegistry This = Get();
            dictionary = new Dictionary<string, AreaAsset>();
            foreach (var item in This.areaAssets) dictionary.Add(item.name, item);
            dictionarybuilt = true;
        }

        public static AreaAsset GetArea(string name)
        {
            if (!dictionarybuilt) BuildDictionary();
            return dictionary[name];
        }

    }

}