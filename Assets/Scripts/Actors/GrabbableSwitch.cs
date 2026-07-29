using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class GrabbableSwitch : MonoBehaviour
{
    [SerializeField] UltEvents.UltEvent onGrabbed;

    public void Invoke() => onGrabbed?.Invoke();
}