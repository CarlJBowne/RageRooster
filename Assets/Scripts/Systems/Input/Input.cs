using SLS.ISingleton;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Button = UnityEngine.InputSystem.InputAction;
using Ref = UnityEngine.InputSystem.InputActionReference;

public class Input : SingletonAsset<Input>
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


    protected override void OnInitialize()
	{
        Asset.Enable();
	}
}