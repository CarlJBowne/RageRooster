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

    public PlayerButtonAction GetButtonAction(PlayerController.ButtonTypes button)
    {
        return button switch
        {
            PlayerController.ButtonTypes.Jump => Jump,
            PlayerController.ButtonTypes.Attack => Attack,
            PlayerController.ButtonTypes.Grab => Grab,
            PlayerController.ButtonTypes.Charge => Charge,
            PlayerController.ButtonTypes.Aim => Aim,
            PlayerController.ButtonTypes.Parry => Parry,
            _ => throw new ArgumentOutOfRangeException(nameof(button), button, null)
        };
    }
    public PlayerButtonAction[] All { get; private set; }

    protected override void OnAwake()
    {
        All = new PlayerButtonAction[]
        {
            Jump,
            Attack,
            Grab,
            Charge,
            Aim,
            Parry
        };
    }

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

            return drawer;
        }
    }
#endif
}