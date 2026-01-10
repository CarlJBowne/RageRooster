using EditorAttributes;
using FMOD.Studio;
using RageRooster.RoomSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using RageRooster.Systems.ObjectPool;
using FMODUnity;
using RageRooster.Systems;
using UnityEngine.UIElements;
using UnityEditor;

public class TestScript : MonoBehaviour
{
    List<int> testInts = new();

#if UNITY_EDITOR
    public class _Editor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            return new SuperList<int>(serializedObject.FindProperty("testInts"))
            {

            };
        }
    }
#endif

    public class SuperList<T> : VisualElement
    {
        public SuperList(SerializedProperty listProperty)
        {
            property = listProperty;

            // Header bar (dark gray, ~one line high)
            var headerBar = new VisualElement();
            headerBar.name = "superlist-headerbar";
            headerBar.style.flexDirection = FlexDirection.Row;
            headerBar.style.alignItems = Align.Center;
            headerBar.style.height = 18; // approximate single line height
            headerBar.style.backgroundColor = new StyleColor(new Color(0.18f, 0.18f, 0.18f)); // dark gray
            headerBar.style.paddingLeft = 4;
            headerBar.style.paddingRight = 4;
            headerBar.style.marginBottom = 2;

            // Foldout arrow button
            foldoutArrow = new Button(() =>
            {
                expanded = !expanded;
            })
            {
                text = "▶"
            };
            foldoutArrow.name = "superlist-foldout";
            foldoutArrow.style.width = 18;
            foldoutArrow.style.height = 16;
            foldoutArrow.style.unityTextAlign = TextAnchor.MiddleCenter;
            foldoutArrow.style.marginRight = 6;
            headerBar.Add(foldoutArrow);

            // Main label (flexible)
            label = new Label("Super List");
            label.name = "superlist-label";
            label.style.flexGrow = 1;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.color = new StyleColor(Color.white);
            label.style.fontSize = 11;
            headerBar.Add(label);

            // Element counter (small, right-justified)
            elementCounter = new Label((property != null) ? property.arraySize.ToString() : "0");
            elementCounter.name = "superlist-counter";
            elementCounter.style.width = 36;
            elementCounter.style.unityTextAlign = TextAnchor.MiddleRight;
            elementCounter.style.color = new StyleColor(new Color(0.85f, 0.85f, 0.85f));
            elementCounter.style.marginRight = 6;
            headerBar.Add(elementCounter);

            // Large plus button
            addButton = new Button(() =>
            {
                AddButtonPressed();
            })
            {
                text = "+"
            };
            addButton.name = "superlist-add";
            addButton.style.width = 24;
            addButton.style.height = 20;
            addButton.style.unityTextAlign = TextAnchor.MiddleCenter;
            addButton.style.fontSize = 14;
            addButton.style.backgroundColor = new StyleColor(new Color(0.25f, 0.6f, 0.25f)); // greenish for visibility
            addButton.style.color = new StyleColor(Color.white);
            headerBar.Add(addButton);

            // Collection background (lighter gray box, variable size depending on content)
            collectionBackground = new VisualElement();
            collectionBackground.name = "superlist-collection";
            collectionBackground.style.flexDirection = FlexDirection.Column;
            collectionBackground.style.backgroundColor = new StyleColor(new Color(0.92f, 0.92f, 0.92f)); // light gray
            collectionBackground.style.paddingLeft = 4;
            collectionBackground.style.paddingRight = 4;
            collectionBackground.style.paddingTop = 4;
            collectionBackground.style.paddingBottom = 4;
            collectionBackground.style.marginTop = 0;
            collectionBackground.style.minHeight = 0;
            collectionBackground.visible = false; // collapsed by default

            // Assemble root
            this.Add(headerBar);
            this.Add(collectionBackground);

            // Initialize elements list if property exists
            if (property != null)
            {
                UpdateCounter();
                InitializeElements();
            }
        }

        // Header
        public Label label
        { get; private set; }
        public Button addButton { get; private set; }
        public Button foldoutArrow { get; private set; }
        public Label elementCounter { get; private set; }

        //Content Section
        public VisualElement collectionBackground { get; private set; }
        public List<Element> elements { get; private set; }

        //Callbacks
        public PassListDelegate preAddCallback { get; set; }
        public RemoveElementDelegate preRemoveCallback { get; set; }
        public PassListDelegate preClearCallback { get; set; }

        //Data
        public SerializedProperty property { get; private set; }
        public int elementCount => property != null ? property.arraySize : 0;
        public bool expanded
        {
            get => _expanded;
            set
            {
                _expanded = value;
                if (collectionBackground != null) collectionBackground.visible = value;
                if (foldoutArrow != null) foldoutArrow.text = value ? "▼" : "▶";
            }
        }
        bool _expanded = false;

        public bool expandable
        {
            set
            {
                if (foldoutArrow != null) foldoutArrow.visible = value;
                expanded = false;
            }
        }

        public void InitializeElements()
        {
            elements = new List<Element>();
            if (property == null) return;
            for (int i = 0; i < property.arraySize; i++)
            {
                SerializedProperty elementProperty = property.GetArrayElementAtIndex(i);
                Element element = new(elementProperty);
                elements.Add(element);
                collectionBackground.Add(element);
            }
        }

        public Func<VisualElement, SerializedProperty> DrawElementBody;

        public void AddButtonPressed()
        {
            if (preAddCallback != null) preAddCallback(this);
            else AddSlot();
        }
        public void RemoveButtonPressed(int index)
        {
            if (elements != null && index >= 0 && index < elements.Count) return;
            if (preRemoveCallback != null) preRemoveCallback(this, index);
            else RemoveSlot(index);
        }
        public void ClearButtonPressed()
        {
            if (preClearCallback != null) preClearCallback(this);
            else ClearSlots();
        }



        public void AddSlot()
        {
            property.arraySize++;
            elements.Add(new Element(property.GetArrayElementAtIndex(property.arraySize - 1)));
            UpdateCounter();
        }
        public void RemoveSlot(int index)
        {
            UpdateCounter();
        }
        public void ClearSlots()
        {
            elements.Clear();
            UpdateCounter();
        }















        public void UpdateCounter()
        {
            if (elementCounter != null)
                elementCounter.text = (property != null) ? property.arraySize.ToString() : "0";
            expandable = elementCount > 0;
        }

        public class Element : VisualElement
        {
            public Element(SerializedProperty elementProperty)
            {
                property = elementProperty;
                // Minimal placeholder visual for an element
                background = new VisualElement();
                background.style.height = 18;
                background.style.marginBottom = 2;
                background.style.backgroundColor = new StyleColor(new Color(1f, 1f, 1f));
                content = new VisualElement();
                content.style.flexDirection = FlexDirection.Row;
                background.Add(content);

                removeButton = new Button(() => { /* no-op for placeholder */ })
                {
                    text = "x"
                };
                removeButton.style.width = 18;
                removeButton.style.height = 14;
                removeButton.style.marginLeft = 4;
                content.Add(removeButton);

                this.Add(background);
            }
            public SerializedProperty property { get; private set; }
            public VisualElement background { get; private set; }
            public VisualElement content { get; private set; }
            public Button removeButton { get; private set; }
            public VisualElement dragHandle { get; private set; }
            public int selfIndex { get; private set; }
        }

        public delegate VisualElement PropertyToVisualElementDelegate(SerializedProperty elementProperty);
        public delegate void PassListDelegate(SuperList<T> list);
        public delegate void RemoveElementDelegate(SuperList<T> list, int index);
    }

}