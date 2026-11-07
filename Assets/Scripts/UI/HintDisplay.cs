using System;
using System.Collections.Generic;
using System.Text;
using PlasticGui;
using TMPro;
using UnityEngine;

namespace RageRooster.UI
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class HintDisplay : MonoBehaviour
    {
        public TextMeshProUGUI text;

        public float hintTime;

        private float hintTimer;

        public void ShowHint(string hintString)
        {
            text.text = HintTextParser(hintString);
            transform.parent.gameObject.SetActive(true);
            hintTimer = hintTime;
        }

        public string HintTextParser(string input)
        {
            string result = string.Empty;
            //Split the whole text into parts based on the <> tags
            //Even numbers in the array are text, odd numbers are tags
            string[] subTexts = input.Split('<', '>');

            // textmeshpro still needs to parse its built-in tags, so we only include noncustom tags
            for (int i = 0; i < subTexts.Length; i++)
            {
                if (i % 2 == 0)
                    result += subTexts[i];
                else //Is Tag
                {
                    string tag = subTexts[i].Replace(" ", "");
                    if (tag.StartsWith("control="))
                        result += RemappingMenu.GetControlString(tag.Substring(8));
                    else result += $"<{tag}>";
                }
            }

            return result;
        }

        bool isCustomTag(string tag)
        {
            return tag.StartsWith("speed=") || tag.StartsWith("pause=") || tag.StartsWith("emotion=") || tag.StartsWith("action=");
        }

        private void Update()
        {
            hintTimer -= Time.deltaTime;
            if (hintTimer <= 0) transform.parent.gameObject.SetActive(false);
        }
    }
}
