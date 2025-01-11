using EditorAttributes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[System.Serializable]
public class WorldChange : ScriptableObject, ICustomSerialized
{
    [SerializeField, DisableInEditMode, DisableInPlayMode] private bool _enabled;

    private System.Action _activateAction;

    public bool Enabled => _enabled;
    [JsonIgnore]
    public System.Action Action => _activateAction;

    [JsonIgnore]
    public bool defaultValue;

    public void Activate()
    {
        _activateAction?.Invoke();
        _enabled = true;
    }
    public void Deactivate() => _enabled = false;

    public JToken Serialize()
    {
        return new JObject
            (new JProperty("Enabled", _enabled)
            );
    }

    public void Deserialize(JToken Data)
    {
        _enabled = Data["Enabled"].Value<bool>();
        EditorUtility.SetDirty(this);
    }
}
