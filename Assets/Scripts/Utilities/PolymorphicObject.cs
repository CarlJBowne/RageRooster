using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;

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

    public class List<T> : object, IPolymorphicListView where T : PolymorphicObject
    {
        [SerializeReference]
        public System.Collections.Generic.List<T> RealList = new();

        #region List-like functionality
        // Generic-list-like API
        public void Add(T item) => RealList.Add(item);
        public bool Remove(T item) => RealList.Remove(item);
        public int IndexOf(T item) => RealList.IndexOf(item);
        public void Insert(int index, T item) => RealList.Insert(index, item);
        public bool Contains(T item) => RealList.Contains(item);
        public void CopyTo(T[] array, int arrayIndex) => RealList.CopyTo(array, arrayIndex);
        public IEnumerator<T> GetEnumerator() => RealList.GetEnumerator();

        // Typed indexer for convenience (IList<T>)
        public T this[int index]
        {
            get => RealList[index];
            set => RealList[index] = value ?? throw new ArgumentNullException(nameof(value));
        }

        // Non-generic IList implementation (explicit) - preserves original behavior for inspectors etc.
        object System.Collections.IList.this[int index]
        {
            get => RealList[index];
            set
            {
                if (value is T t)
                {
                    RealList[index] = t;
                }
                else
                {
                    throw new ArgumentException($"Value must be of type {typeof(T)}");
                }
            }
        }

        // Original non-generic IList members preserved/adapted
        public int Add(object value)
        {
            if (value is T t)
            {
                RealList.Add(t);
                return RealList.Count - 1;
            }
            throw new ArgumentException($"Value must be of type {typeof(T)}");
        }

        public void Clear() => RealList.Clear();

        public bool Contains(object value) => value is T t && RealList.Contains(t);

        public int IndexOf(object value) => value is T t ? RealList.IndexOf(t) : -1;

        public void Insert(int index, object value)
        {
            if (value is T t)
            {
                RealList.Insert(index, t);
            }
            else
            {
                throw new ArgumentException($"Value must be of type {typeof(T)}");
            }
        }

        public void Remove(object value)
        {
            if (value is T t)
            {
                RealList.Remove(t);
            }
        }

        public void RemoveAt(int index)
        {
            RealList.RemoveAt(index);
        }

        public bool IsFixedSize => false;

        public bool IsReadOnly => false;

        public void CopyTo(Array array, int index)
        {
            ((ICollection)RealList).CopyTo(array, index);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public int Count => RealList.Count;

        public bool IsSynchronized => ((ICollection)RealList).IsSynchronized;

        public object SyncRoot => ((ICollection)RealList).SyncRoot;

        #endregion

    }
    private interface IPolymorphicListView : System.Collections.IList
    {

#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(IPolymorphicListView), true)]
        public class PolymorphicListDrawer : LimitedListDrawer
        {
            Type BaseType;

            public override VisualElement CreatePropertyGUI(SerializedProperty property)
            {
                BaseType = GetListElementType(property) ?? typeof(PolymorphicObject);
                return base.CreatePropertyGUI(property.FindPropertyRelative("RealList"));
            }

            protected override void BindListItem(VisualElement element, int index, IList list)
            {
                element.Clear();
                SerializedProperty itemProp = rootProperty.GetArrayElementAtIndex(index);
                if (itemProp != null)
                    element.Add((itemProp.managedReferenceValue as PolymorphicObject).BodyDrawer(itemProp));
            }

            protected override void OnAdd(IList list)
            {
                PolymorphicObject.ShowChooseTypeMenu(BaseType, false, (Type t) =>
                {
                    if (t == null) return;

                    var newProp = rootProperty.AddArrayElement();
                    newProp.managedReferenceValue = Activator.CreateInstance(t);

                    // Refresh the ListView to show the new item
                    listView.RefreshItems();
                    rootProperty.serializedObject.ApplyModifiedProperties();
                });
            }
            protected override void OnRemove(IList list, int index)
            {
                list.RemoveAt(index);

                listView.RefreshItems();
                rootProperty.serializedObject.ApplyModifiedProperties();
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
                            // If this is PolymorphicObject.List<T>, return T
                            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(PolymorphicObject.List<>))
                                return t.GetGenericArguments()[0];

                            // If this is an array, return its element type
                            if (t.IsArray) return t.GetElementType();

                            // If this is a generic List<>, return its generic argument
                            if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(System.Collections.Generic.List<>))
                                return t.GetGenericArguments()[0];

                            // If the type has a RealList field of List<U>, use that U
                            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                            var realListField = t.GetField("RealList", flags);
                            if (realListField != null)
                            {
                                var ft = realListField.FieldType;
                                if (ft.IsGenericType && ft.GetGenericTypeDefinition() == typeof(System.Collections.Generic.List<>))
                                    return ft.GetGenericArguments()[0];
                            }

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

                // If final resolved type is a PolymorphicObject.List<T>, return its generic argument
                if (currentType.IsGenericType && currentType.GetGenericTypeDefinition() == typeof(PolymorphicObject.List<>))
                    return currentType.GetGenericArguments()[0];

                // If the resolved type has a RealList field of List<U>, return U
                {
                    var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
                    var realListField = currentType.GetField("RealList", flags);
                    if (realListField != null)
                    {
                        var ft = realListField.FieldType;
                        if (ft.IsGenericType && ft.GetGenericTypeDefinition() == typeof(System.Collections.Generic.List<>))
                            return ft.GetGenericArguments()[0];
                    }
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