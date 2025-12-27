using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.StateMachineH;
using UltEvents;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;



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

    public PlayerButtonAction GetButtonAction(InputAction button)
    {
        return button == Input.Jump ? Jump
            : button == Input.AttackTap ? Attack
            : button == Input.Grab ? Grab
            : button == Input.Charge1 || button == Input.Charge2 ? Charge 
            : button == Input.Aim ? Aim 
            : button == Input.Parry ? Parry 
            : null;
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

    protected override void OnEnter(State prev, bool isFinal) => PlayerController.RegisterActionSource(this);
    protected override void OnExit(State next)
    {
        PlayerController.RegisterActionSource(this, true);
        if (!Jump.persistAcrossStateChange) Jump.Finish();
        if (!Attack.persistAcrossStateChange) Jump.Finish();
        if (!Grab.persistAcrossStateChange) Jump.Finish();
        if (!Charge.persistAcrossStateChange) Jump.Finish();
        if (!Aim.persistAcrossStateChange) Jump.Finish();
        if (!Parry.persistAcrossStateChange) Jump.Finish();
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