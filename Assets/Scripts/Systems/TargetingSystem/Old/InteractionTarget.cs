using System.Collections;
using System.Collections.Generic;
using UltEvents;
using UnityEngine;

public class InteractionTarget : TargetOldBase
{
    public UltEvent OnInteract;
    public Vector3 PopupPosition;

    public override void OnDeTargeted(TargetOldBase nextTarget)
    {
        //if(!nextTarget) TargetingManager.InteractionPopup.SetActive(false);
    }
    public override void OnTargeted(TargetOldBase prevTarget)
    {
        //TargetingManager.InteractionPopup.SetActive(true);
        //TargetingManager.InteractionPopup.transform.position = transform.position + PopupPosition;
    }
}
