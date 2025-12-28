using EditorAttributes;
using RageRooster.Systems.SaveSystem;
using SLS.StateMachineH;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using CTX = UnityEngine.InputSystem.InputAction.CallbackContext;

[DefaultExecutionOrder(ExecutionOrders.PlayerSystems)]
public class PlayerController : PlayerStateBehavior
{
    #region Config

    public float jumpBuffer = 0.3f;

    public PlayerAirborneMovement airChargeState;
    public PlayerAirborneMovement airChargeFallState;
    public PlayerAirborneMovement glideCheck; //Keeping this here for now in case we decide to re-implement the gliding.
    public PlayerWallJump wallJumpState;
    public PlayerRanged ranged;
    public PlayerAiming aimingState;
    public State groundedSpin;
    public State airSpin;
    public PlayerHellcopterMovement airUpwardTornado;
    public State ventGlideState;

    public bool overrideMovementControl;
    public Vector2 overrideMovementVector;

    #endregion
    #region Data

    [HideProperty] public float jumpInput;
    [HideProperty] public Vector3 camAdjustedMovement;
    [HideProperty] public PlayerRanged grabber;

    #endregion
    #region Getters

    #endregion

    protected override void OnAwake()
    {
        if (!grabber) grabber = GetComponentFromMachine<PlayerRanged>();
    }

    private void OnEnable()
    {
        if(true)
        {
            NewSystemSubscribe(true);
            return;
        }
        Input.Jump.performed += JumpPress;
        Input.AttackTap.performed += BeginActionEvent;
        Input.AttackHold.performed += BeginActionEvent;
        Input.Grab.performed += BeginActionEvent;
        Input.Parry.performed += BeginActionEvent;
        Input.Interact.performed += BeginActionEvent;

        Input.Jump.canceled += JumpRelease;
        Input.Aim.performed += ShootModeActivate;
        Input.Aim.canceled += ShootModeDeactivate;

        Input.Charge1.performed += ChargeButtons;
        Input.Charge2.performed += ChargeButtons;
    }
    private void OnDisable()
    {
        if (true)
        {
            NewSystemSubscribe(false);
            return;
        }
        Input.Jump.performed -= JumpPress;
        Input.AttackTap.performed -= BeginActionEvent;
        Input.AttackHold.performed -= BeginActionEvent;
        Input.Grab.performed -= BeginActionEvent;
        Input.Parry.performed -= BeginActionEvent;
        Input.Interact.performed -= BeginActionEvent;

        Input.Jump.canceled -= JumpRelease;
        Input.Aim.performed -= ShootModeActivate;
        Input.Aim.canceled -= ShootModeDeactivate;

        Input.Charge1.performed -= ChargeButtons;
        Input.Charge2.performed -= ChargeButtons;
    }

    protected override void OnUpdate()
    {
        if (!enabled) return;
        if (jumpInput > 0) jumpInput -= Time.deltaTime;
        if (!overrideMovementControl) camAdjustedMovement = Input.Movement.ToXZ().Rotate(Machine.cameraTransform.eulerAngles.y, Vector3.up);
        else camAdjustedMovement = overrideMovementVector.ToXZ().Rotate(Machine.cameraTransform.eulerAngles.y, Vector3.up);
    }

    public bool CheckJumpBuffer()
    {
        bool result = jumpInput > 0;
        jumpInput = 0;
        return result;
    }
    public void BeginJumpInputBuffer() => jumpInput = jumpBuffer + Time.fixedDeltaTime;



    private void BeginActionEvent(InputAction.CallbackContext callbackContext) => Machine.SendSignal(callbackContext.action.name);
    public void BeginActionEvent(string name) => Machine.SendSignal(name);

    public void ReadyNextAction() => Machine.SignalManager.Unlock();
    public void FinishAction() => Machine.SignalManager.FireSignal(new("Finish", ignoreLock: true));



    public void ParryActionAirborne()
    {
        if (Upgrades.Active.hellcopter)
        {
            airSpin.Enter();
            if (playerMovementBody.isOverVent) Machine.SendSignal(new("EnterVent", 0, true));
        }
    }

    public void MidJumpJumpAction()
    {
        if (!wallJumpState.WallJump(transform.forward))
        {
            (!playerMovementBody.isOverVent ? sGlide : ventGlideState).Enter();
        }
    }
    public void MidWallJumpJumpAction() => wallJumpState.WallJump(transform.forward);

    //Other events.
    private void JumpPress(CTX ctx) => jumpInput = Machine.SendSignal(ctx.action.name) ? 0 : jumpBuffer;
    private void JumpRelease(CTX ctx) => Machine.SendSignal(new("JumpRelease", 0, true));
    private void ShootModeActivate(CTX ctx) => Machine.SendSignal(new("ShootMode", ignoreLock: true));
    private void ShootModeDeactivate(CTX ctx) => Machine.SendSignal(new("ShootModeExit", ignoreLock: true));

    private void ChargeButtons(CTX ctx) => Machine.SendSignal("Charge");


    //NewButtonSystem.

