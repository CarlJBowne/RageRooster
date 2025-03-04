using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteracter : PlayerStateBehavior
{
    public float radius = 1.5f;
    public float frontOffset = 2;

    /// <summary>
    /// Attempts to interact with something in front of the player.
    /// </summary>
    /// <returns>Whether the interaction happened.</returns>
    public bool TryInteract()
    {
        var interactCheck = Physics.OverlapSphere(playerMovementBody.center + playerMovementBody.transform.forward * frontOffset, 1.5f);
        for (int i = 0; i < interactCheck.Length; i++)
            if (interactCheck[i].TryGetComponent(out IInteractable foundInteractable))
            {
                foundInteractable.Interact();
                return true;
            }
        return false;
    }
}
