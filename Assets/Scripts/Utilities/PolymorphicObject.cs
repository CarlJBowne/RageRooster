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

public class PolymorphicObject
{
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(PolymorphicObject), true)]
    public class Drawer : PropertyDrawer
    {
        public Drawer(SerializedProperty property, out VisualElement V)
        {
            this.property = property;
            BaseType = GetDeclaredFieldType() ?? typeof(PolymorphicObject);
            V = DrawGUI();
        }
        //Built-in initialization for default Editor Window.
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            this.property = property;
            BaseType = GetDeclaredFieldType() ?? typeof(PolymorphicObject);
            return DrawGUI();
        }

        public SerializedProperty property { get; private set; }
        public VisualElement result { get; private set; }
        public FoldoutPlus foldout { get; private set; }
        public VisualElement body { get; private set; }
        public Button TypeButton { get; private set; }
        public Type BaseType { get; private set; }
        public Action<Type> OnTypeChanged;
        public bool drawnSuccessfully { get; private set; } = false;


        public VisualElement DrawGUI(bool forceRedo = false)
        {
            if (result != null && !forceRedo) return result;

            result = new();

            foldout = new();
            result.Add(foldout);

            foldout.text = property.displayName;

            // Use the SerializedProperty's isExpanded to persist foldout state across UI rebuilds
            foldout.value = property.isExpanded;

            // keep property.isExpanded in sync when user toggles the foldout
            foldout.RegisterValueChangedCallback(evt =>
            {
                property.isExpanded = evt.newValue;
                property.serializedObject.ApplyModifiedProperties();
            });

            body = new();
            body.style.marginLeft = 12;
            body.style.marginTop = 2;
            body.style.marginBottom = 2;
            body.style.flexDirection = FlexDirection.Column;

            foldout.contentContainer.Add(body);
            UpdateObjectBody();

            TypeButton = new Button(ChooseAndSetType);
            foldout.headerSide.Add(TypeButton);
            UpdateTypeDisplayName();

            drawnSuccessfully = true;
            return result;
        }

        public void UpdateObjectBody()
        {
            body.Clear();
            if (property.managedReferenceValue != null)
            {
                if (property.managedReferenceValue is ICustomDrawer I) body.Add(I.Draw(property));
                else property.IterateAndDraw(body);
            }
            OnTypeChanged?.Invoke(property.managedReferenceValue?.GetType());
        }

        // Resolve the declared type of the serialized field represented by 'property'.
        private Type GetDeclaredFieldType()
        {
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

        public void ChooseAndSetType() => ShowChooseTypeMenu(BaseType, property.managedReferenceValue != null, SetNewType);

        private void SetNewType(Type t)
        {
            // preserve current expanded state
            bool wasExpanded = property.isExpanded;

            property.managedReferenceValue = t == null ? null : Activator.CreateInstance(t);

            // restore expanded state so foldout stays open if it was
            property.isExpanded = wasExpanded;

            property.serializedObject.ApplyModifiedProperties();
            UpdateTypeDisplayName();

            // Ensure UI foldout reflects the serialized flag (in case the element tree was recreated)
            foldout.value = property.isExpanded;

            UpdateObjectBody();
        }

        void UpdateTypeDisplayName()
        {
            TypeButton.text = property.managedReferenceValue != null
                ? property.managedReferenceValue.GetType().Name
                : "Choose Type";
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
        public List<Tab> tabs { get; private set; }

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

                bodyDrawer = new PolymorphicObject.Drawer(property, out VisualElement V);
                contentContainer.Add(V);

                UpdateLiteralObject(property.managedReferenceValue?.GetType());
                bodyDrawer.OnTypeChanged += UpdateLiteralObject;
            }

            public string displayName { get; private set; }
            public SerializedProperty property { get; private set; }
            public PolymorphicObject.Drawer bodyDrawer { get; private set; }


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

    public interface ICustomDrawer
    {
        public VisualElement Draw(SerializedProperty prop);
    }
}