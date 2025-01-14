using EditorAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SavePoint : MonoBehaviour
{
    public Transform SpawnPoint;
    [Button]
    public void Checkpoint()
    {
        Gameplay.spawnSceneName = gameObject.scene.name;
        Gameplay.spawnPointID = GetID();
    }
    [Button]
    public void Save() => GlobalState.Save();
    [Button]
    public void BeginFromHere()
    {
        GetComponentInParent<ZoneRoot>().loadID = GetID();
        UnityEditor.EditorApplication.isPlaying = true;
    }

    public int GetID()
    {
        ZoneRoot Root = GetComponentInParent<ZoneRoot>();
        int ID = -1;
        for (int i = 0; i < Root.spawns.Length; i++)
            if (Root.spawns[i] == this)
            {
                ID = i;
                break;
            }
        return ID;
    }



}
