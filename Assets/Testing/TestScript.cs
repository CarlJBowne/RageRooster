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
                elementCounter.style.width = 36;
                elementCounter.style.unityTextAlign = TextAnchor.MiddleRight;
                elementCounter.style.color = new StyleColor(.85f.Gray());
                elementCounter.style.marginRight = 6;
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

    public class Item : VisualElement
    {
        /*
         Plan / Pseudocode (detailed):
         - Goal:
           1) Place the remove button to the left of the element.
           2) Allow clicking the element background (or its body) to select the item with a visual highlight.
           3) Make the visual element stretch to fit the content's size (flexible width/height as needed).

         - Strategy:
           - Keep only lightweight references in the Item: store the owner SerializedObject and the element property path.
           - Build a container `background` that spans/grows and receives clicks for selection.
           - Inside `background` place a `content` row. Add `removeButton` first (left) and then the `body` (property field or drawn element).
           - Configure flex layout:
             - `background` and `content` get `flexGrow = 1` so they stretch.
             - `body` (the PropertyField or returned VisualElement) gets `flexGrow = 1` and `alignSelf = Stretch` so it fills available space.
           - Selection:
             - Clicking `background` selects this Item.
             - Maintain selection purely inside visual elements (no change to SerializedObject). On select:
               - Unselect other items in the parent list (iterate `parentList.items`).
               - Update visual state (change background color / border).
             - Ensure `removeButton` click does not bubble and cause selection when pressed (stop propagation on its click event).
           - Removal:
             - Keep existing remove callback behavior (calls parentList.RemoveButtonPressed_Default).
           - Defensive drawing:
             - Resolve a fresh SerializedProperty for the element when building the body (via `owner.FindProperty(elementPropertyPath)`).
             - If `drawBody` returns a VisualElement, use it; otherwise fallback to a `PropertyField`.
             - Bind the created body to `owner` to keep UI in sync.

         - Implementation details to watch:
           - Use UIElements style properties (`flexGrow`, `alignSelf`, `minHeight`) to achieve stretching behavior.
           - Use `ClickEvent` for selection and `evt.StopPropagation()` on the remove button's click to avoid interfering with selection.
           - Keep all editor-only serialized property access guarded by `#if UNITY_EDITOR`.

        * Move the remove control to the RIGHT side of the item.
           * Make the remove control visually minimal: a plain "-" glyph, no background color, no border.
           * Keep remove click from selecting the item (stop propagation).
           * Keep selection by clicking the background or any non-interactive area of the body.
           * Ensure the body (PropertyField or custom returned VisualElement) stretches to fill remaining space.
         - Steps:
           1) Store only lightweight references: owner SerializedObject, element property path, parent list.
           2) Build `background` (row) and `content` (row) that grow.
           3) Create `removeButton` but do NOT add it first. Configure it to:
               - display text "-" only
               - backgroundColor = clear, border widths = 0
               - small fixed width and height
               - stop event propagation on ClickEvent
               - perform removal on click
           4) Resolve a fresh SerializedProperty and let `drawBody` provide a body element if available.
               - If not, create a UnityEditor.UIElements.PropertyField fallback.
               - Style the body: flexGrow = 1, alignSelf = Stretch so it expands.
               - Register body click to SelectSelf (so clicking body selects).
               - Bind the body to the owner SerializedObject.
           5) Add body to `content`, then add the `removeButton` so it appears on the right.
           6) Add `content` to `background` and `background` to this.
           7) Selection: when selected, change background coloring and border top/bottom; unselect siblings.
        * Add a lightweight "grab icon" element to the LEFT of the item content to mimic Unity's list grab handle.
          * Use a simple Label with a glyph (e.g. "≡") so no heavy event handling is required.
          * Make it fixed-size, center-aligned, visually subtle (light gray), and stop propagation on clicks so it
            does not select the item when interacted with.
          * Optionally register MouseDown/MouseUp to stop propagation as well so dragging interactions won't toggle selection.
        * Increase vertical separation between items:
          * Increase the background element's bottom margin to create clearer separation.
          * Add a small top margin or vertical padding if necessary for better visual spacing.
        * Insert the grab handle into the content BEFORE the body so it appears on the left.
        * Keep the remove button on the right as-is.
        * Ensure body creation and binding logic is unchanged; body should flex to fill the space between grab handle and remove button.
        * Ensure all event callbacks on interactive bits stop propagation where appropriate (grab handle and remove button).
        */

        // Store lightweight references for owner object and property path
        private SerializedObject ownerObjectLocal;
        private string elementPropertyPath;
        private SuperList<T> parentList;

        public Item(SerializedObject owner, string elementPath, SuperList<T> parent, PropertyToVisualElementDelegate drawBody)
        {
            ownerObjectLocal = owner;
            elementPropertyPath = elementPath;
            parentList = parent;

            // Root element (this) should grow if parent allows it
            this.style.flexGrow = 1;
            this.style.minHeight = 18;

            // Background container
            background = new VisualElement();
            background.name = "superlist-item-background";
            background.style.flexDirection = FlexDirection.Row;
            background.style.alignItems = Align.Center;
            background.style.justifyContent = Justify.FlexStart;
            background.style.flexGrow = 1;
            background.style.paddingLeft = 2;
            background.style.paddingRight = 2;
            // Increase vertical spacing between items for clearer separation
            background.style.marginBottom = 8;
            background.style.marginTop = 4;
            background.style.backgroundColor = new StyleColor(Color.clear);

            // Clicking the background selects this element
            background.RegisterCallback<ClickEvent>((evt) =>
            {
                SelectSelf();
            });

            // Content row holds grab handle (left), body (expandable) and remove button (fixed, right)
            content = new VisualElement();
            content.style.flexDirection = FlexDirection.Row;
            content.style.alignItems = Align.Center;
            content.style.flexGrow = 1;
            content.style.flexShrink = 1;

            // Grab handle on the LEFT - minimal visual "≡"
            dragHandle = new Label("≡");
            dragHandle.name = "superlist-item-grab";
            dragHandle.style.width = 18;
            dragHandle.style.height = 16;
            dragHandle.style.marginRight = 8;
            dragHandle.style.marginLeft = 2;
            dragHandle.style.unityTextAlign = TextAnchor.MiddleCenter;
            dragHandle.style.color = new StyleColor(new Color(0.72f, 0.72f, 0.72f));
            dragHandle.style.alignSelf = Align.Center;
            // Make it clear that it's not selectable when clicked; stop propagation
            dragHandle.RegisterCallback<ClickEvent>((evt) => evt.StopPropagation());
            // Also prevent pointer down/up from bubbling (useful if implementing drag later)
            dragHandle.RegisterCallback<MouseDownEvent>((evt) => evt.StopPropagation());
            dragHandle.RegisterCallback<MouseUpEvent>((evt) => evt.StopPropagation());

            // Prepare the remove button (right side) - minimal visual: just a "-" glyph
            removeButton = new Button(() => { parentList?.RemoveButtonPressed_Default(this); })
            {
                text = "-"
            };
            removeButton.name = "superlist-item-remove";
            removeButton.style.width = 18;
            removeButton.style.height = 16;
            removeButton.style.marginLeft = 6;
            removeButton.style.marginRight = 0;
            // Make it visually minimal: clear background, no border, subtle text color
            removeButton.style.backgroundColor = new StyleColor(Color.clear);
            removeButton.style.borderTopWidth = 0;
            removeButton.style.borderBottomWidth = 0;
            removeButton.style.borderLeftWidth = 0;
            removeButton.style.borderRightWidth = 0;
            removeButton.style.unityTextAlign = TextAnchor.MiddleCenter;
            removeButton.style.color = new StyleColor(new Color(0.78f, 0.78f, 0.78f));
            // Prevent clicks on the remove button from bubbling up and selecting the item
            removeButton.RegisterCallback<ClickEvent>((evt) => evt.StopPropagation());

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

                    // Clicking anywhere in the body should select this item as well,
                    // but we don't stop propagation so controls inside the body still work.
                    body.RegisterCallback<ClickEvent>((evt) =>
                    {
                        SelectSelf();
                    });

                    // Bind the body to the owner object so it updates correctly
                    try { body.Bind(ownerObjectLocal); } catch { }

                    // Add grab handle first so it appears on the left, then the body takes remaining space
                    content.Add(dragHandle);
                    content.Add(body);
                }
                else
                {
                    // If no body, still add the grab handle so layout remains consistent
                    content.Add(dragHandle);
                }
            }
            catch
            {
                // drawing failed -> ensure grab handle exists to keep consistent spacing
                if (!content.Contains(dragHandle)) content.Add(dragHandle);
            }
