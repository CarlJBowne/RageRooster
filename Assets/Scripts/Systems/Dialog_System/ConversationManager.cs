
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
using UnityEngine.InputSystem;
using System;

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
    bool isAdvancingText;
    private void Awake()
    {
        instance = this;
    }
    private void OnEnable()
    {
        Input.Parry.performed += TryAdvanceText;
    }
    private void OnDisable()
    {
        Input.Parry.performed -= TryAdvanceText;
    }

    private void TryAdvanceText(InputAction.CallbackContext context)
    {
        isAdvancingText = true;
        Debug.Log("Trying to advance text");
    }

    private void Start()
    {
        animatedText.onDialogueFinish.AddListener(() => FinishDialogue());
    }
    private void Update()
    {
        
        if (isAdvancingText && inDialogue)
        {
            if (canExit)
            {
                CameraChange(false);
                FadeUI(false, .2f, 0);
                Sequence s = DOTween.Sequence();
                s.AppendInterval(.8f);
                s.AppendCallback(() => ResetState());
            }

            if (nextDialogue)
            {
                animatedText.ReadText(currentSpeaker.dialogue.conversationBlock[dialogueIndex]);
            }
        }
        isAdvancingText = false;
        
    }

    public void FadeUI(bool show, float time, float delay)
    {
        Sequence s = DOTween.Sequence();
        s.AppendInterval(delay);
        s.Append(canvasGroup.DOFade(show ? 1 : 0, time));
        if (show)
        {
            dialogueIndex = 0;
            s.Join(canvasGroup.transform.DOScale(0, time * 2).From().SetEase(Ease.OutBack));
            s.AppendCallback(() => animatedText.ReadText(currentSpeaker.dialogue.conversationBlock[0]));
        }
    }

    public void SetCharNameAndColor()
    {
        nameTMP.text = currentSpeaker.data.npcName;

    }

    public void CameraChange(bool dialogue)
    {
        gameCamera.SetActive(!dialogue);
        dialogueCamera.SetActive(dialogue);

        //Depth of field modifier
        float dofWeight = dialogueCamera.activeSelf ? 1 : 0;
//        DOVirtual.Float(dialogueDof.weight, dofWeight, .8f, DialogueDOF);
    }

    public void DialogueDOF(float x)
    {
        dialogueDof.weight = x;
    }

    public void ClearText()
    {
        animatedText.text = string.Empty;
    }

    public void ResetState()
    {
        currentSpeaker.Reset();
        //Interface to stop player movement while in dialogue by disabling the component for movement
        //FindObjectOfType<MovementInput>().active = true;
        inDialogue = false;
        canExit = false;
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
