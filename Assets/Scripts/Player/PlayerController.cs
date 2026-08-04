using EditorAttributes;
using RageRooster.SaveSystem;
using SLS.StateMachineH;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Utilities.Xtensions;

using CTX = UnityEngine.InputSystem.InputAction.CallbackContext;

[DefaultExecutionOrder(ExecutionOrders.PlayerSystems)]
public class PlayerController : StateBehavior
{
    #region Config

    public float jumpBuffer = 0.3f;

    public bool overrideMovementControl;
    public Vector2 overrideMovementVector;

    #endregion
    #region Data

    [HideProperty] public float jumpInput;
    [HideProperty]
    public Vector3 camAdjustedMovement
    {
        get => !overrideMovementControl
          ? Input.Movement.ToXZ().Rotated(Cameras.RealCamera.transform.eulerAngles.y, Vector3.up)
          : overrideMovementVector.ToXZ().Rotated(Cameras.RealCamera.transform.eulerAngles.y, Vector3.up);
    }
    [SerializeField] Upgrades upgradesDisplay;

    #endregion
    #region Getters

    #endregion

    protected override void OnAwake() => upgradesDisplay = Upgrades.Active;

    private void OnEnable()
    {
        Input.Jump.started += ActionButtonPressed;
        Input.Jump.canceled += ActionButtonReleased;
        Input.Attack.started += ActionButtonPressed;
        Input.Attack.canceled += ActionButtonReleased;
        Input.Grab.started += ActionButtonPressed;
        Input.Grab.canceled += ActionButtonReleased;
        Input.Charge1.started += ActionButtonPressed;
        Input.Charge1.canceled += ActionButtonReleased;
        Input.Charge2.started += ActionButtonPressed;
        Input.Charge2.canceled += ActionButtonReleased;
        Input.Aim.started += AimPress;
        Input.Aim.canceled += AimRelease;
        //Input.Aim.started += ButtonPressed;
        //Input.Aim.canceled += ButtonRelease;
        Input.Parry.started += ActionButtonPressed;
        Input.Parry.canceled += ActionButtonReleased;
    }
    private void OnDisable()
    {
        Input.Jump.started -= ActionButtonPressed;
        Input.Jump.canceled -= ActionButtonReleased;
        Input.Attack.started -= ActionButtonPressed;
        Input.Attack.canceled -= ActionButtonReleased;
        Input.Grab.started -= ActionButtonPressed;
        Input.Grab.canceled -= ActionButtonReleased;
        Input.Charge1.started -= ActionButtonPressed;
        Input.Charge1.canceled -= ActionButtonReleased;
        Input.Charge2.started -= ActionButtonPressed;
        Input.Charge2.canceled -= ActionButtonReleased;
        Input.Aim.started -= AimPress;
        Input.Aim.canceled -= AimRelease;
        //Input.Aim.started -= ButtonPressed;
        //Input.Aim.canceled -= ButtonRelease;
        Input.Parry.started -= ActionButtonPressed;
        Input.Parry.canceled -= ActionButtonReleased;
    }

