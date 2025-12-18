using System;
using System.Collections.Generic;
using System.Linq;
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
        private static readonly Dictionary<string, bool> foldoutStates = new();

        Label label;
        Button TypeButtonOld;
        VisualElement Header;

        public VisualElement CreatePropertyGUIX(SerializedProperty property)
        {
            var root = new VisualElement();
            //root.style.marginBottom = 4;

            // Create Keys
            string stateKey = $"{property.serializedObject.targetObject.GetInstanceID()}:{property.propertyPath}";
            if (!foldoutStates.ContainsKey(stateKey))
                foldoutStates[stateKey] = false;
            bool expanded = foldoutStates[stateKey];

            // Establish Base Type
            Type baseType = property.propertyPath.Contains("Array.data")
                    ? property.managedReferenceFieldTypename != null
                        ? Type.GetType(property.managedReferenceFieldTypename.Split(' ')[1])
                        : typeof(PolymorphicObject)
                    : property.managedReferenceValue?.GetType().BaseType
                       ?? typeof(PolymorphicObject);


            // Header (replaces the label of a traditional dropdown)
            Header = new VisualElement();
            Header.style.flexDirection = FlexDirection.Row;
            Header.style.alignItems = Align.Center;
            Header.style.paddingTop = 2;
            Header.style.paddingBottom = 2;
            //header.style.cursor = new StyleCursor(SystemCursor.Arrow);

            // Left: clickable area with the display name which toggles the body
            label = new Label(property.displayName);
            label.style.flexGrow = 1;
            label.style.unityTextAlign = TextAnchor.MiddleLeft;
            label.style.paddingLeft = 2;
            //leftToggle.style.cursor = new StyleCursor(SystemCursor.Link); // indicates clickable

            // Right: type chooser button (shows current type name)
            TypeButtonOld = new Button(() =>
            {
                // Show menu. The callback will be invoked with either a Type or null.
                ShowChooseTypeMenu(baseType ?? typeof(PolymorphicObject), property.managedReferenceValue != null, SetNewType);
            });
            UpdateTypeDisplayName();
            TypeButtonOld.style.marginLeft = 4;
            
            void SetNewType(Type t)
            {
                property.managedReferenceValue = t == null ? null : Activator.CreateInstance(t);
                property.serializedObject.ApplyModifiedProperties();
                UpdateTypeDisplayName();
            }
            void UpdateTypeDisplayName()
            {
                TypeButtonOld.text = property.managedReferenceValue != null
                    ? property.managedReferenceValue.GetType().Name
                    : "Choose Type";
            }

            Header.Add(label);
            Header.Add(TypeButtonOld);
            root.Add(Header);

            // Body container that will hold the property fields and is toggleable
            var body = new VisualElement();
            body.style.marginLeft = 12;
            body.style.marginTop = 2;
            body.style.marginBottom = 2;
            body.style.flexDirection = FlexDirection.Column;
            body.style.display = expanded ? DisplayStyle.Flex : DisplayStyle.None;

            // Only add the editable fields when there's an instance
            if (property.managedReferenceValue != null)
            {
                var bodyField = new PropertyField(property);
                bodyField.Bind(property.serializedObject);
                body.Add(bodyField);
            }

            root.Add(body);

            // Toggle behavior: clicking the left label toggles the body.
            // Ensure clicks targeted to the chooser button don't toggle.
            label.RegisterCallback<MouseDownEvent>(evt =>
            {
                // Toggle state
                foldoutStates[stateKey] = !foldoutStates[stateKey];
                body.style.display = foldoutStates[stateKey] ? DisplayStyle.Flex : DisplayStyle.None;
                evt.StopPropagation();
            });

            // Also allow clicking anywhere on the header except the chooser button to toggle.
            Header.RegisterCallback<MouseDownEvent>(evt =>
            {
                var targetVE = evt.target as VisualElement;
                if (targetVE != null)
                {
                    // If the click originated from the chooser button (or its descendants), ignore.
                    if (TypeButtonOld == targetVE || TypeButtonOld.Contains(targetVE))
                        return;

                    // Otherwise toggle (same as clicking the left label)
                    foldoutStates[stateKey] = !foldoutStates[stateKey];
                    body.style.display = foldoutStates[stateKey] ? DisplayStyle.Flex : DisplayStyle.None;
                    evt.StopPropagation();
                }
            });

            return root;
        }

        FoldoutPlus foldout;
        VisualElement body;
        Button TypeButton;

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            VisualElement root = new();

            // Establish Base Type
            Type baseType = property.propertyPath.Contains("Array.data")
                    ? property.managedReferenceFieldTypename != null
                        ? Type.GetType(property.managedReferenceFieldTypename.Split(' ')[1])
                        : typeof(PolymorphicObject)
                    : property.managedReferenceValue?.GetType().BaseType
                       ?? typeof(PolymorphicObject);

            foldout = new();
            foldout.text = property.displayName;
            foldout.value = false;

            body = new();
            body.style.marginLeft = 12;
            body.style.marginTop = 2;
            body.style.marginBottom = 2;
            body.style.flexDirection = FlexDirection.Column;

            if (property.managedReferenceValue != null)
            {
                var bodyField = new PropertyField(property);
                bodyField.Bind(property.serializedObject);
                body.Add(bodyField);
            }
            foldout.contentContainer.Add(body);


            TypeButton = new Button(() =>
            {
                ShowChooseTypeMenu(baseType ?? typeof(PolymorphicObject), property.managedReferenceValue != null, SetNewType);
            });
            foldout.headerSide.Add(TypeButton);
            UpdateTypeDisplayName();

            void SetNewType(Type t)
            {
                property.managedReferenceValue = t == null ? null : Activator.CreateInstance(t);
                property.serializedObject.ApplyModifiedProperties();
                UpdateTypeDisplayName();
            }
            void UpdateTypeDisplayName()
            {
                TypeButton.text = property.managedReferenceValue != null
                    ? property.managedReferenceValue.GetType().Name
                    : "Choose Type";
            }

            root.Add(foldout);

            Foldout controlFoldout = new() { text = "Controls" };
            controlFoldout.Add(new Button() { text = "TEST"});
            root.Add(controlFoldout);

            return root;
        }
    }


    public static Type[] GetSubtypes(Type baseType)
    {
        try
        {
            var types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .Where(t => baseType.IsAssignableFrom(t) && !t.IsAbstract && t != baseType)
                .ToArray();

            return types;
        }
        catch
        {
            return Array.Empty<Type>();
        }
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