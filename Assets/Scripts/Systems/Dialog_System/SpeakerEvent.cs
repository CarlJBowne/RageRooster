using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeakerEvent : MonoBehaviour
{
    public string eventName = String.Empty;

    public UltEvents.UltEvent onSpeakerEvent;


    public void OnSpeakerEvent()
    {
        Debug.Log("Event has fired!");
    }

}
