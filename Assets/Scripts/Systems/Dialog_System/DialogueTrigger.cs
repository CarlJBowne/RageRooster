using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Cinemachine;

public class DialogueTrigger : MonoBehaviour
{
    private ConversationManager ui;
    private SpeakerScript currentSpeaker;
    //private MovementInput movement;
    public CinemachineTargetGroup targetGroup;

    [Space]

    [Header("Post Processing")]
    public Volume dialogueDof;

    void Start()
    {
        ui = ConversationManager.instance;
        //Get the source of player movement and disable that
        //movement = GetComponent<MovementInput>();
    }

    void Update()
    {
        //Move everything in the Update into a different section to trigger the iteraction of talking
        if (!ui.inDialogue && currentSpeaker != null)
        {
            targetGroup.m_Targets[1].target = currentSpeaker.transform;
            //movement.active = false;
            ui.SetCharNameAndColor();
            ui.inDialogue = true;
            ui.CameraChange(true);
            ui.ClearText();
            ui.FadeUI(true, .2f, .65f);
            currentSpeaker.TurnToPlayer(transform.position);
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Villager"))
        {
            currentSpeaker = other.GetComponent<SpeakerScript>();
            ui.currentSpeaker = currentSpeaker;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Villager"))
        {
            currentSpeaker = null;
            ui.currentSpeaker = currentSpeaker;
        }
    }
}
