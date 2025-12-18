using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEditor;
using UnityEditor.UIElements;
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

        public static void SetAllMargins(this VisualElement v, float value)
        {
            v.style.marginTop = value;
            v.style.marginBottom = value;
            v.style.marginLeft = value;
            v.style.marginRight = value;
        }

        public static VisualElement GetChild(this VisualElement V, int i) 
            => V.hierarchy.childCount > i ? V.hierarchy.ElementAt(i) : null;

        public static VisualElement GetDescendent(this VisualElement V, params int[] path)
        {
            VisualElement.Hierarchy H = V.hierarchy;
            VisualElement R = null;
            for (int i = 0; i < path.Length; i++)
            {
                if (H.childCount <= path[i]) return null;
                R = H.ElementAt(path[i]);
                H = R.hierarchy;
            }
            return R;
        }

        public static void Iterate(this SerializedProperty property, Action<SerializedProperty> action)
        {
            SerializedProperty iterator = property.Copy();
            SerializedProperty end = iterator.GetEndProperty(); // one past the last child

            if (!iterator.NextVisible(true)) return;

            while (!SerializedProperty.EqualContents(iterator, end))
            {
                action(iterator);
                if (!iterator.NextVisible(false)) break;
            }
        }

        public static void IterateAndDraw(this SerializedProperty property, VisualElement container)
        {
            // Iterate visible children of the property and add a PropertyField for each.
            SerializedProperty iterator = property.Copy();
            SerializedProperty end = iterator.GetEndProperty(); // one past the last child
            // Move into the first visible child
            if (!iterator.NextVisible(true))
                return;

            while (!SerializedProperty.EqualContents(iterator, end))
            {
                // Make a copy for the PropertyField since iterator will advance
                var childProp = iterator.Copy();
                var field = new PropertyField(childProp);
                field.Bind(property.serializedObject);
                container.Add(field);

                // Advance to next visible sibling/child
                if (!iterator.NextVisible(false))
                    break;
            }

        }







    }
    public class FoldoutPlus : Foldout
    {
        public FoldoutPlus()
        {
            header = this.GetChild(0) as Toggle;

            headerSide = new VisualElement();
            header.Add(headerSide);

            header.style.overflow = Overflow.Visible;

            headerSide.style.flexDirection = FlexDirection.Column;
            headerSide.style.position = Position.Absolute;
            headerSide.style.left = EditorGUIUtility.labelWidth;
            headerSide.style.right = 0;
            headerSide.style.maxHeight = EditorGUIUtility.singleLineHeight;
            this.contentContainer.style.marginTop = 0;



            this.RegisterCallback<AttachToPanelEvent>(EstablishElements);

            void EstablishElements(AttachToPanelEvent evt)
            {
                arrowButton = header.GetDescendent(0, 0);
                label = header.GetDescendent(0, 1) as Label;

                this.UnregisterCallback<AttachToPanelEvent>(EstablishElements);
            }
            
            //label.RegisterCallback<GeometryChangedEvent>(evt =>
            //{
            //    var rect = label.layout; // layout is in UIElements coordinates
            //                              // Left = label's x + its width (+ small gap if you want)
            //    headerSide.style.left = rect.x + rect.width + 2;
            //    // Right = keep zero so the header side fills to the right edge of the toggle
            //    headerSide.style.right = 0;
            //});
        }
        public Toggle header { get; private set; }
        public VisualElement arrowButton { get; private set; } 
        public Label label { get; private set; }
        public VisualElement headerSide { get; private set; }


        public void RecursiveDisplay(VisualElement V, int level = 0)
        {
            Debug.Log($"Element: {V}, level {level}, children {V.hierarchy.childCount}");
            for (int i = 0; i < V.hierarchy.childCount; i++)
                RecursiveDisplay(V.hierarchy.ElementAt(i), level + 1);
        }
    }
}

