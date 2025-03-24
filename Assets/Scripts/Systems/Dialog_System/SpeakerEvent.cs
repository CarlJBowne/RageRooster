using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeakerEvent : MonoBehaviour
{
    public string eventName = String.Empty;

    public UltEvents.UltEvent onSpeakerEvent;

    SpeakerScript speaker;

    public void Start()
    {
        speaker = GetComponent<SpeakerScript>();
        if(speaker != null)
        {
            speaker.onActionTrigger += CheckSpeakerEvent;
        }

    }
    void OnDisable()
    {
        speaker = GetComponent<SpeakerScript>();
        if(speaker != null)
        {
            speaker.onActionTrigger += CheckSpeakerEvent;
        }
    }

    

    public void CheckSpeakerEvent(string action)
    {
        if(action == eventName)
        {
            OnSpeakerEvent();
        }
    }

    public virtual void OnSpeakerEvent()
    {

        //Always trigger the speaker event
        onSpeakerEvent?.Invoke();
    }

}
