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
}

public class SuperList<T> : VisualElement
{
    // Plan / Pseudocode (detailed):
    // 1. Expose a delegate type `PropertyToVisualElementDelegate(SerializedProperty)` that produces
    //    a VisualElement for a given element SerializedProperty.
    // 2. Add a public property `DrawElementBody` of that delegate type on SuperList so callers can set it
    //    when constructing the SuperList.
    // 3. When creating Element instances (both during InitializeElements and AddSlot) invoke DrawElementBody
    //    with the element's SerializedProperty. If the delegate returns null, fall back to a default (a
    //    PropertyField for that SerializedProperty).
    // 4. Give each Element a reference to its parent SuperList and its index so the element's remove button
    //    can call back to the parent RemoveSlot via RemoveButtonPressed(index).
    // 5. Make AddSlot / RemoveSlot / ClearSlots update the underlying SerializedProperty properly:
    //    - call property.serializedObject.Update() before making changes,
    //    - modify property.arraySize or call DeleteArrayElementAtIndex,
    //    - call property.serializedObject.ApplyModifiedProperties() after changes,
    //    - keep the `elements` list and the visual `collectionBackground` in sync and update indices.
    // 6. Provide a helper UpdateElementIndices to refresh Element.selfIndex after removes/clears.
    // 7. Fix the bug in RemoveButtonPressed check (return when invalid index) so callbacks run properly.
    //
    // This approach allows parent code to pass any factory that turns a SerializedProperty into a
    // VisualElement (with binding) and have the SuperList instantiate and wire those element bodies
    // into its UI and lifecycle (add/remove/clear).

    public SuperList(SerializedProperty listProperty, PropertyToVisualElementDelegate drawElementBody = null)
    {
        property = listProperty;
        DrawElementBody = drawElementBody;

        CreateVisualElements();

        // Initialize elements list if property exists
        if (property != null)
        {
            UpdateCounter();
            InitializeArrayElements();
        }
    }

    private void CreateVisualElements()
    {
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
    }

    public void InitializeArrayElements()
    {
        elements = new List<Element>();
        if (property == null) return;
        property.serializedObject.Update();
        for (int i = 0; i < arraySize; i++)
        {
            SerializedProperty elementProperty = property.GetArrayElementAtIndex(i);
            Element element = new Element(elementProperty, i, this, DrawElementBody);
            elements.Add(element);
            collectionBackground.Add(element);
        }
        UpdateElementIndices();
        UpdateCounter();
    }

    #region Visual Pieces
    public Label label { get; private set; }
    public Button addButton { get; private set; }
    public Button foldoutArrow { get; private set; }
    public Label elementCounter { get; private set; }

    //Content Section
    public VisualElement collectionBackground { get; private set; }
    #endregion


    //Callbacks
    public PassListDelegate preAddCallback { get; set; }
    public RemoveElementDelegate preRemoveCallback { get; set; }
    public PassListDelegate preClearCallback { get; set; }

    //Data
    public SerializedProperty property { get; private set; }
    public List<Element> elements { get; private set; }

    public int arraySize
    {
        get => (property != null) ? property.arraySize : 0;
        set => property.arraySize = value;
    }
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


    // The delegate callers should set to produce the body VisualElement for a given element property.
    public PropertyToVisualElementDelegate DrawElementBody { get; set; }

    public void AddButtonPressed()
    {
        if (preAddCallback != null) preAddCallback(this);
        else Add_Base();
    }
    public void RemoveButtonPressed(int index)
    {
        // Return early if index invalid
        if (elements == null || index < 0 || index >= elements.Count) return;
        if (preRemoveCallback != null) preRemoveCallback(this, index);
        else Remove_Base(index);
    }
    public void ClearButtonPressed()
    {
        if (preClearCallback != null) preClearCallback(this);
        else Clear_Base();
    }



