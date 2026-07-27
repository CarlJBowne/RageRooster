using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;
using SLS.Singletons;
using Button = UnityEngine.InputSystem.InputAction;
using Ref = UnityEngine.InputSystem.InputActionReference;

public class Input : GlobalAsset<Input>
{
    [System.Serializable]
    public class ReferencesClass
    {
        public Ref Movement;
        public Ref Camera;
        public Ref Jump;
        public Ref Attack;
        public Ref Parry;
        public Ref Interact;
        public Ref Aim;
        public Ref Grab;
        public Ref Charge1;
        public Ref Charge2;
        public Ref Pause;
        public Ref UI_Confirm;
        public Ref UI_Cancel;
        public Ref Debug_GodMode;
        public Ref Debug_ToggleTextOverlay;
    }
    public ReferencesClass References;


    [FormerlySerializedAs("Asset")] public InputActionAsset RootAsset;

    public static Vector2 Movement => MovementAction.ReadValue<Vector2>();
    public static Vector2 Camera => CameraAction.ReadValue<Vector2>();
    public static Button MovementAction { get; internal set; }
    public static Button CameraAction { get; internal set; }
    public static Button Jump { get; internal set; }
    public static Button Attack { get; internal set; }
    public static Button Parry { get; internal set; }
    public static Button Grab { get; internal set; }
    public static Button Aim { get; internal set; }
    public static Button Charge1 { get; internal set; }
    public static Button Charge2 { get; internal set; }
    public static Button Interact { get; internal set; }
    public static Button Pause { get; internal set; }

    public static class UI
    {
        public static Button Confirm { get; internal set; }
        public static Button Cancel { get; internal set; }
    }
    public static class Debug
    {
        public static Button GodMode { get; internal set; }
        public static Button ToggleTextOverlay { get; internal set; }
    }

    public override void OnInit()
    {
        Enable();
        //Enable Debug Action Map only if in dev build or editor
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        RootAsset.FindActionMap("Debug").Enable();
#else
		Asset.FindActionMap("Debug").Disable();
#endif

        MovementAction = References.Movement.action;
        CameraAction = References.Camera.action;
        Jump = References.Jump.action;
        Attack = References.Attack.action;
        Parry = References.Parry.action;
        Grab = References.Grab.action;
        Aim = References.Aim.action;
        Charge1 = References.Charge1.action;
        Charge2 = References.Charge2.action;
        Interact = References.Interact.action;
        Pause = References.Pause.action;
        UI.Confirm = References.UI_Confirm.action;
        UI.Cancel = References.UI_Cancel.action;
        Debug.GodMode = References.Debug_GodMode.action;
        Debug.ToggleTextOverlay = References.Debug_ToggleTextOverlay.action;

    }

    public static void Enable() => Get.RootAsset.Enable();
    public static void Disable() => Get.RootAsset.Disable();
}
