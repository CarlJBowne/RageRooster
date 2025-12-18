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
                // Add concrete instance fields directly (no nested foldout)
                if (property.managedReferenceValue is ICustomDrawer I) body.Add(I.Draw());
                else property.IterateAndDraw(body);
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

    public interface ICustomDrawer
    {
        public VisualElement Draw();
    }
}