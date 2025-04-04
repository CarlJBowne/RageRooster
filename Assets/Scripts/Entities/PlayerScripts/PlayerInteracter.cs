using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Input;

/// <summary>
/// Now Combined with the Dialogue Trigger
/// </summary>
public class PlayerInteracter : MonoBehaviour
{
    public GameObject popupTransform;

    public List<IInteractable> interactablesInFront = new();

    private ConversationManager conversationManager;


    private void Awake()
    {
        conversationManager = ConversationManager.instance;
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.TryGetComponent(out IInteractable foundInteractable))
        {
            interactablesInFront.Add(foundInteractable);
            UpdateInteractableList();
        }

    }

    private void OnTriggerExit(Collider other)
    {
        if(other.TryGetComponent(out IInteractable foundInteractable) && interactablesInFront.Contains(foundInteractable))
        {
            interactablesInFront.Remove(foundInteractable);
            UpdateInteractableList();
        }
            
    }

    void UpdateInteractableList()
    {
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
