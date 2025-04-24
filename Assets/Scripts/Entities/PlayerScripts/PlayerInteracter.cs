using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Input;

/// <summary>
/// Now Combined with the Dialogue Trigger
/// </summary>
public class PlayerInteracter : Singleton<PlayerInteracter>
{
    public static GameObject ThisGameObject;

    public GameObject popupTransform;
    public static GameObject PopupTransform;

    public static List<IInteractable> interactablesInFront = new();
    public static List<IGrabbable> grabbablesInFront = new();
    public static IGrabbable SelectedGrabbable
    {
        get => _selectedGrabbable;
        set
        {
            if(_selectedGrabbable != null) _selectedGrabbable.Selected = false;
            _selectedGrabbable = value;
            if (_selectedGrabbable != null) _selectedGrabbable.Selected = true;
        }
    }
    private static IGrabbable _selectedGrabbable;


    protected override void OnAwake()
    {
        Gameplay.PreReloadSave += ResetSystem;
        PopupTransform = popupTransform;
        ThisGameObject = gameObject;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out IInteractable foundInteractable)) FoundInteractable(foundInteractable);
        if (other.TryGetComponent(out IGrabbable foundGrabbable)) FoundGrabbable(foundGrabbable);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out IInteractable foundInteractable)) LostInteractable(foundInteractable);
        if (other.TryGetComponent(out IGrabbable foundGrabbable)) LostGrabbable(foundGrabbable);
    }


    public static void FoundInteractable(IInteractable interactable)
    {
        interactablesInFront.Add(interactable);
        UpdateInteractableList();
    }
    public static void LostInteractable(IInteractable interactable)
    {
        if (!interactablesInFront.Contains(interactable)) return;
        interactablesInFront.Remove(interactable);
        UpdateInteractableList();
    }
    public static void UpdateInteractableList()
    {
        while (interactablesInFront.Count > 0 && interactablesInFront[0] == null) interactablesInFront.RemoveAt(0);
        if (interactablesInFront.Count > 0)
        {
            PopupTransform.SetActive(true);
            PopupTransform.transform.position = interactablesInFront[0].PopupPosition;
        }
        else PopupTransform.SetActive(false);
    }


    public static void FoundGrabbable(IGrabbable grabbable)
    {
        grabbablesInFront.Add(grabbable.This);
        UpdateGrabbables();
    }
    public static void LostGrabbable(IGrabbable grabbable)
    {
        if (!grabbablesInFront.Contains(grabbable.This)) return;
        grabbablesInFront.Remove(grabbable.This);
        UpdateGrabbables();
    }
    public static void UpdateGrabbables()
    {
        while (grabbablesInFront.Count > 0 && grabbablesInFront[0] == null) grabbablesInFront.RemoveAt(0);
        for (int i = 0; i < grabbablesInFront.Count; i++)
            if (grabbablesInFront[i].IsGrabbable)
            {
                SelectedGrabbable = grabbablesInFront[i];
                return;
            }
        SelectedGrabbable = null;
    }

    public bool HasUsableGrabbable(out IGrabbable grabbable)
    {
        for (int i = 0; i < grabbablesInFront.Count; i++)
            if (grabbablesInFront[i] != null && grabbablesInFront[i].transform.gameObject.activeSelf && grabbablesInFront[i].IsGrabbable)
            {
                grabbable = grabbablesInFront[i];
                return true;
            }
        grabbable = null;
        return false;
    }
    public IGrabbable HasUsableGrabbable()
    {
        for (int i = 0; i < grabbablesInFront.Count; i++)
            if (grabbablesInFront[i] != null && grabbablesInFront[i].transform.gameObject.activeSelf && grabbablesInFront[i].IsGrabbable)
                return grabbablesInFront[i];
        return null;
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
        grabbablesInFront.Clear();

        UpdateInteractableList();
        UpdateGrabbables();
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
