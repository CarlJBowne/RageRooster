using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityEngine.UIElements
{
    public static class UIHelpers
    {

        // Generic factory: create a VisualElement (or subtype) and apply style initialization
        public static T NewVisualElement<T>(System.Action<IStyle> styleInit = null) where T : VisualElement, new()
        {
            var el = new T();
            styleInit?.Invoke(el.style);
            return el;
        }

        public static void SetStyle(this VisualElement v, IStyle input)
        {
            v.style.flexDirection = input.flexDirection;
            v.style.alignItems = input.alignItems;
            v.style.paddingTop = input.paddingTop;
            v.style.paddingBottom = input.paddingBottom;
            v.style.paddingLeft = input.paddingLeft;
            v.style.paddingRight = input.paddingRight;
            v.style.marginTop = input.marginTop;
            v.style.marginBottom = input.marginBottom;
            v.style.marginLeft = input.marginLeft;
            v.style.marginRight = input.marginRight;
            v.style.width = input.width;
            v.style.height = input.height;
            v.style.flexGrow = input.flexGrow;
            v.style.unityTextAlign = input.unityTextAlign;
            v.style.display = input.display;
            v.style.bottom = input.bottom;
            v.style.left = input.left;
            v.style.position = input.position;
            v.style.right = input.right;
            v.style.top = input.top;
            v.style.cursor = input.cursor;
            v.style.width = input.width;
            v.style.height = input.height;
        }

        public static int LabelTextWidth(this Label label)
        {
            return 0;

            //string text = label.text;
            //IStyle style = label.style;
            //Length fontSize = style.fontSize.value;
            //FontStyle fontStyle = style.unityFontStyleAndWeight.value;
            //Font font = style.unityFont.value ?? Font.CreateDynamicFontFromOSFont("Arial", (int)fontSize);
            //FontStyle fs = FontStyle.Normal;
            //switch (fontStyle)
            //{
            //    case FontStyle.Bold: fs = FontStyle.Bold; break;
            //    case FontStyle.Italic: fs = FontStyle.Italic; break;
            //    case FontStyle.BoldAndItalic: fs = FontStyle.BoldAndItalic; break;
            //    default: fs = FontStyle.Normal; break;
            //}
            //font.RequestCharactersInTexture(text, (int)fontSize, fs);
            //int totalWidth = 0;
            //foreach (char c in text)
            //{
            //    if (font.GetCharacterInfo(c, out CharacterInfo charInfo, (int)fontSize, fs))
            //    {
            //        totalWidth += charInfo.advance;
            //    }
            //}
            //return totalWidth;
        }

















    }

}

