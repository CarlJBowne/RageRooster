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

[System.Serializable]
public abstract class PolymorphicObject
{
#if UNITY_EDITOR

    public virtual bool OverrideBody(VisualElement.Hierarchy container, SerializedProperty property) => false;


    [CustomPropertyDrawer(typeof(PolymorphicObject), true)]
    public class Drawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property) => new HeaderDrawer(property);
    }

    //Note: Consider making a "No Choosing Header" option, but that's more or less useless so maybe ignore this.


    public class HeaderDrawer : VisualElement
    {
        public HeaderDrawer(SerializedProperty p) : base()
        {
            property = p;
            BaseType = GetDeclaredFieldType() ?? typeof(PolymorphicObject);
            CurrentType = property?.managedReferenceValue?.GetType();
            name = $"HeaderDrawer-{BaseType.Name}-{property.name}";

            propertyField ??= new PropertyField(p)
            {
                name = $"HeaderDrawer-PropertyField__{p.name}"
            };
            if (!this.Contains(propertyField)) this.Add(propertyField);
            typeButton ??= new Button(TypeButtonClick)
            {
                name = "Type Chooser",
                text = "*",
                style =
                        {
                            alignSelf = Align.FlexEnd,
                            flexDirection = FlexDirection.Row,
                            position = Position.Absolute,
                            width = 16,
                            height = 16,
                            fontSize = 18,
                            flexGrow = 1,
                            paddingTop = 3,
                            paddingBottom = 0,
                            paddingLeft = 0,
                            paddingRight = 0,
                            right = -1,
                            top = 0
                        }
            };
            if (!this.Contains(typeButton)) this.Add(typeButton);
            if (TryCacheFoldout()) foldout.value = true;

            // Schedule Delayed building of the Layout.
            this.DelayedBuild(Update);
        }

        void Update()
        {

            // Update label and toggle UI. Create the TypeButton once and only add it to the labelElement if not already present.
            if (this.QCache(out label, className: "unity-label"))
            {
                label.text = CorrectLabel;

                label.style.right = 0;
                label.style.flexGrow = 1;
                label.style.height = EditorGUIUtility.singleLineHeight;
            }

            TryCacheFoldout();
            this.QCache(out contentContainer, "unity-content");

            //Handle other hasInstance specific pieces.
            if (this.QCache(out toggle, className: "unity-foldout__checkmark"))
            {
                toggle.style.marginRight = 1;
                if (CurrentType == null) toggle.value = false;
            }

            if (this.QCache(out toggleArrow, "unity-checkmark")) toggleArrow.visible = CurrentType != null;

            if (property.managedReferenceValue is not null and PolymorphicObject O && bodyInvalid)
            {
                if (O.OverrideBody(contentContainer.hierarchy, property))
                    contentContainer.Bind(property.serializedObject);

                bodyInvalid = false;
            }
        }

        void UpdateType(Type t) => UpdateType(t, false);
        void UpdateType(Type t, bool forceRebuild = false)
        {
            if (property == null || (t == CurrentType && !forceRebuild)) return;

            bool wasPreviouslyNull = CurrentType == null && t != null;
            if (CurrentType != t)
            {
                if (t != null) property.managedReferenceValue = Activator.CreateInstance(t);
                else property.managedReferenceValue = null;
            }

            CurrentType = t;
            //bodyInvalidated = true;

            // Re-bind the hidden anchor (the only bound element) to ensure prefab behavior remains correct.
            //try { overrideAnchor?.Bind(property.serializedObject); } catch { /* defensive */ }

            if (foldout != null || TryCacheFoldout()) foldout.value = true;

            // Apply the modification so the SerializedProperty reflects the new instance/type.
            property.serializedObject.ApplyModifiedProperties();

            bodyInvalid = true;

            // Rebuild the visible parts of the HeaderDrawer.
            if (!wasPreviouslyNull) Update();
            else propertyField.DelayedBuild(Update);

            if (foldout != null || TryCacheFoldout()) foldout.value = true;

            // Notify listeners of the type change.
            OnTypeChanged?.Invoke(property?.managedReferenceValue?.GetType());
        }

        //Pieces
        PropertyField propertyField;
        Button typeButton;
        Toggle toggle;
        Foldout foldout;
        Label label;
        new VisualElement contentContainer;
        VisualElement toggleArrow;


        //Data
        SerializedProperty property;
        Type BaseType;
        Type CurrentType;
        bool bodyInvalid = true;


        public Action<Type> OnTypeChanged;
        public bool drawnSuccessfully { get; private set; } = false;

        //VisualElement bodyDrawer;
        //bool bodyInvalidated = true;

        // Hidden bound anchor used to preserve prefab Apply/Revert behavior even when value is null.
        //private PropertyField overrideAnchor;
        private string NAME => name;

        Type GetDeclaredFieldType()
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

        static FieldInfo GetFieldInfoRecursive(Type type, string fieldName)
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

        string CorrectLabel => CurrentType != null ? $"{property.displayName} ({CurrentType.Name})" : property.displayName;

        void TypeButtonClick() => ShowChooseTypeMenu(BaseType, CurrentType != null, UpdateType);

        bool TryCacheFoldout() => this.QCache(out foldout, className: "unity-foldout");

        //PropertyField OverrideAnchor()
        //{
        //    // Ensure anchor still exists and is bound (insulates against inspector re-creation).
        //    if (overrideAnchor == null && property != null)
        //    {
        //        overrideAnchor = new PropertyField(property);
        //        overrideAnchor.name = "headerDrawer_overrideAnchor";
        //        overrideAnchor.style.display = DisplayStyle.None;
        //        this.hierarchy.Add(overrideAnchor);
        //        try { overrideAnchor.Bind(property.serializedObject); } catch { /* ignore */ }
        //    }
        //    return overrideAnchor;
        //}
    }
    public class TabbedDrawer : VisualElement
    {
        public TabbedDrawer() : base()
        {
            name = "TabbedDrawer";
            tabView = new TabView();
            this.Add(tabView);
            tabView.Q<VisualElement>("unity-tab-view__header-container").style.flexGrow = 1;
            tabs = new();
            //this.DelayedBuild(() =>
            //{
            //    for (int i = 0; i < tabs.Count; i++)
            //    {
            //        tabView.Add(tabs[i]);
            //    }
            //});
        }

        TabView tabView;
        List<Tab> tabs;

        public void Add(string displayName, SerializedProperty prop)
        {
            Tab newTab = new(displayName, prop);
            tabView.Add(newTab);
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

                //contentContainer.Add(new Label($"Content for {displayName}")); //(Debug thing.)

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



    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public sealed class ListAttribute : PropertyAttribute
    {
        public Type elementType { get; }

        public ListAttribute(Type elementType) => this.elementType = elementType ?? throw new ArgumentNullException(nameof(elementType));

#if UNITY_EDITOR
        [CustomPropertyDrawer(typeof(ListAttribute), true)]
        public class PolymorphicListAttributeDrawer : PropertyDrawer
        {
            public override VisualElement CreatePropertyGUI(SerializedProperty property)
            {
                var attr = attribute as ListAttribute;
                Type elemType = attr?.elementType ?? typeof(PolymorphicObject);

                if (property == null)
                    return new Label("Null property");

                // Determine whether the property is an array/list as Unity understands it.
                bool isArrayProperty = property.isArray;

                // If Unity didn't report it as an array, try to detect via the reflected fieldInfo.
                Type reflectedElementType = null;
                if (!isArrayProperty && fieldInfo != null)
                {
                    Type fieldType = fieldInfo.FieldType;
                    if (fieldType.IsArray)
                    {
                        reflectedElementType = fieldType.GetElementType();
                        isArrayProperty = true;
                    }
                    else if (fieldType.IsGenericType)
                    {
                        var genDef = fieldType.GetGenericTypeDefinition();
                        if (genDef == typeof(List<>))
                        {
                            reflectedElementType = fieldType.GetGenericArguments()[0];
                            isArrayProperty = true;
                        }
                    }
                }

                if (!isArrayProperty)
                    return new Label("PolymorphicListAttribute must be applied to a List/Array field");

                // If a reflected element type was found, prefer it over the attribute-provided one.
                if (reflectedElementType != null)
                    elemType = reflectedElementType;

                try
                {
                    // Construct SuperList<elemType> via reflection and pass the SerializedProperty.
                    Type superListGeneric = typeof(SuperList<>).MakeGenericType(elemType);

                    // The SuperList ctor signature expected: (SerializedProperty listProperty, PropertyToVisualElementDelegate drawElementBody = null)
                    object instance = Activator.CreateInstance(superListGeneric, new object[] { property, null });

                    if (instance is VisualElement ve) return ve;
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                }

                return new Label("Failed to create SuperList for element type: " + (elemType?.Name ?? "null"));
            }
        }
#endif
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
            menu.AddItem(new GUIContent("Add"), false, () => { result?.Invoke(baseType); });
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


}