    protected override void OnUpdate()
    {
        if (!enabled) return;
        if (jumpInput > 0) jumpInput -= Time.deltaTime;
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
            Self.StateMachine.AirParry.Enter();
            if (Self.MovementBody.isOverVent) Machine.SendSignal(new("EnterVent", 0, true));
        }
    }

    public void MidJumpJumpAction()
    {
        if (!Self.StateMachine.WallJump.WallJump(transform.forward))
        {
            if (Self.MovementBody.isOverVent) Self.StateMachine.VentGliding.Enter();
            else Self.StateMachine.Gliding.Enter();
        }
    }
    public void MidWallJumpJumpAction() => Self.StateMachine.WallJump.WallJump(transform.forward);

    public static void AirJumpAction(bool allowDoubleJump, bool allowGlide)
    {
        if (Upgrades.Active.wallJump && Self.StateMachine.WallJump.WallJump(Self.Transform.forward)) return;
        else if (allowDoubleJump && Upgrades.Active.doubleJump && Self.MovementBody.canDoDoubleJump)
        {
            Self.StateMachine.Jump.BeginJump();
            Self.MovementBody.canDoDoubleJump = false;
        }
        else if (allowGlide && Upgrades.Active.glide)
        {
            if (Self.MovementBody.isOverVent) Self.StateMachine.VentGliding.Enter();
            else Self.StateMachine.Gliding.Enter();
        }
    }


    private void AimPress(CTX cTX) => Machine.SendSignal("Aim");
    private void AimRelease(CTX cTX) => Machine.SendSignal("AimRelease");



    //NewButtonSystem.

    private void ActionButtonPressed(CTX c)
    {
        if (PlayerButtonAction.Current != null || ButtonLocked) return;
        int i = ActionSourceStack.Count - 1;
        while(i > -1)
        {
            if (i > -1 && ActionSourceStack[i] != null && !ActionSourceStack[i].Locked) break;
            i--;
        }
        if (i == -1) return;
        if (!(ActionSourceStack[i][c.action] is PlayerButtonAction action and not null) || action.active) return;
        ActiveButtonAction = c.action;
        action.Press();
    }
    private void ActionButtonReleased(CTX c)
    {
        if (PlayerButtonAction.Current == null || ActiveButtonAction != c.action) return;
        PlayerButtonAction.Current.Release();
        ActiveButtonAction = null;
    }

    public bool ButtonLocked = false;
    public static InputAction ActiveButtonAction { get; private set; } = null;
    private readonly static List<PlayerButtonActions> ActionSourceStack = new();

    public static void RegisterActionSource(PlayerButtonActions source, bool deregister = false)
    {
        if (!deregister && !ActionSourceStack.Contains(source)) ActionSourceStack.Add(source);
        else if (deregister && ActionSourceStack.Contains(source)) ActionSourceStack.Remove(source);
    }


    //Note to self: For some bizarre reason the player is exiting and entering IDLE state several times on scene load for no apparent reason. Investigate later.

    [ContextMenu("Transfer to New Buttons")]
    private void TransferFromSignalsToNewButtons()
    {
        Recurse(Machine);
        void Recurse(State state)
        {
            if (state.TryGetComponent(out SLS.StateMachineH.Signals.SignalNode signalNode))
            {
                PlayerButtonActions actionSet = signalNode.GetOrAddComponent<PlayerButtonActions>();

                if (signalNode.ContainsName("Jump"))
                {
                    actionSet.Jump = new PlayerButtonAction.BasicPush()
                    {
                        pressEvent = signalNode["Jump"]
                    };
                    signalNode.Remove("Jump");
                }


                if (signalNode.ContainsName("AttackTap") && signalNode.ContainsName("AttackHold"))
                {
                    actionSet.Attack = new PlayerButtonAction.TapOrHold()
                    {
                        tapEvent = signalNode["AttackTap"],
                        holdEvent = signalNode["AttackHold"]
                    };
                    signalNode.Remove("AttackTap");
                    signalNode.Remove("AttackHold");
                }
                else if (signalNode.ContainsName("AttackTap"))
                {
                    actionSet.Jump = new PlayerButtonAction.BasicPush()
                    {
                        pressEvent = signalNode["AttackTap"]
                    };
                    signalNode.Remove("AttackTap");
                }
                else if (signalNode.ContainsName("AttackHold"))
                {
                    actionSet.Jump = new PlayerButtonAction.TapOrHold()
                    {
                        holdEvent = signalNode["AttackHold"],
                        autoFinishHold = true
                    };
                    signalNode.Remove("AttackHold");
                }


                if (signalNode.ContainsName("Grab"))
                {
                    actionSet.Grab = new PlayerButtonAction.BasicPush()
                    {
                        pressEvent = signalNode["Grab"]
                    };
                    signalNode.Remove("Grab");
                }
                if (signalNode.ContainsName("Charge"))
                {
                    actionSet.Charge = new PlayerButtonAction.BasicPush()
                    {
                        pressEvent = signalNode["Charge"]
                    };
                    signalNode.Remove("Charge");
                }


                //if (signalNode.signals.ContainsKey("ShootMode"))
                //{
                //    actionSet.Aim = new PlayerButtonAction.CrossStatePressRelease()
                //    {
                //        actionEvent = signalNode.signals["ShootMode"]
                //    };
                //    signalNode.signals.Remove("ShootMode");
                //}
                //else if (signalNode.signals.ContainsKey("ShootModeExit"))
                //{
                //    actionSet.Aim = new PlayerButtonAction.CrossStatePressRelease()
                //    {
                //        actionEvent = signalNode.signals["ShootModeExit"]
                //    };
                //    signalNode.signals.Remove("ShootModeExit");
                //}


                if (signalNode.ContainsName("Parry"))
                {
                    actionSet.Parry = new PlayerButtonAction.BasicPush()
                    {
                        pressEvent = signalNode["Parry"]
                    };
                    signalNode.Remove("Parry");
                }

            }
            foreach (var child in state.Children) Recurse(child);
        }
    }
}

