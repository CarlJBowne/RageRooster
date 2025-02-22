using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Events;
namespace TMPro
{

    //Creating actions to interface with code and trigger what we want to happen during or after dialouge
    [System.Serializable] public class ActionEvent : UnityEvent<string> {}
    [System.Serializable] public class TextRevealEvent : UnityEvent<char> {}
    [System.Serializable] public class DialogueEvent : UnityEvent {}
    public class TMP_Animated : TextMeshProUGUI
    {
        // Jacob Dreyer
        // Creating a custom text element that can be animated and has a controlled text speed

        [SerializeField] private float textSpeed = 10; //TODO:  Replace with a reference to a global setting for players to control default speed
        public ActionEvent onAction;
        public TextRevealEvent onTextReveal;
        public DialogueEvent onDialogueFinish;

        public void ReadText(string newText)
        {
            text = string.Empty;
            //Split the whole text into parts based on the <> tags
            //Even numbers in the array are text, odd numbers are tags
            string[] subTexts = newText.Split('<', '>');

            
        }

    }
}

