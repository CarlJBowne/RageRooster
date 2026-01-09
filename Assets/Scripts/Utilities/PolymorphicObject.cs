using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

public abstract class PolymorphicObject
{
#if UNITY_EDITOR

    public virtual VisualElement BodyDrawer(SerializedProperty property)
    {
        VisualElement result = new();
        property.IterateAndDraw(result);
        return result;
    }

    [CustomPropertyDrawer(typeof(PolymorphicObject), true)]
    public class Drawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            bool hasChoosingHeader = false;

            try
            {
                // Most common / recommended path: use fieldInfo provided by PropertyDrawer
                if (fieldInfo != null) hasChoosingHeader = fieldInfo.IsDefined(typeof(ChoosingHeaderAttribute), inherit: true);
            }
            catch
            {
                // Ignore and treat as not having the attribute.
                hasChoosingHeader = false;
            }

            if (hasChoosingHeader)
            {
                // Use the header-style drawer which contains the type chooser
                return new HeaderDrawer(property);
            }
            else if (property.managedReferenceValue is PolymorphicObject obj and not null)
            {
                return obj.BodyDrawer(property);
            }
            VisualElement fallback = new Label("No Object present");
            return fallback;
        }
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class ChoosingHeaderAttribute : Attribute
    {

    }


    public class HeaderDrawer : FoldoutPlus
    {
        public HeaderDrawer(SerializedProperty property)
        {
            this.property = property ?? throw new ArgumentNullException(nameof(property));
            BaseType = GetDeclaredFieldType() ?? typeof(PolymorphicObject);
            DrawGUI(forceRedo: true);

        }

        public SerializedProperty property { get; private set; }
        public Button TypeButton { get; private set; }
        public Type BaseType { get; private set; }
        public Type CurrentType { get; private set; } = null;
        public Action<Type> OnTypeChanged;
        public bool drawnSuccessfully { get; private set; } = false;

        //Inherited fields from FoldoutPlus:
        //  Toggle header
        //  VisualElement arrowButton
        //  Label label
        //  VisualElement headerSide
        //  bool expanded
        //  bool expandable
        //  bool toggleOnLabelClick

        public VisualElement DrawGUI(bool forceRedo = false)
        {
            if (!forceRedo && drawnSuccessfully && this.childCount > 0) return this;

            this.Clear();

            this.text = property != null ? property.displayName : "Polymorphic Object";
            if (property != null)
            {
                this.value = property.isExpanded;
                this.RegisterValueChangedCallback(evt =>
                {
                    if (property == null) return;
                    property.isExpanded = evt.newValue;
                    property.serializedObject.ApplyModifiedProperties();
                });
            }

            this.contentContainer.style.marginLeft = 12;
            this.contentContainer.style.marginTop = 2;
            this.contentContainer.style.marginBottom = 2;
            this.contentContainer.style.flexDirection = FlexDirection.Column;

            CurrentType = property?.managedReferenceValue?.GetType();

            TypeButton = new Button(ChooseAndSetType);
            try { this.headerSide.Add(TypeButton); }
            catch { this.Add(TypeButton); }

            return this;
        }

        protected override void OnEstablishElements()
        {
            base.OnEstablishElements();
            UpdateObject();
            drawnSuccessfully = true;
        }

        public void UpdateObject()
        {
            contentContainer.Clear();
            if (property != null && property.managedReferenceValue is PolymorphicObject P && P != null)
                contentContainer.Add(P.BodyDrawer(property));
            OnTypeChanged?.Invoke(property?.managedReferenceValue?.GetType());

            TypeButton.text = property?.managedReferenceValue != null
                ? property.managedReferenceValue.GetType().Name
                : "Choose Type";

            expandable = CurrentType != null;
            if (CurrentType == null) expanded = false;
        }

        private Type GetDeclaredFieldType()
        {
            if (property == null) return null;

            // If Unity gives a managedReferenceFieldTypename, try to parse it first.
            if (!string.IsNullOrEmpty(property.managedReferenceFieldTypename))
            {
                // managedReferenceFieldTypename can contain tokens; try to resolve each token to a Type.
                var parts = property.managedReferenceFieldTypename.Split(' ');
                foreach (var part in parts)
                {
                    var t = Type.GetType(part);
                    if (t != null) return t;
                }
            }

            // Fall back to reflection over the target object and the propertyPath.
            object target = property.serializedObject.targetObject;
            if (target == null) return null;

            Type currentType = target.GetType();
            string path = property.propertyPath;
            string[] tokens = path.Split('.');

            for (int i = 0; i < tokens.Length; i++)
            {
                string token = tokens[i];

                if (token == "Array")
                {
                    // 'Array' is followed by 'data[x]' token; the element type will be handled when we hit data[...]
                    continue;
                }

                if (token.StartsWith("data["))
                {
                    // The previous field was a collection; get its element type.
                    if (currentType.IsArray)
                    {
                        currentType = currentType.GetElementType() ?? currentType;
                    }
                    else if (currentType.IsGenericType)
                    {
                        var genDef = currentType.GetGenericTypeDefinition();
                        if (genDef == typeof(List<>) || currentType.GetInterfaces().Any(iFace => iFace.IsGenericType && iFace.GetGenericTypeDefinition() == typeof(IEnumerable<>)))
                        {
                            currentType = currentType.GetGenericArguments()[0];
                        }
                        else
                        {
                            // Unknown collection type; abort resolution.
                            return null;
                        }
                    }
                    else
                    {
                        // Unknown collection shape; cannot resolve element type.
                        return null;
                    }
                    continue;
                }

                FieldInfo field = GetFieldInfoRecursive(currentType, token);
                if (field == null)
                {
                    // Could not find the field; abort.
                    return null;
                }

                currentType = field.FieldType;
            }

            // If the final resolved type is a collection, return its element type.
            if (currentType.IsArray) return currentType.GetElementType() ?? currentType;
            if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == typeof(List<>))
                return currentType.GetGenericArguments()[0];

            return currentType;
        }

        private static FieldInfo GetFieldInfoRecursive(Type type, string fieldName)
        {
            while (type != null)
            {
                var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                var fi = type.GetField(fieldName, flags);
                if (fi != null) return fi;

                // Unity sometimes stores auto-property fields as backing fields with this pattern.
                string backing = $"<{fieldName}>k__BackingField";
                fi = type.GetField(backing, flags);
                if (fi != null) return fi;

                type = type.BaseType;
            }
            return null;
        }

        public void ChooseAndSetType() => ShowChooseTypeMenu(BaseType, property?.managedReferenceValue != null, ChangeType);

        private void ChangeType(Type t)
        {
            if (property == null) return;
            CurrentType = t;

            property.managedReferenceValue = CurrentType == null ? null : Activator.CreateInstance(t);

            UpdateObject();

            property.serializedObject.ApplyModifiedProperties();

            expanded = true;
        }
    }

    public class TabbedDrawer : TabView
    {
        public TabbedDrawer(SerializedObject serializedObject) : base()
        {
            this.serializedObject = serializedObject;
            reorderable = false;
            tabs = new();
            this.GetDescendent(0, 0).style.flexGrow = 1f;
        }

        public SerializedObject serializedObject { get; private set; }
        public System.Collections.Generic.List<Tab> tabs { get; private set; }

        public void CreateTab(string displayName, SerializedProperty prop)
        {
            tabs.Add(new Tab(displayName, prop));
            Add(tabs[^1]);
        }


        public class Tab : UnityEngine.UIElements.Tab
        {
            public Tab(string title, SerializedProperty property) : base(title)
            {
                displayName = title;
                this.property = property;
                tabHeader.style.paddingLeft = 5;
                tabHeader.style.paddingRight = 5;
                tabHeader.style.flexGrow = 1f;
                tabHeader.style.justifyContent = Justify.Center;

                bodyDrawer = new PolymorphicObject.HeaderDrawer(property);
                contentContainer.Add(bodyDrawer);

                UpdateLiteralObject(property.managedReferenceValue?.GetType());
                bodyDrawer.OnTypeChanged += UpdateLiteralObject;
            }

            public string displayName { get; private set; }
            public SerializedProperty property { get; private set; }
            public PolymorphicObject.HeaderDrawer bodyDrawer { get; private set; }


            private void UpdateLiteralObject(Type T) => tabHeader.style.color = T != null ? Color.white : Color.gray;
        }
    }


    public static Type[] GetSubtypes(Type baseType)
    {
        return AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a =>
            {
                try { return a.GetTypes(); }
                catch { return Type.EmptyTypes; }
            })
            .Where(t =>
                !t.IsAbstract &&
                // For interfaces, include implementers; for classes, include strict subclasses only.
                t.IsSubclassOf(baseType) && (t.IsPublic || t.IsNestedPublic || t.IsNestedFamORAssem || t.IsNestedFamily)
            )
            .ToArray();
    }

    public static void ShowChooseTypeMenu(Type baseType, bool showNullOption, Action<Type> result)
    {
        GenericMenu menu = new();


        Type[] types = GetSubtypes(baseType);
        if (types.Length == 0)
        {
            menu.AddDisabledItem(new GUIContent("No subtypes available"));
        }
        else
        {
            foreach (Type t in types)
            {
                if (t == baseType) continue;
                menu.AddItem(new GUIContent(t.Name), false, () => { result?.Invoke(t); });
            }

        }

        if (showNullOption) menu.AddItem(new GUIContent("Nullify"), false, () => { result?.Invoke(null); });

        menu.ShowAsContext();
    }
