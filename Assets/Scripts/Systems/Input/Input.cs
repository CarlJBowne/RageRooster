using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Button = UnityEngine.InputSystem.InputAction;

public class Input : Staticon<Input>
{
	public PlayerActions asset;



	public override void Awake()
	{
		asset = new();
		asset.Enable();
	}

	public static Vector2 Movement => Get().asset.Gameplay.Movement.ReadValue<Vector2>();
	public static Vector2 Camera => Get().asset.Gameplay.Camera.ReadValue<Vector2>();
	public static Button Jump => Get().asset.Gameplay.Jump;
	public static Button Attack => Get().asset.Gameplay.Attack;
	public static Button Parry => Get().asset.Gameplay.Parry;
	public static Button Grab => Get().asset.Gameplay.Grab;
	public static Button ShootMode => Get().asset.Gameplay.ShootMode;
	public static Button Shoot => Get().asset.Gameplay.Shoot;
	public static Button Charge => Get().asset.Gameplay.Charge;
	public static Button Interact => Get().asset.Gameplay.Interact;

	public Vector2 movement => asset.Gameplay.Movement.ReadValue<Vector2>();
	public Vector2 camera => asset.Gameplay.Camera.ReadValue<Vector2>();
	public Button jump => asset.Gameplay.Jump;
	public Button attack => asset.Gameplay.Attack;
	public Button parry => asset.Gameplay.Parry;
	public Button grab => asset.Gameplay.Grab;
	public Button shootMode => asset.Gameplay.ShootMode;
	public Button shoot => asset.Gameplay.Shoot;
	public Button charge => asset.Gameplay.Charge;
	public Button interact => asset.Gameplay.Interact;


}