using EditorAttributes;
using RageRooster.Systems.SaveSystem;
using SLS.StateMachineH;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Utilities.Xtensions;
using Utilities.Xtensions.Unity;
using CTX = UnityEngine.InputSystem.InputAction.CallbackContext;

[DefaultExecutionOrder(ExecutionOrders.PlayerSystems)]
public class PlayerController : PlayerStateBehavior
{
    #region Config

    public float jumpBuffer = 0.3f;

    public bool overrideMovementControl;
    public Vector2 overrideMovementVector;

    #endregion
    #region Data

    [HideProperty] public float jumpInput;
    [HideProperty] public Vector3 camAdjustedMovement;
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
        camAdjustedMovement = !overrideMovementControl
            ? Input.Movement.ToXZ().Rotated(Machine.cameraTransform.eulerAngles.y, Vector3.up)
            : overrideMovementVector.ToXZ().Rotated(Machine.cameraTransform.eulerAngles.y, Vector3.up);
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
            Player.StateMachine.AirParry.Enter();
            if (playerMovementBody.isOverVent) Machine.SendSignal(new("EnterVent", 0, true));
        }
    }

    public void MidJumpJumpAction()
    {
        if (!Player.StateMachine.WallJump.WallJump(transform.forward))
        {
            if (playerMovementBody.isOverVent) Player.StateMachine.VentGliding.Enter();
            else Player.StateMachine.Gliding.Enter();
        }
    }
    public void MidWallJumpJumpAction() => Player.StateMachine.WallJump.WallJump(transform.forward);

    public void AirJumpAction(bool allowDoubleJump, bool allowGlide)
    {
        if (Upgrades.Active.wallJump && Player.StateMachine.WallJump.WallJump(transform.forward)) return;
        else if (allowDoubleJump && Upgrades.Active.doubleJump && Player.MovementBody.canDoDoubleJump)
        {
            Player.StateMachine.Jump.BeginJump();
            Player.MovementBody.canDoDoubleJump = false;
        }
        else if (allowGlide && Upgrades.Active.glide)
        {
            if (playerMovementBody.isOverVent) Player.StateMachine.VentGliding.Enter();
            else Player.StateMachine.Gliding.Enter();
        }
    }


    private void AimPress(CTX cTX) => Machine.SendSignal("Aim");
    private void AimRelease(CTX cTX) => Machine.SendSignal("AimRelease");



    //NewButtonSystem.

    private void ActionButtonPressed(CTX c)
    {
        if (PlayerButtonAction.Current != null || ActionSourceStack.Count == 0) return;
        if (ActionSourceStack[^1][c.action] is PlayerButtonAction action and not null && !action.active)
        {
            ActiveButtonAction = c.action;
            action.Press();
        }
    }
    private void ActionButtonReleased(CTX c)
    {
        if (PlayerButtonAction.Current != null && ActiveButtonAction == c.action)
        {
            PlayerButtonAction.Current.Release();
            ActiveButtonAction = null;
        }
    }

    //public static bool ButtonReady = true; Implement later
    public static InputAction ActiveButtonAction { get; private set; } = null;
    private readonly static List<PlayerButtonActions> ActionSourceStack = new();

    public static void RegisterActionSource(PlayerButtonActions source, bool deregister = false)
    {
        if (!deregister)
        {
            if (!ActionSourceStack.Contains(source)) ActionSourceStack.Add(source);
        }
        else
        {
            if (ActionSourceStack.Count > 0 && ActionSourceStack.Contains(source))
            {
                if (ActionSourceStack[^1] == source) ActionSourceStack.RemoveAtLast();
            }
        }
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


                if (signalNode.signals.ContainsKey("Grab"))
                {
                    actionSet.Grab = new PlayerButtonAction.BasicPush()
                    {
                        pressEvent = signalNode.signals["Grab"]
                    };
                    signalNode.signals.Remove("Grab");
                }
                if (signalNode.signals.ContainsKey("Charge"))
                {
                    actionSet.Charge = new PlayerButtonAction.BasicPush()
                    {
                        pressEvent = signalNode.signals["Charge"]
                    };
                    signalNode.signals.Remove("Charge");
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


                if (signalNode.signals.ContainsKey("Parry"))
                {
                    actionSet.Parry = new PlayerButtonAction.BasicPush()
                    {
                        pressEvent = signalNode.signals["Parry"]
                    };
                    signalNode.signals.Remove("Parry");
                }

            }
            foreach (var child in state.Children) Recurse(child);
        }
    }
}