#endif

    public class List<T> : System.Collections.Generic.List<T>, IPolymorphicListView where T : PolymorphicObject
    {

        public System.Collections.IList GetList() => this;
    }
    private interface IPolymorphicListView : System.Collections.IList
    {
        System.Collections.IList GetList();

#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(IPolymorphicListView))]
        public class PolymorphicListDrawer : UnityEditor.PropertyDrawer
        {
            /*
            PSEUDOCODE / PLAN (detailed):
            1. Keep existing discovery logic for the serialized "array" property and the runtime IList itemsSource.
            2. Configure the ListView as before (virtualization, header, add/remove footer, reorder).
            3. Provide makeItem to produce an empty container for each element.
            4. In bindItem:
               a. Try to resolve a SerializedProperty for the element (prefer arrayProperty).
               b. If found, draw using the existing logic (PolymorphicObject.BodyDrawer or IterateAndDraw).
               c. FALLBACK: If no SerializedProperty can be found (elemProp == null),
                  - Use the runtime itemsSource to get the raw object at the index (if available).
                  - If the runtime object is a PolymorphicObject, render a compact representation:
                    * Show a one-line header with the concrete type name.
                    * Reflect the object's fields and show simple "name: value" labels for quick visibility.
                  - Otherwise, show a Label with the object's ToString() or "Null".
               d. This fallback ensures list entries are not blank when Unity doesn't expose a SerializedProperty for the element.
            5. Keep onAdd / onRemove logic unchanged but ensure itemsSource and arrayProperty are refreshed after mutations.
            */

            ListView listView;
            Type BaseType;
            System.Collections.IList itemsSource;
            SerializedProperty rootProperty;
            SerializedProperty arrayProperty;

            public override VisualElement CreatePropertyGUI(SerializedProperty property)
            {
                /*
                PSEUDOCODE / PLAN (detailed):
                1. Keep existing discovery logic for the serialized "array" property and the runtime IList itemsSource.
                2. Configure the ListView as before (virtualization, header, add/remove footer, reorder).
                3. Provide makeItem to produce an empty container for each element.
                4. In bindItem:
                   a. Try to resolve a SerializedProperty for the element (prefer arrayProperty).
                   b. If found, draw using the existing logic (PolymorphicObject.BodyDrawer or IterateAndDraw).
                   c. FALLBACK: If no SerializedProperty can be found (elemProp == null),
                      - Use the runtime itemsSource to get the raw object at the index (if available).
                      - If the runtime object is a PolymorphicObject, render a compact representation:
                        * Show a one-line header with the concrete type name.
                        * Reflect the object's fields and show simple "name: value" labels for quick visibility.
                      - Otherwise, show a Label with the object's ToString() or "Null".
                   d. This fallback ensures list entries are not blank when Unity doesn't expose a SerializedProperty for the element.
                5. Keep onAdd / onRemove logic unchanged but ensure itemsSource and arrayProperty are refreshed after mutations.
                */

                rootProperty = property;

                arrayProperty = ResolveArraySerializedProperty(rootProperty);
                itemsSource = ResolveRuntimeList(rootProperty) ?? new System.Collections.ArrayList();
                static System.Collections.IList ResolveRuntimeList(SerializedProperty root)
                {
                    if (root == null) return null;

                    var mr = root.managedReferenceValue;
                    if (mr == null) return null;

                    if (mr is IPolymorphicListView plv) return plv.GetList();
                    if (mr is System.Collections.IList raw) return raw;

                    return null;
                }

                BaseType = GetListElementType(property) ?? typeof(PolymorphicObject);

                listView = new ListView(itemsSource);
                listView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
                listView.showFoldoutHeader = true;
                listView.headerTitle = property.displayName;
                listView.showAddRemoveFooter = true;
                listView.reorderMode = ListViewReorderMode.Animated;


                listView.makeItem = () =>
                {
                    var container = new VisualElement();
                    container.style.flexDirection = FlexDirection.Column;
                    container.style.paddingLeft = 2;
                    container.style.paddingRight = 2;
                    container.style.marginTop = 2;
                    container.style.marginBottom = 2;
                    return container;
                };

                listView.bindItem = (element, index) =>
                {
                    element.Clear();

                    SerializedProperty elemProp = null;
                    if (arrayProperty != null)
                    {
                        if (index >= 0 && index < arrayProperty.arraySize)
                            elemProp = arrayProperty.GetArrayElementAtIndex(index);
                    }
                    else
                    {
                        // Fallback to trying rootProperty as array (older setups)
                        if (rootProperty != null && rootProperty.isArray && index >= 0 && index < rootProperty.arraySize)
                            elemProp = rootProperty.GetArrayElementAtIndex(index);
                    }

                    if (elemProp != null)
                    {
                        object managedValue = elemProp.managedReferenceValue;
                        if (managedValue is PolymorphicObject po && po != null) element.Add(po.BodyDrawer(elemProp));
                        else elemProp.IterateAndDraw(element);
                        return;
                    }

                    // FALLBACK: No SerializedProperty available for this element (common for some managed-reference list shapes).
                    // Use the runtime itemsSource to render a readable representation so the list element isn't a blank line.
                    try
                    {
                        if (itemsSource != null && index >= 0 && index < itemsSource.Count)
                        {
                            var runtimeItem = itemsSource[index];
                            if (runtimeItem is PolymorphicObject runtimePo && runtimePo != null)
                            {
                                // Header showing the concrete type
                                var header = new Label(runtimePo.GetType().Name);
                                header.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Bold;
                                element.Add(header);

                                // Show a compact list of fields (public + non-public) for quick visibility
                                var fields = runtimePo.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                                foreach (var f in fields)
                                {
                                    object val = null;
                                    try { val = f.GetValue(runtimePo); } catch { val = "(unreadable)"; }
                                    var valueLabel = new Label($"{f.Name}: {val?.ToString() ?? "null"}");
                                    valueLabel.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Normal;
                                    element.Add(valueLabel);
                                }
                            }
                            else
                            {
                                // Generic fallback: show a simple label with ToString() or "Null"
                                element.Add(new Label(runtimeItem?.ToString() ?? "Null"));
                            }
                        }
                        else
                        {
                            // Nothing to show: still add a placeholder to avoid blank line
                            element.Add(new Label("Empty"));
                        }
                    }
                    catch
                    {
                        // Conservative fallback on any reflection/runtime error
                        element.Add(new Label("Unable to display element"));
                    }
                };

                listView.onAdd = OnAdd;

                listView.onRemove = OnRemove;

                listView.RegisterCallback<DetachFromPanelEvent>(evt =>
                {
                    // No special cleanup needed here currently.
                });

                return listView;
            }

            void OnAdd(BaseListView baselistView)
            {
                PolymorphicObject.ShowChooseTypeMenu(BaseType, false, (Type t) =>
                {
                    if (t == null) return;

                    // Ensure the underlying managed-reference list instance exists
                    var currentListObj = rootProperty.managedReferenceValue as IPolymorphicListView;
                    System.Collections.IList rawList = null;
                    if (currentListObj == null)
                    {
                        // Try to resolve the concrete list type from the managedReferenceFieldTypename first
                        Type listType = null;
                        if (!string.IsNullOrEmpty(rootProperty.managedReferenceFieldTypename))
                        {
                            var parts = rootProperty.managedReferenceFieldTypename.Split(' ');
                            foreach (var part in parts)
                            {
                                var lt = Type.GetType(part);
                                if (lt != null) { listType = lt; break; }
                            }
                        }

                        // Fallback to System.Collections.Generic.List<BaseType>
                        if (listType == null)
                        {
                            try
                            {
                                listType = typeof(System.Collections.Generic.List<>).MakeGenericType(BaseType);
                            }
                            catch
                            {
                                listType = null;
                            }
                        }

                        if (listType == null)
                        {
                            // Could not resolve a concrete list type; abort add.
                            return;
                        }

                        var newList = Activator.CreateInstance(listType);
                        rootProperty.managedReferenceValue = newList;
                        rootProperty.serializedObject.ApplyModifiedProperties();

                        // Refresh cached serialized array property and runtime itemsSource after mutation
                        arrayProperty = ResolveArraySerializedProperty(rootProperty);

                        currentListObj = rootProperty.managedReferenceValue as IPolymorphicListView;
                        if (currentListObj == null)
                        {
                            rawList = rootProperty.managedReferenceValue as System.Collections.IList;
                            if (rawList != null)
                            {
                                itemsSource = rawList;
                                listView.itemsSource = itemsSource;
                            }
                            else
                            {
                                // unknown list shape; abort
                                return;
                            }
                        }
                        else
                        {
                            itemsSource = currentListObj.GetList();
                            listView.itemsSource = itemsSource;
                        }
                    }
                    else
                    {
                        // existing polymorphic list
                        itemsSource = currentListObj.GetList();
                        listView.itemsSource = itemsSource;
                    }

                    // Determine actual IList to add into:
                    System.Collections.IList targetIList = null;
                    if (rootProperty.managedReferenceValue is IPolymorphicListView ilv)
                        targetIList = ilv.GetList();
                    else if (rootProperty.managedReferenceValue is System.Collections.IList raw)
                        targetIList = raw;
                    else if (rawList != null)
                        targetIList = rawList;
                    else
                    {
                        // Unexpected; abort
                        return;
                    }

                    // Create and add the new element instance
                    var elementInstance = Activator.CreateInstance(t);
                    targetIList.Add(elementInstance);

                    rootProperty.serializedObject.ApplyModifiedProperties();

                    // Ensure ListView uses the updated itemsSource and rebuild UI
                    // Refresh serialized array property after mutation (Unity may create array entries)
                    arrayProperty = ResolveArraySerializedProperty(rootProperty);
                    itemsSource = targetIList;
                    listView.itemsSource = itemsSource;
                    listView.Rebuild();
                });
            }
            void OnRemove(BaseListView baselistView)
            {
                int sel = listView.selectedIndex;
                if (sel < 0) return;

                // Determine actual IList to remove from
                System.Collections.IList targetIList = null;
                if (rootProperty.managedReferenceValue is IPolymorphicListView ilv)
                    targetIList = ilv.GetList();
                else if (rootProperty.managedReferenceValue is System.Collections.IList raw)
                    targetIList = raw;
                else
                {
                    // nothing to remove from
                    return;
                }

                if (sel >= 0 && sel < targetIList.Count)
                {
                    targetIList.RemoveAt(sel);
                    rootProperty.serializedObject.ApplyModifiedProperties();

                    // Refresh serialized array property and itemsSource after mutation
                    arrayProperty = ResolveArraySerializedProperty(rootProperty);
                    itemsSource = targetIList;
                    listView.itemsSource = itemsSource;
                    listView.Rebuild();
                }
            }


            private static SerializedProperty ResolveArraySerializedProperty(SerializedProperty root)
            {
                if (root == null) return null;

                // If the root itself is an array, return it
                if (root.isArray) return root;

                var so = root.serializedObject;
                if (so == null) return null;

                string targetPath = root.propertyPath + ".Array";

                var it = so.GetIterator();
                // Move to first property
                if (!it.Next(true)) return null;

                do
                {
                    if (it.propertyPath == targetPath)
                    {
                        return it.Copy();
                    }
                } while (it.NextVisible(true));

                return null;
            }

            private static Type GetListElementType(SerializedProperty property)
            {
                if (property == null) return null;

                // Helper to get T from IEnumerable<T> if available
                static Type GetIEnumerableElementType(Type type)
                {
                    if (type == null) return null;

                    // If the type itself is generic IEnumerable<T>
                    if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(System.Collections.Generic.IEnumerable<>))
                        return type.GetGenericArguments()[0];

                    // Otherwise check implemented interfaces for IEnumerable<T>
                    var iEnum = type.GetInterfaces()
                        .FirstOrDefault(iFace => iFace.IsGenericType && iFace.GetGenericTypeDefinition() == typeof(System.Collections.Generic.IEnumerable<>));
                    if (iEnum != null) return iEnum.GetGenericArguments()[0];

                    return null;
                }

                // Try to use the managedReferenceFieldTypename if present (works for managed references)
                if (!string.IsNullOrEmpty(property.managedReferenceFieldTypename))
                {
                    var parts = property.managedReferenceFieldTypename.Split(' ');
                    foreach (var part in parts)
                    {
                        var t = Type.GetType(part);
                        if (t != null)
                        {
                            // If this is an array, return its element type
                            if (t.IsArray) return t.GetElementType();

                            // If this is a generic List<>, return its generic argument
                            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(System.Collections.Generic.List<>))
                                return t.GetGenericArguments()[0];

                            // If it implements IEnumerable<T>, return that T
                            var ielem = GetIEnumerableElementType(t);
                            if (ielem != null) return ielem;

                            // Fallback: return the resolved type itself
                            return t;
                        }
                    }
                }

                object target = property.serializedObject?.targetObject;
                if (target == null) return null;

                Type currentType = target.GetType();
                string path = property.propertyPath;
                string[] tokens = path.Split('.');

                for (int i = 0; i < tokens.Length; i++)
                {
                    string token = tokens[i];
                    if (token == "Array") continue;
                    if (token.StartsWith("data["))
                    {
                        // The previous field was a collection; get its element type.
                        if (currentType.IsArray)
                        {
                            currentType = currentType.GetElementType() ?? currentType;
                        }
                        else if (currentType.IsGenericType)
                        {
                            var genDef = currentType.GetGenericTypeDefinition();
                            if (genDef == typeof(System.Collections.Generic.List<>))
                            {
                                currentType = currentType.GetGenericArguments()[0];
                            }
                            else
                            {
                                // Try to get IEnumerable<T> element type for other generic collections
                                var ielem = GetIEnumerableElementType(currentType);
                                if (ielem != null) currentType = ielem;
                                else return null;
                            }
                        }
                        else
                        {
                            // Unknown collection shape; cannot resolve element type.
                            return null;
                        }
                        continue;
                    }

                    FieldInfo field = GetFieldInfoRecursive(currentType, token);
                    static FieldInfo GetFieldInfoRecursive(Type type, string fieldName)
                    {
                        while (type != null)
                        {
                            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                            var fi = type.GetField(fieldName, flags);
                            if (fi != null) return fi;
                            string backing = $"<{fieldName}>k__BackingField";
                            fi = type.GetField(backing, flags);
                            if (fi != null) return fi;
                            type = type.BaseType;
                        }
                        return null;
                    }

                    if (field == null) return null;
                    currentType = field.FieldType;
                }

                // If final resolved type is a collection, return its element type
                if (currentType.IsArray) return currentType.GetElementType() ?? currentType;
                if (currentType.IsGenericType)
                {
                    if (currentType.GetGenericTypeDefinition() == typeof(System.Collections.Generic.List<>))
                        return currentType.GetGenericArguments()[0];

                    var ielem = GetIEnumerableElementType(currentType);
                    if (ielem != null) return ielem;
                }

                return currentType;
            }

        }
#endif
    }
}