    private void NewSystemSubscribe(bool value)
    {
        if (value)
        {
            Input.Jump.started += ButtonPressed;
            Input.Jump.canceled += ButtonRelease;
            Input.AttackTap.started += ButtonPressed;
            Input.AttackTap.canceled += ButtonRelease;
            Input.Grab.started += ButtonPressed;
            Input.Grab.canceled += ButtonRelease;
            Input.Charge1.started += ButtonPressed;
            Input.Charge1.canceled += ButtonRelease;
            Input.Charge2.started += ButtonPressed;
            Input.Charge2.canceled += ButtonRelease;
            Input.Aim.started += ButtonPressed;
            Input.Aim.canceled += ButtonRelease;
            Input.Parry.started += ButtonPressed;
            Input.Parry.canceled += ButtonRelease;
        }
        else
        {
            Input.Jump.started -= ButtonPressed;
            Input.Jump.canceled -= ButtonRelease;
            Input.AttackTap.started -= ButtonPressed;
            Input.AttackTap.canceled -= ButtonRelease;
            Input.Grab.started -= ButtonPressed;
            Input.Grab.canceled -= ButtonRelease;
            Input.Charge1.started -= ButtonPressed;
            Input.Charge1.canceled -= ButtonRelease;
            Input.Charge2.started -= ButtonPressed;
            Input.Charge2.canceled -= ButtonRelease;
            Input.Aim.started -= ButtonPressed;
            Input.Aim.canceled -= ButtonRelease;
            Input.Parry.started -= ButtonPressed;
            Input.Parry.canceled -= ButtonRelease;
        }
    }

    private void ButtonPressed(CTX c)
    {
        if (!ButtonReady || PlayerButtonAction.Current != null || ActionSourceStack.Count == 0) return;
        if (ActionSourceStack.Peek().GetButtonAction(c.action) is PlayerButtonAction action and not null)
        {
            action.Begin(c.action);
            action.Press();
        }
    }
    private void ButtonRelease(CTX c)
    {
        if (PlayerButtonAction.Current != null && PlayerButtonAction.Current.activeButton == c.action)
        {
            if (ButtonReady) PlayerButtonAction.Current.Release();
            PlayerButtonAction.Current.Finish();
        }
    }

    public static bool ButtonReady = true;
    private static Stack<PlayerButtonActions> ActionSourceStack = new();


    public static void RegisterActionSource(PlayerButtonActions source, bool deregister = false)
    {
        if (!deregister) ActionSourceStack.Push(source);
        else if (ActionSourceStack.Count > 0) ActionSourceStack.Pop();
        //Technically this WILL cause issues if sources are deregistered out of order, but in practice that shouldn't be technically possible.
    }



    [ContextMenu("Transfer to New Buttons")]
    private void TransferFromSignalsToNewButtons()
    {
        SLS.StateMachineH.Signals.SignalNode[] signalNodes = gameObject.GetComponentsInChildren<SLS.StateMachineH.Signals.SignalNode>();

        foreach (var signalNode in signalNodes)
        {
            PlayerButtonActions actionSet = signalNode.GetOrAddComponent<PlayerButtonActions>();


            if (signalNode.signals.ContainsKey("Jump"))
            {
                actionSet.Jump = new PlayerButtonAction.BasicPush()
                {
                    pressEvent = signalNode.signals["Jump"]
                };
                signalNode.signals.Remove("Jump");
            }


            if (signalNode.signals.ContainsKey("AttackTap") && signalNode.signals.ContainsKey("AttackHold"))
            {
                actionSet.Attack = new PlayerButtonAction.TapOrHold()
                {
                    tapEvent = signalNode.signals["AttackTap"],
                    holdEvent = signalNode.signals["AttackHold"]
                };
                signalNode.signals.Remove("AttackTap");
                signalNode.signals.Remove("AttackHold");
            }
            else if (signalNode.signals.ContainsKey("AttackTap"))
            {
                actionSet.Jump = new PlayerButtonAction.BasicPush()
                {
                    pressEvent = signalNode.signals["AttackTap"]
                };
                signalNode.signals.Remove("AttackTap");
            }
            else if (signalNode.signals.ContainsKey("AttackHold"))
            {
                actionSet.Jump = new PlayerButtonAction.TapOrHold()
                {
                    holdEvent = signalNode.signals["AttackHold"],
                    autoFinishHold = true
                };
                signalNode.signals.Remove("AttackHold");
            }


            if(signalNode.signals.ContainsKey("Grab"))
            {
                actionSet.Grab = new PlayerButtonAction.BasicPush()
                {
                    pressEvent = signalNode.signals["Grab"]
                };
                signalNode.signals.Remove("Grab");
            }
            if(signalNode.signals.ContainsKey("Charge"))
            {
                actionSet.Charge = new PlayerButtonAction.BasicPush()
                {
                    pressEvent = signalNode.signals["Charge"]
                };
                signalNode.signals.Remove("Charge");
            }


            if(signalNode.signals.ContainsKey("ShootMode"))
            {
                actionSet.Aim = new PlayerButtonAction.CrossStatePressRelease()
                {
                    actionEvent = signalNode.signals["ShootMode"]
                };
                signalNode.signals.Remove("ShootMode");
            }
            else if (signalNode.signals.ContainsKey("ShootModeExit"))
            {
                actionSet.Aim = new PlayerButtonAction.CrossStatePressRelease()
                {
                    actionEvent = signalNode.signals["ShootModeExit"]
                };
                signalNode.signals.Remove("ShootModeExit");
            }


            if (signalNode.signals.ContainsKey("Parry"))
            {
                actionSet.Parry = new PlayerButtonAction.BasicPush()
                {
                    pressEvent = signalNode.signals["Parry"]
                };
                signalNode.signals.Remove("Parry");
            }
           
        }
    }
}