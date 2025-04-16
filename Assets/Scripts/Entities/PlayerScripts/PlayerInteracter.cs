using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Input;

/// <summary>
/// Now Combined with the Dialogue Trigger
/// </summary>
public class PlayerInteracter : Singleton<PlayerInteracter>
{
    public GameObject popupTransform;

    public List<IInteractable> interactablesInFront = new();

    private ConversationManager conversationManager;


    private void Awake()
    {
        conversationManager = ConversationManager.instance;
        Gameplay.PreReloadSave += ResetSystem;
    }

    private void OnTriggerEnter(Collider other)
    {if(other.TryGetComponent(out IInteractable foundInteractable)) FoundInteractable(foundInteractable);}

    private void OnTriggerExit(Collider other)
    {if (other.TryGetComponent(out IInteractable foundInteractable)) LostInteractable(foundInteractable);}


    public void FoundInteractable(IInteractable interactable)
    {
        interactablesInFront.Add(interactable);
        UpdateInteractableList();
    }
    public void LostInteractable(IInteractable interactable)
    {
        if (!interactablesInFront.Contains(interactable)) return;
        interactablesInFront.Remove(interactable);
        UpdateInteractableList();
    }


    void UpdateInteractableList()
    {
        while (interactablesInFront.Count > 0 && interactablesInFront[0] == null) interactablesInFront.RemoveAt(0);
        if (interactablesInFront.Count > 0)
        {
            popupTransform.SetActive(true);
            popupTransform.transform.position = interactablesInFront[0].PopupPosition;
        }
        else
        {
            popupTransform.SetActive(false);
        }
    }

    /// <summary>
    /// Attempts to interact with something in front of the player.
    /// </summary>
    /// <returns>Whether the interaction happened.</returns>
    public bool TryInteract()
    {
        if (interactablesInFront.Count > 0)
        {
            bool success = interactablesInFront[0].Interact();
            if(success) popupTransform.SetActive(false);
            return success;
        }
        else return false;
    }

    private void OnDestroy()
    {
        Gameplay.PreReloadSave -= ResetSystem;
    }

    void ResetSystem()
    {
        interactablesInFront.Clear();
    }







    //public float radius = 1.5f;
    //public float frontOffset = 2;

    /// <summary>
    /// Attempts to interact with something in front of the player.
    /// </summary>
    /// <returns>Whether the interaction happened.</returns>
    //public bool TryInteract()
    //{
    //    var interactCheck = Physics.OverlapSphere(playerMovementBody.center + playerMovementBody.transform.forward * frontOffset, 1.5f);
    //    for (int i = 0; i < interactCheck.Length; i++)
    //        if (interactCheck[i].TryGetComponent(out IInteractable foundInteractable))
    //        {
    //            foundInteractable.Interact();
    //            return true;
    //        }
    //    return false;
    //}
}
