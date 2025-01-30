using EditorAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SavePoint : MonoBehaviour, IInteractable
{
    public Transform SpawnPoint;

    bool canInteract = true;
    bool IInteractable.canInteract => canInteract;

    
    public void Checkpoint()
    {
        Gameplay.spawnSceneName = gameObject.scene.name;
        Gameplay.spawnPointID = GetID();
    }
    
    public void Save() => GlobalState.Save();
    [Button]
    private void BeginFromHere()
    {
        GetComponentInParent<ZoneRoot>().SetSpawn(this);
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

    bool IInteractable.Interaction()
    {
        new CoroutinePlus(Save_CR(), this);
        return true;
    }
    private IEnumerator Save_CR()
    {
        Save();

        Light light = gameObject.GetOrAddComponent<Light>();
        light.type = LightType.Point;
        light.intensity = 10;
        light.color = Color.green;
        while(light.intensity > 0)
        {
            yield return null;
            light.intensity -= 0.1f; 
        }
    }

}

public interface IInteractable
{
    public bool Interact()
    {
        if (canInteract) return Interaction();
        else return false;
    }
    protected bool canInteract { get; }
    protected bool Interaction();
}