#endif

            // Finally add the remove button on the RIGHT
            content.Add(removeButton);

            background.Add(content);
            this.Add(background);
        }

        // Visual pieces for the item
        public VisualElement background { get; private set; }
        public VisualElement content { get; private set; }
        public Button removeButton { get; private set; }
        public VisualElement dragHandle { get; private set; }

        // Selection state
        private bool _selected = false;
        public bool selected
        {
            get => _selected;
            private set
            {
                _selected = value;
                // Visual feedback for selection: subtle highlight and border
                if (_selected)
                {
                    background.style.backgroundColor = new StyleColor(new Color(0.14f, 0.24f, 0.42f, 0.12f));
                    background.style.borderTopColor = new StyleColor(new Color(0.14f, 0.24f, 0.42f, 0.25f));
                    background.style.borderBottomColor = new StyleColor(new Color(0.14f, 0.24f, 0.42f, 0.25f));
                    background.style.borderLeftColor = new StyleColor(Color.clear);
                    background.style.borderRightColor = new StyleColor(Color.clear);
                    background.style.borderTopWidth = 1;
                    background.style.borderBottomWidth = 1;
                }
                else
                {
                    background.style.backgroundColor = new StyleColor(Color.clear);
                    background.style.borderTopWidth = 0;
                    background.style.borderBottomWidth = 0;
                }
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


    //Suggestions for later

    // Avoid keeping long-lived SerializedProperty instances across ApplyModifiedProperties()—they can become stale. Prefer calling GetArrayElementAtIndex() when you need the current element.
}


#endif