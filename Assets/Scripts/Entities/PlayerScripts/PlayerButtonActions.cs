using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.StateMachineH;
using UltEvents;
using UnityEngine.UIElements;


#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UIElements;
#endif

public class PlayerButtonActions : PlayerStateBehavior
{
    
    [SerializeReference] public PlayerButtonAction Jump;
    [SerializeReference] public PlayerButtonAction Attack;
    [SerializeReference] public PlayerButtonAction Grab;
    [SerializeReference] public PlayerButtonAction Charge;
    [SerializeReference] public PlayerButtonAction Aim;
    [SerializeReference] public PlayerButtonAction Parry;

#if UNITY_EDITOR

    [CustomEditor(typeof(PlayerButtonActions)), CanEditMultipleObjects]
    public class PlayerButtonActionsEditor : Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            // Build a TabView-based inspector (same behavior as original)
            var tabView = new TabView();
            tabView.reorderable = false;

            tabView.Add(CreateTab("Jump"));
            tabView.Add(CreateTab("Attack"));
            tabView.Add(CreateTab("Grab"));
            tabView.Add(CreateTab("Charge"));
            tabView.Add(CreateTab("Aim"));
            tabView.Add(CreateTab("Parry"));

            return tabView;
        }

        private VisualElement CreateTab(string title)
        {
            var tab = new Tab(title);

            var prop = serializedObject.FindProperty(title);
            if (prop != null)
            {
                var drawer = new PolymorphicObject.Drawer();
                var field = drawer.CreatePropertyGUI(prop);
                tab.contentContainer.Add(field);
            }
            else
            {
                // Provide a visible hint so it's obvious in the inspector why nothing appears.
                tab.contentContainer.Add(new Label($"Property '{title}' not found on {serializedObject.targetObject.GetType().Name}."));
            }

            return tab;
        }
    }
#endif
}