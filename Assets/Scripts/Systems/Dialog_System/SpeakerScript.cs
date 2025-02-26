
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

        if (action == "shake")
        {
            //TODO: Change this to not grab the Camera main and create our own static reference to the main camera to be more performant
            //Camera.main.GetComponent<CinemachineImpulseSource>().GenerateImpulse();
        }
        else
        {
            //These will be replaced with our own custom actions that the speakers can do when the tag is parsed for them
            //PlayParticle(action);

            if (action == "sparkle")
            {
                dialogueAudio.effectSource.clip = dialogueAudio.sparkleClip;
                dialogueAudio.effectSource.Play();
            }
            else if (action == "rain")
            {
                dialogueAudio.effectSource.clip = dialogueAudio.rainClip;
                dialogueAudio.effectSource.Play();
            }
        }
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
        Debug.Log("Speaker is activated");
        onSpeakerActivate?.Invoke();
        dialogue = data.dialogueList[0];
        return true;
    }
}
