using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Cinemachine;
using UnityEngine.Assertions.Must;
using Unity.VisualScripting;

public class DialogueTrigger : MonoBehaviour
{
    private ConversationManager ui;
    private SpeakerScript currentSpeaker;
    //private MovementInput movement;
    public CinemachineTargetGroup targetGroup;
    public CinemachineVirtualCamera dialogueCamera;

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
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("NPC"))
        {
            currentSpeaker = other.GetComponent<SpeakerScript>();
            ui.currentSpeaker = currentSpeaker;
            targetGroup = currentSpeaker.targetGroup;
            currentSpeaker.onSpeakerActivate += StartConversation;
            

        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("NPC"))
        {            
            currentSpeaker.onSpeakerActivate -= StartConversation;
            currentSpeaker = null;
            ui.currentSpeaker = currentSpeaker;

        }
    }

    public void StartConversation()
    {
        Debug.Log("Conversation is starting");
        
        if (!ui.inDialogue && currentSpeaker != null)
        {
            
            targetGroup.m_Targets[1].target = Gameplay.I.player.transform;
            //movement.active = false;
            ui.dialogueCamera.GetComponent<CinemachineVirtualCamera>().Follow = targetGroup.transform;
            ui.dialogueCamera.GetComponent<CinemachineVirtualCamera>().LookAt = targetGroup.transform;
            ui.SetCharNameAndColor();
            ui.inDialogue = true;
            ui.CameraChange(true);
            ui.ClearText();
            ui.FadeUI(true, .2f, .65f);
            currentSpeaker.TurnToPlayer(Gameplay.I.player.transform.position);
        }
    }
}
