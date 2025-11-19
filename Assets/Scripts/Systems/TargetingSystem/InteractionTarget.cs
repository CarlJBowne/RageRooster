using System.Collections;
using System.Collections.Generic;
using UltEvents;
using UnityEngine;

public class InteractionTarget : Target
{
    public UltEvent OnInteract;
    public Vector3 PopupPosition;

    public override TargetStates TargetState 
    { 
        get => base.TargetState; 
        set
        {
            if(currentState == TargetStates.Targeted && value != TargetStates.Targeted)
            {
                TargetingManager.InteractionPopup.SetActive(false);
            }
            else if (currentState != TargetStates.Targeted && value == TargetStates.Targeted)
            {
                TargetingManager.InteractionPopup.SetActive(true);
                TargetingManager.InteractionPopup.transform.position = transform.position + PopupPosition;
            }
            base.TargetState = value;
        }
    }
}
