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
    [SerializeField] List<int> testInts = new();

#if UNITY_EDITOR
    [CustomEditor(typeof(TestScript))]
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

#if UNITY_EDITOR
// Plan / Pseudocode (detailed):
// 1. Wrap the entire SuperList<T> with #if UNITY_EDITOR so editor-only types (SerializedProperty, PropertyField, etc.)
//    do not cause player build compile errors.
// 2. Provide a factory delegate type `PropertyToVisualElementDelegate(SerializedProperty)` that creates a
//    VisualElement for an element slot. Allow callers to provide this delegate via the constructor.
// 3. Build a compact header bar UI with:
//      - foldout arrow button (toggles expansion)
//      - label showing the list property display name (or a default)
//      - counter label showing element count
//      - add button to append a new element
//    Ensure styles and sizing are stable for Inspector layout.
// 4. Provide a collection container (collectionBackground) which holds row Elements and can be collapsed.
// 5. When constructing the SuperList:
//      - store the SerializedProperty
//      - create UI pieces
//      - if the property exists, Update the serialized object and create Element children for each array entry
// 6. Element creation:
//      - each Element stores its SerializedProperty and its parent SuperList reference
//      - Element contains a remove button wired to parent.RemoveButtonPressed(index)
//      - Element builds its body from DrawElementBody delegate; if null or if delegate throws, fallback to
//        creating a `UnityEditor.UIElements.PropertyField(property)` for safe binding.
// 7. Add / Remove / Clear operations:
//      - operate on the underlying SerializedProperty using the recommended SerializedObject pattern:
//          * property.serializedObject.Update()
//          * modify arraySize, InsertArrayElementAtIndex/DeleteArrayElementAtIndex as appropriate
//          * property.serializedObject.ApplyModifiedProperties()
//      - keep the `elements` list and the UI `collectionBackground` in sync
//      - after mutating, call UpdateElementIndices and UpdateCounter to keep UI consistent
// 8. Removal robustness:
//      - Unity sometimes leaves a null placeholder when deleting object-reference array slots.
//        After calling DeleteArrayElementAtIndex once, check the slot at the same index and if it
//        is a null ObjectReference, call DeleteArrayElementAtIndex again to fully remove the slot.
// 9. Expose preAddCallback / preRemoveCallback / preClearCallback for callers to override default behavior.
// 10. Keep layout measurements consistent (set fixed/min heights) so List virtualization and inspector layout
//     are stable.
// 11. Keep code defensive: guard against null `property` and null `elements` lists, ensure indices are validated.
//
// The following implementation applies these rules and fixes two notable missing pieces:
// - Guards the editor-only class with #if UNITY_EDITOR to avoid player compile errors.
// - Implements robust Delete (double-delete for null object references).
// - Sets the header label to the property display name if available.
public class SuperList<T> : VisualElement
{
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
    public VisualElement headerBar { get; private set; }
    public Label label { get; private set; }
    public Button addButton { get; private set; }
    public FoldoutArrow foldoutArrow { get; private set; }
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
        set { if (property != null) property.arraySize = value; }
    }
    public bool expanded
    {
        get => _expanded;
        set
        {
            _expanded = value;
            if (collectionBackground != null) collectionBackground.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
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
        // Add a new array slot. Using arraySize++ is sufficient in many cases.
        // If special initialization is needed caller can use preAddCallback.
        int newIndex = property.arraySize;
        property.arraySize++;
        property.serializedObject.ApplyModifiedProperties();

        // Create element visual for the new slot
        SerializedProperty newElemProp = property.GetArrayElementAtIndex(newIndex);
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

        // Delete once. For object reference slots Unity may leave a null placeholder and require a second delete call.
        property.DeleteArrayElementAtIndex(index);

        // If the array still has an element at this index and it's an object reference that is null,
        // delete it again to fully remove the slot.
        if (index < property.arraySize)
        {
            var maybeElem = property.GetArrayElementAtIndex(index);
            if (maybeElem != null && maybeElem.propertyType == SerializedPropertyType.ObjectReference && maybeElem.objectReferenceValue == null)
            {
                property.DeleteArrayElementAtIndex(index);
            }
        }

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




    private void CreateVisualElements()
    {
        //HeaderBar()
        {
            var headerBar = new VisualElement();
            headerBar.name = "superlist-headerbar";
            headerBar.style.flexDirection = FlexDirection.Row;
            headerBar.style.alignItems = Align.Center;
            headerBar.style.height = 20;

            headerBar.style.Colors(null, .2078432f.Gray(), .1411765f.Gray()).BorderWidth(1).Radius(0, top: 6);

            //FoldoutArrow()
            {
                foldoutArrow = new FoldoutArrow((value) => { expanded = value; })
                {
                    name = "superlist-foldout"
                };
                headerBar.Add(foldoutArrow);
            }

            //Label()
            {
                label = new Label(property != null ? property.displayName : "Super List");
                label.name = "superlist-label";
                label.style.flexGrow = 1;
                label.style.Text(12, TextAnchor.MiddleLeft);
                label.style.color = new StyleColor(.82f.Gray());
                label.RegisterCallback<ClickEvent>((evt) =>
                {
                    foldoutArrow.SetExpanded(!expanded);
                });
                label.focusable = true;
                //label.RegisterCallback < UnityEngine.UIElements.Focus >
                headerBar.Add(label);
            }

            //ElementCounter()
            {
                elementCounter = new Label((property != null) ? property.arraySize.ToString() : "0");

                elementCounter.name = "superlist-counter";
                elementCounter.style.width = 36;
                elementCounter.style.unityTextAlign = TextAnchor.MiddleRight;
                elementCounter.style.color = new StyleColor(.85f.Gray());
                elementCounter.style.marginRight = 6;
                headerBar.Add(elementCounter);
            }

            //AddButton()
            {
                addButton = new Button(() => { AddButtonPressed(); })
                {
                    text = "+",
                    name = "superlist-add"
                };
                //addButton.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);

                addButton.style
                    .FixedSize(24, 18)
                    .Colors(null, Color.clear, Color.clear)
                    .Text(14, TextAnchor.LowerCenter)
                    .BorderWidth(0)
                    .Radius(0, topRight: 6)
                    .Margins(0)
                    .Padding(0);

                headerBar.Add(addButton);
            }

            this.headerBar = headerBar;
            this.Add(headerBar);
        }

        //CollectionBackground()
        {
            collectionBackground = new();
            collectionBackground.name = "superlist-collection";
            collectionBackground.style.flexDirection = FlexDirection.Column;
            collectionBackground.style.Colors(null, .254902f.Gray(), .1411765f.Gray()).Padding(4).BorderWidth(1, top: 0).Radius(0, bottom: 4);
            collectionBackground.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None; 

            this.Add(collectionBackground);
        }
    }




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
            background.style.backgroundColor = new StyleColor(Color.clear);
            content = new VisualElement();
            content.style.flexDirection = FlexDirection.Row;
            background.Add(content);

            removeButton = new Button(() => { parentList?.RemoveButtonPressed(selfIndex); })
            {
                text = "-"
            };
            removeButton.style.width = 18;
            removeButton.style.height = 14;
            removeButton.style.marginLeft = 4;
            removeButton.style.BorderNull().Padding(0);
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
#endif