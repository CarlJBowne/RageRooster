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
using UnityEditor.UIElements;

public class TestScript : MonoBehaviour
{
    [SerializeField, SerializeReference] List<PlayerButtonAction> buttons = new();

#if UNITY_EDITOR
    [CustomEditor(typeof(TestScript))]
    public class _Editor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            return new SuperList<PlayerButtonAction>(serializedObject.FindProperty("buttons"))
            {
                preAddCallback = (list) =>
                {
                    PlayerButtonAction.ShowChooseTypeMenu(typeof(PlayerButtonAction), false, (type) =>
                    {
                        list.CreatePropertySlot(out int newID);
                        list.SetOrCreateItemValue(newID, Activator.CreateInstance(type));
                        list.CreateItemElement(newID);
                    });
                },
            };
        }
    }
#endif
}

#if UNITY_EDITOR

public class SuperList<T> : VisualElement
{
    // Plan / Pseudocode (detailed):
    // 1) Fix new-item drawing:
    //    - Avoid holding long-lived SerializedProperty instances that can become stale.
    //    - When creating UI for an element, resolve a fresh SerializedProperty via the owning SerializedObject
    //      using the list property's path + ".Array.data[i]".
    //    - Ensure the created element (and any returned VisualElement from DrawElementBody) is bound to the
    //      owning SerializedObject so UIElements reflect serialized data changes.
    //
    // 2) Fix Reset / external updates:
    //    - Unity may change the underlying array (e.g. Reset) without reconstructing this VisualElement.
    //    - Use EditorApplication.update polling (editor-only) to detect when the serialized array size differs
    //      from the number of UI items and rebuild the UI accordingly.
    //    - Register the update callback in the constructor and unregister on DetachFromPanelEvent to avoid leaks.
    //
    // 3) Implementation details:
    //    - CreateItemElement will compute elementPath = property.propertyPath + ".Array.data[" + ID + "]".
    //    - Item constructor will accept the owner SerializedObject and elementPath and build a PropertyField
    //      from owner.FindProperty(elementPath). After building the body (via DrawElementBody or fallback),
    //      call body.Bind(owner) to ensure proper binding.
    //    - RebuildItems clears existing UI children and items list, then recreates items from 0..arraySize-1.
    //    - Guard registrations and ensure safe null checks.
    //
    // This approach keeps SerializedProperty usage short-lived (only when building UI) and ensures
    // new elements and external changes are reflected in the inspector UI.

    public SuperList(SerializedProperty listProperty, PropertyToVisualElementDelegate drawElementBody = null)
    {
        property = listProperty;
        serializedObject = listProperty.serializedObject;
        DrawElementBody = drawElementBody;

        CreateVisualElements();

        // Initialize elements list if property exists
        items = new List<Item>();
        if (property != null)
        {
            property.serializedObject.Update();
            for (int i = 0; i < arraySize; i++) CreateItemElement(i);
            arraySize = arraySize; // force UI counter update
        }

        this.Bind(listProperty.serializedObject);

        // Register polling to detect external changes (Reset, script changes, etc.)
        RegisterEditorUpdate();
        // Ensure we unregister when the element is removed from the panel
        this.RegisterCallback<DetachFromPanelEvent>((evt) => { UnregisterEditorUpdate(); });
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
    public SerializedObject serializedObject { get; private set; }
    public List<Item> items { get; private set; }

    public int arraySize
    {
        get => (property != null) ? property.arraySize : 0;
        set
        {
            if (property == null) return;

            if (value > property.arraySize) expanded = true;
            expandable = value > 0;

            property.serializedObject.Update();
            property.arraySize = value;
            if (elementCounter != null)
                elementCounter.text = (property != null) ? property.arraySize.ToString() : "0";
            // Do not ApplyModifiedProperties here — callers should apply as needed, but keep UI in sync
        }
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
            if (!value) expanded = false;
        }
    }


    #region Add Systems

