
/*
Original Authors
    André Cardoso - Github
License
    This project is licensed under the MIT License - see the LICENSE.md file for details
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using DG.Tweening;
using Cinemachine;
using System;
using static Input;
[RequireComponent(typeof(DialogueAudio))]
[RequireComponent(typeof(Animator))]
public class SpeakerScript : MonoBehaviour, IInteractable
{
    public NPC_Data data;
    public DialogueData dialogue;

    public bool villagerIsTalking;

    private TMP_Animated animatedText;
    private DialogueAudio dialogueAudio;
    private Animator animator;

    public Transform particlesParent;

    bool IInteractable.canInteract => true;

    public event Action onSpeakerActivate;
    public CinemachineTargetGroup targetGroup;

    public int currentConversationIndex = 0;


    public UltEvents.UltEvent onConversationEnd;

    public UltEvents.UltEvent<string> onActionTrigger;

    void Start()
    {
        dialogueAudio = GetComponent<DialogueAudio>();
        animator = GetComponent<Animator>();
        animatedText = ConversationManager.instance.animatedText;
        animatedText.onAction.AddListener((action) => SetAction(action));
    }
    public void EmotionChanger(Emotion e)
    {
        if (this != ConversationManager.instance.currentSpeaker)
            return;

        //Trigger animation based on current emotion
        animator.SetTrigger(e.ToString());
    }

    //This function controls the "action" of the speaker to determine what effect to apply or sound to play
    public void SetAction(string action)
    {
        if (this != ConversationManager.instance.currentSpeaker)
            return;

        onActionTrigger?.Invoke(action);
        
    }

    public void PlayParticle(string x)
    {
        if (particlesParent.Find(x + "Particle") == null)
            return;
        particlesParent.Find(x + "Particle").GetComponent<ParticleSystem>().Play();
    }

    public void Reset()
    {
        animator.SetTrigger("normal");
    }

    public void TurnToPlayer(Vector3 playerPos)
    {
        playerPos = Gameplay.I.player.transform.position;
        transform.DOLookAt(playerPos, Vector3.Distance(transform.position, playerPos) / 5);
        string turnMotion = isRightSide(transform.forward, playerPos, Vector3.up) ? "rturn" : "lturn";
        animator.SetTrigger(turnMotion);
    }

    //https://forum.unity.com/threads/left-right-test-function.31420/
    public bool isRightSide(Vector3 fwd, Vector3 targetDir, Vector3 up)
    {
        Vector3 right = Vector3.Cross(up.normalized, fwd.normalized);        // right vector
        float dir = Vector3.Dot(right, targetDir.normalized);
        return dir > 0f;
    }

    bool IInteractable.Interaction()
    {
        //Debug.Log("Speaker is activated");

        ConversationManager UI = ConversationManager.instance;

        if (!UI.inDialogue)
        {
            UI.currentSpeaker = this;

            targetGroup.m_Targets[1].target = Gameplay.I.player.transform;
            Gameplay.PlayerStateMachine.PauseState();
            //UI.dialogueCamera.GetComponent<CinemachineVirtualCamera>().Follow = targetGroup.transform;
            UI.dialogueCamera.GetComponent<CinemachineVirtualCamera>().LookAt = targetGroup.transform;
            UI.SetCharNameAndColor();
            UI.inDialogue = true;
            PauseMenu.canPause = false;
            UI.CameraChange(true);
            UI.ClearText();
            UI.FadeUI(true, .2f, .65f);
            TurnToPlayer(Gameplay.Player.transform.position);
            animator.SetTrigger("talking");
        }

        dialogue = data.dialogueList[data.dialogueID];
        onSpeakerActivate?.Invoke();

        return true;
    }

    public virtual void CheckForNextConversation(int index) => data.OnConversationFinished();


    public Vector3 PopupPosition => transform.position + Vector3.up * 3;
}
