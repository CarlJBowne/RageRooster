using EditorAttributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Upgrade : ScriptableObject, ICustomSerialized
{
    [SerializeField, DisableInEditMode, DisableInPlayMode]
    public bool value;
    public static implicit operator bool (Upgrade upgrade) => upgrade.value;

    [JsonIgnore]
    public bool defaultValue;



    private void OnEnable() => value = defaultValue;
    private void OnDisable() => value = defaultValue;

    public JToken Serialize()
        => new JObject
            (new JProperty("Enabled", value)
        );
    

    public void Deserialize(JToken Data)
    {
        value = Data["Enabled"].Value<bool>();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}