    protected virtual void AddButtonPressed_Default()
    {
        if (preAddCallback != null) preAddCallback(this);
        else
        {
            CreatePropertySlot(out int newID);
            SetOrCreateItemValue(newID);
            CreateItemElement(newID);
        }
    }

    public virtual void CreatePropertySlot(out int newID)
    {
        if (property == null) throw new InvalidOperationException("Property is null");

        property.serializedObject.Update();

        arraySize++;

        property.serializedObject.ApplyModifiedProperties();

        newID = property.arraySize - 1;
    }

    public virtual void SetOrCreateItemValue(int ID, object input = null)
    {
        if (property == null) throw new InvalidOperationException("Property is null");
        property.serializedObject.Update();
        SerializedProperty targetProperty = property.GetArrayElementAtIndex(ID) ?? throw new ArgumentOutOfRangeException(nameof(ID));

        // If input is null, provide a sensible default depending on the property type.
        if (input == null)
        {
            switch (targetProperty.propertyType)
            {
                case SerializedPropertyType.Integer:
                    targetProperty.intValue = 0;
                    break;
                case SerializedPropertyType.Boolean:
                    targetProperty.boolValue = false;
                    break;
                case SerializedPropertyType.Float:
                    targetProperty.floatValue = 0f;
                    break;
                case SerializedPropertyType.String:
                    targetProperty.stringValue = string.Empty;
                    break;
                case SerializedPropertyType.Enum:
                    targetProperty.intValue = 0;
                    break;
                case SerializedPropertyType.ObjectReference:
                    targetProperty.objectReferenceValue = null;
                    break;
                case SerializedPropertyType.ManagedReference:
                    try { targetProperty.managedReferenceValue = Activator.CreateInstance(typeof(T)); }
                    catch { targetProperty.managedReferenceValue = null; }
                    break;
                default:
                    // Try managed reference as fallback
                    try { targetProperty.managedReferenceValue = Activator.CreateInstance(typeof(T)); } catch { }
                    break;
            }
        }
        else
        {
            // Convert input to the appropriate underlying serialized value.
            try
            {
                switch (targetProperty.propertyType)
                {
                    case SerializedPropertyType.Integer:
                        targetProperty.intValue = Convert.ToInt32(input);
                        break;
                    case SerializedPropertyType.Boolean:
                        targetProperty.boolValue = Convert.ToBoolean(input);
                        break;
                    case SerializedPropertyType.Float:
                        targetProperty.floatValue = Convert.ToSingle(input);
                        break;
                    case SerializedPropertyType.String:
                        targetProperty.stringValue = Convert.ToString(input);
                        break;
                    case SerializedPropertyType.Enum:
                        // Enums are stored as intValue
                        targetProperty.intValue = Convert.ToInt32(input);
                        break;
                    case SerializedPropertyType.ObjectReference:
                        targetProperty.objectReferenceValue = input as UnityEngine.Object;
                        break;
                    case SerializedPropertyType.ManagedReference:
                        targetProperty.managedReferenceValue = input;
                        break;
                    default:
                        // best-effort fallback
                        try { targetProperty.managedReferenceValue = input; } catch { }
                        break;
                }
            }
            catch
            {
                // If conversion fails, attempt a safe fallback default
                switch (targetProperty.propertyType)
                {
                    case SerializedPropertyType.Integer:
                        targetProperty.intValue = 0;
                        break;
                    case SerializedPropertyType.Boolean:
                        targetProperty.boolValue = false;
                        break;
                    case SerializedPropertyType.Float:
                        targetProperty.floatValue = 0f;
                        break;
                    case SerializedPropertyType.String:
                        targetProperty.stringValue = string.Empty;
                        break;
                    default:
                        // leave as-is for object/managed references
                        break;
                }
            }
        }

        property.serializedObject.ApplyModifiedProperties();
    }

