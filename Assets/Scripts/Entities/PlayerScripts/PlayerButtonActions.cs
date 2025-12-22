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
        PolymorphicObject.TabbedDrawer drawer;

        public override VisualElement CreateInspectorGUI()
        {
            drawer = new(serializedObject);

            drawer.CreateTab(nameof(Jump), serializedObject.FindProperty(nameof(Jump)));
            drawer.CreateTab(nameof(Attack), serializedObject.FindProperty(nameof(Attack)));
            drawer.CreateTab(nameof(Grab), serializedObject.FindProperty(nameof(Grab)));
            drawer.CreateTab(nameof(Charge), serializedObject.FindProperty(nameof(Charge)));
            drawer.CreateTab(nameof(Aim), serializedObject.FindProperty(nameof(Aim)));
            drawer.CreateTab(nameof(Parry), serializedObject.FindProperty(nameof(Parry)));

            return drawer.tabView; 
        }

        private VisualElement CreateTab(string title)
        {
            var tab = new Tab(title);
            tab.tabHeader.style.paddingLeft = 5;
            tab.tabHeader.style.paddingRight = 5;
            tab.tabHeader.style.flexGrow = 1f;
            tab.tabHeader.style.justifyContent = Justify.Center;

            PlayerButtonAction.Drawer drawer = new(serializedObject.FindProperty(title), out VisualElement V);

            tab.contentContainer.Add(V);
            drawer.OnTypeChanged += (newType) =>
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(target);
            };

            return tab;
        }
    }
#endif
}