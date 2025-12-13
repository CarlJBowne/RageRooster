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
    public PlayerButtonAction Jump;
    public PlayerButtonAction Attack;
    public PlayerButtonAction Grab;
    public PlayerButtonAction Charge;
    public PlayerButtonAction Aim;
    public PlayerButtonAction Parry;

#if UNITY_EDITOR
    [CustomEditor(typeof(PlayerButtonActions))]
    public class Editor : UnityEditor.Editor
    {
        public override VisualElement CreateInspectorGUI()
        {
            return base.CreateInspectorGUI();
        }
    }
#endif
}