    public virtual void CreateItemElement(int ID)
    {
        if (property == null) throw new InvalidOperationException("Property is null");
        if (serializedObject == null) throw new InvalidOperationException("Owner object is null");

        // Ensure the SerializedObject is up-to-date before getting the element
        property.serializedObject.Update();

        // Compute the element path so we can resolve a fresh SerializedProperty whenever needed.
        string elementPath = $"{property.propertyPath}.Array.data[{ID}]";

        Item element = new(serializedObject, elementPath, this, DrawElementBody);
        items.Add(element);
        collectionBackground.Add(element);

        // Bind the newly created element to the owner object so it displays immediately and reacts to changes.
        try { element.Bind(serializedObject); } catch { }
    }

    #endregion

    #region Remove Systems

    protected virtual void RemoveButtonPressed_Default(Item E)
    {
        // Return early if index invalid
        if (items == null || E == null || !items.Contains(E)) return;
        int index = items.IndexOf(E);
        if (preRemoveCallback != null) preRemoveCallback(this, index);
        else
        {
            if (property == null) return;
            if (items == null || index < 0 || index >= items.Count) return;

            // Remove from serialized property first
            DeletePropertySlotAt(index);

            // Then remove the UI element and internal list entry
            RemoveItemElementAt(index);
        }
    }

    public virtual void DeletePropertySlotAt(int index)
    {
        if (property == null) return;
        property.serializedObject.Update();

        // Delete once. For object reference slots Unity may leave a null placeholder and require a second delete call.
        property.DeleteArrayElementAtIndex(index);

        // If the array still has an element at this index and it's an object reference that is null,
        // delete it again to fully remove the slot.
        if (index < property.arraySize)
        {
            SerializedProperty maybeElem = property.GetArrayElementAtIndex(index);
            if (maybeElem != null && maybeElem.propertyType == SerializedPropertyType.ObjectReference && maybeElem.objectReferenceValue == null)
            {
                property.DeleteArrayElementAtIndex(index);
            }
        }

        // Keep UI counter accurate
        arraySize = property.arraySize;
        property.serializedObject.ApplyModifiedProperties();
    }

    public virtual void RemoveItemElementAt(int index)
    {
        if (items == null) return;
        if (index < 0 || index >= items.Count) return;

        if (collectionBackground != null && items[index] != null) collectionBackground.Remove(items[index]);
        items.RemoveAt(index);
    }


    protected virtual void ClearButtonPressed_Default()
    {
        if (preClearCallback != null) preClearCallback(this);
        else
        {
            if (property != null)
            {
                property.serializedObject.Update();
                property.arraySize = 0;
                property.serializedObject.ApplyModifiedProperties();
            }

            if (items != null)
            {
                foreach (var el in items)
                {
                    collectionBackground.Remove(el);
                }
                items.Clear();
            }
            arraySize = 0;
        }
    }
    #endregion


    // The delegate callers should set to produce the body VisualElement for a given element property.
    public PropertyToVisualElementDelegate DrawElementBody
    { get; set; }

    public delegate VisualElement PropertyToVisualElementDelegate(SerializedProperty elementProperty);
    public delegate void PassListDelegate(SuperList<T> list);
    public delegate void RemoveElementDelegate(SuperList<T> list, int index);

    // Editor update registration
    private bool updateRegistered = false;

    private void RegisterEditorUpdate()
    {
#if UNITY_EDITOR
        if (updateRegistered) return;
        EditorApplication.update += EditorUpdate;
        updateRegistered = true;
#endif
    }

    private void UnregisterEditorUpdate()
    {
#if UNITY_EDITOR
        if (!updateRegistered) return;
        EditorApplication.update -= EditorUpdate;
        updateRegistered = false;
#endif
    }

    private void EditorUpdate()
    {
#if UNITY_EDITOR
        if (property == null) return;
        try
        {
            property.serializedObject.Update();
            int size = property.arraySize;
            if (items == null) items = new List<Item>();
            if (size != items.Count)
            {
                // External change detected (Reset, undo, etc.) -> rebuild UI to match serialized data
                RebuildItems();
            }
        }
        catch
        {
            // swallow exceptions to avoid EditorApplication update throwing
        }
#endif
    }

