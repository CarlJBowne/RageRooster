using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-1)]
public class CullableEntity : MonoBehaviour
{
    //Config
    public float radius = 1.5f;
    public Behaviour[] components;

    //Data
    public bool active = false;
    public bool init = false;

    private void Awake()
    {
        if(!Gameplay.Active) return;
        Gameplay.EnemyCullingGroup.AddEnemyToCullingGroup(this);
        WithinRangeChange(false);
    }
    private void OnDestroy() 
    {
        if (!Gameplay.Active) return;
        Gameplay.EnemyCullingGroup.RemoveEnemyFromCullingGroup(this); 
    }

    public void WithinRangeChange(bool value)
    {
        if(!init) init = true;
        if (active == value && init) return;
        for (int i = 0; i < components.Length; i++)
            components[i].enabled = value;
        active = value;
    }
}