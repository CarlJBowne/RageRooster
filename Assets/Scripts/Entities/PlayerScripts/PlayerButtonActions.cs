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
            tabView.GetDescendent(0, 0).style.flexGrow = 1f;

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