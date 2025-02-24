
/*
Original Authors
    André Cardoso - Github
License
    This project is licensed under the MIT License - see the LICENSE.md file for details
*/


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using UnityEngine.Rendering;
using Cinemachine;

public class ConversationManager : MonoBehaviour
{
    public bool inDialogue; //Are we in dialouge?

    public static ConversationManager instance; //We only need 1 instance of the ConversationManager

    public CanvasGroup canvasGroup;
    public TMP_Animated animatedText;
    public Image namePlate; //Showing the name plate image of speaker
    public TextMeshProUGUI nameTMP; //Storing the text component of the speaker

    //The current speaker 
    [HideInInspector]
    public SpeakerScript currentSpeaker;

    private int dialogueIndex; //What box of text we are in
    public bool canExit; //Can the player exit the dialogue?
    public bool nextDialogue; //Are we advancing to the next dialogue?
    [Space]
    [Header("Cameras")]
    public GameObject gameCamera; //Current game camera
    public GameObject dialogueCamera; //Current dialogue camera
    [Space]

    public Volume dialogueDof;
    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        animatedText.onDialogueFinish.AddListener(() => FinishDialogue());
    }

    public void FinishDialogue()
    {
        if (dialogueIndex < currentSpeaker.dialogue.conversationBlock.Count - 1)
        {
            dialogueIndex++;
            nextDialogue = true;
        }
        else
        {
            nextDialogue = false;
            canExit = true;
        }
    }
}