    private void RebuildItems()
    {
        if (property == null) return;
        // Clear existing visuals
        if (collectionBackground != null)
        {
            collectionBackground.Clear();
        }
        if (items != null)
        {
            items.Clear();
        }
        // Recreate elements from serialized property
        property.serializedObject.Update();
        int size = property.arraySize;
        for (int i = 0; i < size; i++)
        {
            CreateItemElement(i);
        }
        // Update counter
        if (elementCounter != null) elementCounter.text = size.ToString();
        // Ensure display state aligns with expandability
        expandable = size > 0;
    }

    private void CreateVisualElements()
    {
        //HeaderBar()
        {
            var headerBar = new VisualElement();
            headerBar.name = "superlist-headerbar";

            headerBar.style
                .Flex(FlexDirection.Row)
                .Align(Align.Center)
                .FixedSize(height: 20)
                .Colors(null, .2078432f.Gray(), .1411765f.Gray())
                .Border(1)
                .Radius(0, top: 6);

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
                label.style
                    .Flex(grow: 1)
                    .Text(12, TextAnchor.MiddleLeft)
                    .Colors(color: .82f.Gray());
                label.RegisterCallback<ClickEvent>((evt) =>
                {
                    if (arraySize == 0) return;
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
                elementCounter.style
                    .FixedSize(width: 36)
                    .Text(null, TextAnchor.MiddleRight)
                    .Colors(color: .85f.Gray())
                    .Margins(right: 6);
                headerBar.Add(elementCounter);
            }

            //AddButton()
            {
                addButton = new Button(() => { AddButtonPressed_Default(); })
                {
                    text = "+",
                    name = "superlist-add"
                };
                //addButton.style.backgroundColor = new Color(0.18f, 0.18f, 0.18f);

                addButton.style
                    .FixedSize(24, 18)
                    .Colors(null, Color.clear, Color.clear)
                    .Text(14, TextAnchor.LowerCenter)
                    .Border(0)
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
            collectionBackground = new() { name = "superlist-collection" };
            collectionBackground.style
                .Colors(null, .254902f.Gray(), .1411765f.Gray())
                .Padding(horizontal: 4)
                .Border(1, top: 0)
                .Radius(0, bottom: 4)
                .Flex(FlexDirection.Column);
            collectionBackground.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;

            this.Add(collectionBackground);
        }
    }

    public class Item : VisualElement
    {
        public Item(SerializedObject owner, string elementPath, SuperList<T> parent, PropertyToVisualElementDelegate drawBody)
        {
            ownerObjectLocal = owner;
            elementPropertyPath = elementPath;
            parentList = parent;

            // Background container is the Item root itself
            name = "superlist-item";

            style.Flex(FlexDirection.Row, 1).Align(Align.Center, Justify.FlexStart)
                .Padding(vertical: 4).Border(vertical: 1)
                .Colors(null, Color.clear, new(0, 0, 0, .1f));
            style.flexGrow = 1;
            style.minHeight = 18;

            //Drag Handle (container that stretches full height so clicks work anywhere in the column)
            {
                // Use a Button so the handle is an accessible, clickable control, but style it to have no background or border.
                var handleBtn = new Button() { name = "superlist-item-grab-symbol" };

                // Fixed width column, stretched vertically to match the item's height.
                handleBtn.style
                    .FixedSize(width: 18)
                    .Align(null, null, Align.Stretch)
                    .Flex(shrink: 0)
                    .Margins(left: 2, right: 2);

                // Make the button visually minimal: no background, no border, transparent hover/active.
                handleBtn.style
                    .Colors(null, Color.clear, Color.clear)
                    .Border(0)
                    .Padding(0);

                // Center contents inside the container
                handleBtn.style.justifyContent = Justify.Center;
                handleBtn.style.alignItems = Align.Center;

                // Ensure it sits above potential overlapping content and is positioned normally
                handleBtn.style.position = Position.Relative;

                // Make the control focusable so it reliably receives pointer/click events across editor versions.
                handleBtn.focusable = true;

                // Inner glyph label (purely visual)
                var glyph = new Label("≡") { name = "superlist-item-grab-glyph" };
                glyph.style
                    .Text(null, TextAnchor.MiddleCenter)
                    .Align(null, null, Align.Center)
                    .FixedSize(width: 16);
                glyph.style.flexGrow = 0;
                glyph.style.maxWidth = 16;
                glyph.style.marginTop = 0;
                glyph.style.marginBottom = 0;

                handleBtn.Add(glyph);

                // Use PointerDownEvent for robust selection handling in the editor.
                // Stop propagation so clicks on the handle don't select other UI or trigger other callbacks.
                handleBtn.RegisterCallback<PointerDownEvent>((evt) =>
                {
                    SelectSelf();
                    evt.StopPropagation();
                });

                // Also handle PointerUp to stop bubbling just in case (prevents accidental parent handlers).
                handleBtn.RegisterCallback<PointerUpEvent>((evt) =>
                {
                    evt.StopPropagation();
                });

                // Assign to the public property (type is VisualElement) so rest of code stays unchanged.
                dragHandle = handleBtn;

                this.Add(dragHandle);
            }

            //Content
            {
                content = new VisualElement();
                content.style.Flex(FlexDirection.Row, 1, 1).Align(Align.Center);
                content.style.alignSelf = Align.Stretch;
                this.Add(content);
            }

            //Remove Button
            {
                removeButton = new Button(() => { parentList?.RemoveButtonPressed_Default(this); })
                {
                    text = "-",
                    name = "superlist-item-remove"
                };
                removeButton.style
                    .FixedSize(16, 16)
                    .Margins(left: 6)
                    .Border(0)
                    .Text(null, TextAnchor.MiddleCenter)
                    .Colors(.78f.Gray());

                // Prevent remove button clicks from selecting the item
                removeButton.RegisterCallback<ClickEvent>((evt) => evt.StopPropagation());
                this.Add(removeButton);
            }



#if UNITY_EDITOR
    // Build the body using a fresh SerializedProperty resolved from the owner object.
    VisualElement body = null;
    try
    {
        SerializedProperty freshProp = ownerObjectLocal.FindProperty(elementPropertyPath);
        if (drawBody != null)
        {
            try
            {
                body = drawBody(freshProp);
            }
            catch
            {
                body = null;
            }
        }

        if (body == null && freshProp != null)
        {
            // fallback: a simple PropertyField bound to the property
            var pf = new UnityEditor.UIElements.PropertyField(freshProp);
            pf.style.minHeight = 16;
            pf.style.height = StyleKeyword.Auto;
            pf.style.flexGrow = 1;
            pf.style.flexShrink = 1;
            pf.style.alignSelf = Align.Stretch;
            body = pf;
        }

        if (body != null)
        {
            // Make the body stretch to fit available space
            body.style.flexGrow = 1;
            body.style.flexShrink = 1;
            body.style.alignSelf = Align.Stretch;

            // Do NOT register the body to select the item; selection is only via drag handle.

            // Bind the body to the owner object so it updates correctly
            try { body.Bind(ownerObjectLocal); } catch { }

            content.Add(body);
        }

    }
    catch
    {

    }
#endif
        }

        public VisualElement dragHandle { get; private set; }
        public VisualElement content { get; private set; }
        public Button removeButton { get; private set; }

        // Data

        private SerializedObject ownerObjectLocal;
        private string elementPropertyPath;
        private SuperList<T> parentList;
        private bool _selected = false;
        public bool selected
        {
            get => _selected;
            private set
            {
                _selected = value;
                // Visual feedback for selection: subtle highlight and border
                style.backgroundColor = new StyleColor(_selected ? new Color(0.14f, 0.24f, 0.42f, 0.12f): Color.clear);
            }
        }

        // Select this item and clear selection on siblings
        private void SelectSelf()
        {
            if (parentList?.items != null)
            {
                foreach (var it in parentList.items)
                {
                    if (it != null && it != this) it.SetSelected(false);
                }
            }
            SetSelected(true);
        }

        public void SetSelected(bool value)
        {
            selected = value;
        }
    }


}


#endif