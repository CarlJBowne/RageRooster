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


}