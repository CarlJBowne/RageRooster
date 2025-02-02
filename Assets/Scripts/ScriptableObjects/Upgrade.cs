using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Upgrade : ScriptableObject, ICustomSerialized
{
    public bool value;

    public static implicit operator bool (Upgrade upgrade) => upgrade.value;

    public JToken Serialize()
    {
        return new JObject
            (new JProperty("Enabled", value)
        );
    }

    public void Deserialize(JToken Data)
    {
        value = Data["Enabled"].Value<bool>();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}