    public void Add_Base()
    {
        if (property == null) return;
        property.serializedObject.Update();
        property.arraySize++;
        property.serializedObject.ApplyModifiedProperties();

        // Create element visual for the new slot
        SerializedProperty newElemProp = property.GetArrayElementAtIndex(property.arraySize - 1);
        var element = new Element(newElemProp, elements != null ? elements.Count : 0, this, DrawElementBody);

        if (elements == null) elements = new List<Element>();
        elements.Add(element);
        collectionBackground.Add(element);

        UpdateElementIndices();
        UpdateCounter();
    }

    public void Remove_Base(int index)
    {
        if (property == null) return;
        if (elements == null || index < 0 || index >= elements.Count) return;

        property.serializedObject.Update();
        // Remove serialized property element (handles null object references)
        property.DeleteArrayElementAtIndex(index);
        property.serializedObject.ApplyModifiedProperties();

        // Remove element visuals and list entry
        var toRemove = elements[index];
        collectionBackground.Remove(toRemove);
        elements.RemoveAt(index);

        UpdateElementIndices();
        UpdateCounter();
    }

    public void Clear_Base()
    {
        if (property != null)
        {
            property.serializedObject.Update();
            property.arraySize = 0;
            property.serializedObject.ApplyModifiedProperties();
        }

        if (elements != null)
        {
            foreach (var el in elements)
            {
                collectionBackground.Remove(el);
            }
            elements.Clear();
        }
        UpdateCounter();
    }

    // After removes/adds keep visual element indices correct for callbacks
    private void UpdateElementIndices()
    {
        if (elements == null) return;
        for (int i = 0; i < elements.Count; i++) elements[i].selfIndex = i;
    }

    public void UpdateCounter()
    {
        if (elementCounter != null)
            elementCounter.text = (property != null) ? property.arraySize.ToString() : "0";
        expandable = arraySize > 0;
    }

    public delegate VisualElement PropertyToVisualElementDelegate(SerializedProperty elementProperty);
    public delegate void PassListDelegate(SuperList<T> list);
    public delegate void RemoveElementDelegate(SuperList<T> list, int index);

    public class Element : VisualElement
    {
        public Element(SerializedProperty elementProperty, int index, SuperList<T> parent, PropertyToVisualElementDelegate drawBody)
        {
            property = elementProperty;
            selfIndex = index;
            parentList = parent;

            // Minimal placeholder visual for an element
            background = new VisualElement();
            background.style.height = 18;
            background.style.marginBottom = 2;
            background.style.backgroundColor = new StyleColor(new Color(1f, 1f, 1f));
            content = new VisualElement();
            content.style.flexDirection = FlexDirection.Row;
            background.Add(content);

            removeButton = new Button(() =>
            {
                // call back to parent to remove this index
                parentList?.RemoveButtonPressed(selfIndex);
            })
            {
                text = "x"
            };
            removeButton.style.width = 18;
            removeButton.style.height = 14;
            removeButton.style.marginLeft = 4;
            content.Add(removeButton);

            // Use the provided drawBody delegate to build the body. Fallback to a PropertyField if null.
#if UNITY_EDITOR
            VisualElement body = null;
            try
            {
                if (drawBody != null)
                    body = drawBody(property);
            }
            catch (Exception)
            {
                body = null;
            }

            if (body == null)
            {
                // fallback: a simple PropertyField bound to the property
                var pf = new UnityEditor.UIElements.PropertyField(property);
                // ensure a minimum height so layout is stable
                pf.style.minHeight = 16;
                pf.style.height = 16;
                body = pf;
            }
            content.Add(body);
#endif
            this.Add(background);
        }

        public SerializedProperty property { get; private set; }
        public VisualElement background { get; private set; }
        public VisualElement content { get; private set; }
        public Button removeButton { get; private set; }
        public VisualElement dragHandle { get; private set; }
        public int selfIndex { get; internal set; }

        // parent reference for callbacks
        private SuperList<T> parentList;
    }

}