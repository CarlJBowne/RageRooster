#define AYellowPaper

using EditorAttributes;
using SLS.ISingleton;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "Global Prefabs", menuName = "Global Prefabs", order = 0)]
public class GlobalPrefabs : SingletonAsset<GlobalPrefabs>
{

#if AYellowPaper
    [SerializeField] AYellowpaper.SerializedCollections.SerializedDictionary<string, GameObject> dictionary;
#else

    [SerializeField] GameObject[] NamedPrefabs;
    [SerializeField] string[] PrefabNames;
    private Dictionary<string, GameObject> dictionary;

#endif

    public GameObject this[string name] => Get().dictionary[name];
    public static GameObject NamedPrefab(string name) => Get().dictionary[name];
    public static bool TryNamedPrefab(string name, out GameObject result) => Get().dictionary.TryGetValue(name, out result);

    public List<string> attackTagNames;
    protected override void OnInitialize()
    {
        base.OnInitialize();
        Attack.InitGlobalData(attackTagNames);
    }
    private void OnValidate() => Attack.InitGlobalData(attackTagNames);

    [Button]
    public void GetFromTagsEnum()
    {
        attackTagNames = new List<string>();
        for (int i = 0; i < 27; i++) attackTagNames.Add(((Attack.Tags)i).ToString());
        Attack.InitGlobalData(attackTagNames);
    }

}