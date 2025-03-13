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

    public static void Enable() => Get().asset.Enable();
	public static void Disable() => Get().asset.Disable();

    public static Vector2 Movement => Get().asset.Gameplay.Movement.ReadValue<Vector2>();
	public static Vector2 Camera => Get().asset.Gameplay.Camera.ReadValue<Vector2>();
	public static Button Jump => Get().asset.Gameplay.Jump;
	public static Button AttackTap => Get().asset.Gameplay.AttackTap;
	public static Button AttackHold => Get().asset.Gameplay.AttackHold;
	public static Button Parry => Get().asset.Gameplay.Parry;
	public static Button GrabTap => Get().asset.Gameplay.GrabTap;
	public static Button ShootMode => Get().asset.Gameplay.ShootMode;
	public static Button ChargeTap => Get().asset.Gameplay.ChargeTap;
	public static Button ChargeHold => Get().asset.Gameplay.ChargeHold;
	public static Button Interact => Get().asset.Gameplay.Interact;

	public Vector2 movement => asset.Gameplay.Movement.ReadValue<Vector2>();
	public Vector2 camera => asset.Gameplay.Camera.ReadValue<Vector2>();
	public Button jump => asset.Gameplay.Jump;
	public Button attackTap => asset.Gameplay.AttackTap;
	public Button attackHold => asset.Gameplay.AttackHold;
	public Button parry => asset.Gameplay.Parry;
	public Button grabTap => asset.Gameplay.GrabTap;
	public Button shootMode => asset.Gameplay.ShootMode;
	public Button chargeTap => asset.Gameplay.ChargeTap;
	public Button chargeHold => asset.Gameplay.ChargeHold;
	public Button interact => asset.Gameplay.Interact;

	public static class UI
	{
		public static Button PauseGame => Get().asset.UI.PauseGame;
	}
}