using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
namespace TMPro
{
        // Jacob Dreyer
        // Creating a custom text element that can be animated and has a controlled text speed

        /*
        Original Authors
            André Cardoso - Github
        License
            This project is licensed under the MIT License - see the LICENSE.md file for details
        */

    //Creating actions to interface with code and trigger what we want to happen during or after dialouge
    [System.Serializable] public class ActionEvent : UnityEvent<string> {}
    [System.Serializable] public class TextRevealEvent : UnityEvent<char> {}
    [System.Serializable] public class DialogueEvent : UnityEvent {}
    public class TMP_Animated : TextMeshProUGUI
    {


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

            
            // textmeshpro still needs to parse its built-in tags, so we only include noncustom tags
            string displayText = "";
            for (int i = 0; i < subTexts.Length; i++)
            {
                if (i % 2 == 0)
                    displayText += subTexts[i];
                else if (!isCustomTag(subTexts[i].Replace(" ", "")))
                    displayText += $"<{subTexts[i]}>";
            }

            // send that string to textmeshpro and hide all of it, then start reading
            text = displayText;
            maxVisibleCharacters = 0;
            StartCoroutine(Read());

            IEnumerator Read()
            {
                int subCounter = 0;
                int visibleCounter = 0;
                while (subCounter < subTexts.Length)
                {
                    // if 
                    if (subCounter % 2 == 1)
                    {
                        yield return EvaluateTag(subTexts[subCounter].Replace(" ", ""));
                    }
                    else
                    {
                        while (visibleCounter < subTexts[subCounter].Length)
                        {
                            onTextReveal.Invoke(subTexts[subCounter][visibleCounter]);
                            visibleCounter++;
                            maxVisibleCharacters++;
                            yield return new WaitForSeconds(1f / textSpeed);
                        }
                        visibleCounter = 0;
                    }
                    subCounter++;
                }
                yield return null;

                WaitForSeconds EvaluateTag(string tag)
                {
                    if (tag.Length > 0)
                    {
                        if (tag.StartsWith("speed="))
                        {
                            textSpeed = float.Parse(tag.Split('=')[1]);
                        }
                        else if (tag.StartsWith("pause="))
                        {
                            return new WaitForSeconds(float.Parse(tag.Split('=')[1]));
                        }
                        else if (tag.StartsWith("action="))
                        {
                            onAction.Invoke(tag.Split('=')[1]);
                        }
                    }
                    return null;
                }
                onDialogueFinish.Invoke();
            }

        }

        

        // check to see if a tag is our own
        bool isCustomTag(string tag)
        {
            return tag.StartsWith("speed=") || tag.StartsWith("pause=") || tag.StartsWith("emotion=") || tag.StartsWith("action=");
        }

    }
}

