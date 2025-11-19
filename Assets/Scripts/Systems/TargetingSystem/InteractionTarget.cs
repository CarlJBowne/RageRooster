using System.Collections;
using System.Collections.Generic;
using UltEvents;
using UnityEngine;

public class InteractionTarget : Target
{
    public UltEvent OnInteract;
    public Vector3 PopupPosition;

    public override void OnDeTargeted(Target nextTarget)
    {
        if(!nextTarget) TargetingManager.InteractionPopup.SetActive(false);
    }
    public override void OnTargeted(Target prevTarget)
    {
        TargetingManager.InteractionPopup.SetActive(true);
        TargetingManager.InteractionPopup.transform.position = transform.position + PopupPosition;
    }
}
