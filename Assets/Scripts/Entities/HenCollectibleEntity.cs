using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HenCollectibleEntity : MonoBehaviour, IInteractable
{
    public WorldChange worldChange;

    bool IInteractable.canInteract => true;

    private void Awake()
    {
        if (worldChange.Enabled) gameObject.SetActive(false);
    }
    bool IInteractable.Interaction()
    {
        GlobalState.AddMaxAmmo(1);
        worldChange.Enable();
        gameObject.SetActive(false);
        return true;
    }
}
