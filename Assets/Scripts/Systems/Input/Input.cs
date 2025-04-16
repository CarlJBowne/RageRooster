using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Button = UnityEngine.InputSystem.InputAction;
using Ref = UnityEngine.InputSystem.InputActionReference;

public class Input : SingletonScriptable<Input>
{

	[SerializeField] public InputActionAsset Asset;
    [SerializeField] Ref _Movement;
	[SerializeField] Ref _Camera;
	[SerializeField] Ref _Jump;
	[SerializeField] Ref _AttackTap;
	[SerializeField] Ref _AttackHold;
	[SerializeField] Ref _Parry;
	[SerializeField] Ref _Interact;
	[SerializeField] Ref _Aim;
	[SerializeField] Ref _Grab;
	[SerializeField] Ref _Charge1;
	[SerializeField] Ref _Charge2;
	[SerializeField] Ref _Pause;


    public static Vector2 Movement => Get()._Movement.action.ReadValue<Vector2>();
    public static Vector2 Camera => Get()._Camera.action.ReadValue<Vector2>();
	public static Button MovementAction => Get()._Movement;
	public static Button CameraAction => Get()._Camera;
	public static Button Jump => Get()._Jump;
	public static Button AttackTap => Get()._AttackTap;
	public static Button AttackHold => Get()._AttackHold;
	public static Button Parry => Get()._Parry;
	public static Button Grab => Get()._Grab;
	public static Button Aim => Get()._Aim;
	public static Button Charge1 => Get()._Charge1;
	public static Button Charge2 => Get()._Charge2;
	public static Button Interact => Get()._Interact;
	public static Button Pause => Get()._Pause;


    public PlayerActions asset;

    protected override void OnAwake()
	{
		Asset.Enable();
	}

    //public override void Awake()
	//{
	//	asset = new();
	//	asset.Enable();
	//}

    //private void ResetAsset_()
	//{
    //    asset.Disable();
    //    asset = new();
    //    asset.Enable();
    //}
    //public static void ResetAsset() => Get().ResetAsset_();

    //public static void Enable() => Get().asset.Enable();
	//public static void Disable() => Get().asset.Disable();

    //public static Vector2 Movement => Get().asset.Gameplay.Movement.ReadValue<Vector2>();
	//public static Vector2 Camera => Get().asset.Gameplay.Camera.ReadValue<Vector2>();
	//public static Button Jump => Get().asset.Gameplay.Jump;
	//public static Button AttackTap => Get().asset.Gameplay.AttackTap;
	//public static Button AttackHold => Get().asset.Gameplay.AttackHold;
	//public static Button Parry => Get().asset.Gameplay.Parry;
	//public static Button GrabTap => Get().asset.Gameplay.GrabTap;
	//public static Button ShootMode => Get().asset.Gameplay.ShootMode;
	//public static Button ChargeTap => Get().asset.Gameplay.ChargeTap;
	//public static Button Interact => Get().asset.Gameplay.Interact;

	//public Vector2 movement => asset.Gameplay.Movement.ReadValue<Vector2>();
	//public Vector2 camera => asset.Gameplay.Camera.ReadValue<Vector2>();
	//public Button jump => asset.Gameplay.Jump;
	//public Button attackTap => asset.Gameplay.AttackTap;
	//public Button attackHold => asset.Gameplay.AttackHold;
	//public Button parry => asset.Gameplay.Parry;
	//public Button grabTap => asset.Gameplay.GrabTap;
	//public Button shootMode => asset.Gameplay.ShootMode;
	//public Button chargeTap => asset.Gameplay.ChargeTap;
	//public Button interact => asset.Gameplay.Interact;
	//
	//public static class UI
	//{
	//	public static Button PauseGame => Get().asset.UI.PauseGame;
	//}
	
}