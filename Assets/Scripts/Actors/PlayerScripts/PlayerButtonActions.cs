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
    [SerializeReference] public PlayerButtonAction Parry;

    public PlayerButtonAction this[InputAction button] => 
              button == Input.Jump ? Jump
            : button == Input.Attack ? Attack
            : button == Input.Grab ? Grab
            : button == Input.Charge1 || button == Input.Charge2 ? Charge
            : button == Input.Parry ? Parry
            : null;
    public PlayerButtonAction[] All { get; private set; }

    protected override void OnAwake()
    {
        All = new PlayerButtonAction[]
        {
            Jump,
            Attack,
            Grab,
            Charge,
            Parry
        };
    }

    protected override void OnEnter(State prev, bool isFinal) => PlayerController.RegisterActionSource(this);
    protected override void OnExit(State next)
    {
        PlayerController.RegisterActionSource(this, true);
        if (Jump != null && Grab is not PlayerButtonAction.Base_ChooseType && !Jump.persistAcrossStateChange) Jump.Finish();
        if (Attack != null && Grab is not PlayerButtonAction.Base_ChooseType && !Attack.persistAcrossStateChange) Attack.Finish();
        if (Grab != null && Grab is not PlayerButtonAction.Base_ChooseType && !Grab.persistAcrossStateChange) Grab.Finish();
        if (Charge != null && Grab is not PlayerButtonAction.Base_ChooseType && !Charge.persistAcrossStateChange) Charge.Finish();
        if (Parry != null && Grab is not PlayerButtonAction.Base_ChooseType && !Parry.persistAcrossStateChange) Parry.Finish();
    }

#if UNITY_EDITOR 

    [CustomEditor(typeof(PlayerButtonActions)), CanEditMultipleObjects]
    public class PlayerButtonActionsEditor : Editor
    {
        Polymorph.TabbedDrawer drawer;

        public override VisualElement CreateInspectorGUI()
        {
            drawer = new();

            drawer.Add(nameof(Jump), serializedObject.FindProperty(nameof(Jump)));
            drawer.Add(nameof(Attack), serializedObject.FindProperty(nameof(Attack)));
            drawer.Add(nameof(Grab), serializedObject.FindProperty(nameof(Grab)));
            drawer.Add(nameof(Charge), serializedObject.FindProperty(nameof(Charge)));
            drawer.Add(nameof(Parry), serializedObject.FindProperty(nameof(Parry)));

            return drawer;
        }
    }
#endif
}