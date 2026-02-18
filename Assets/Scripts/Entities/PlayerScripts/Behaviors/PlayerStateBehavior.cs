using UnityEngine;
using SLS.StateMachineH;

[DefaultExecutionOrder(ExecutionOrders.PlayerBehaviors)]
public abstract class PlayerStateBehavior : StateBehavior
{
    [HideInInspector] public new PlayerStateMachine Machine;
    [HideInInspector] public PlayerMovementBody playerMovementBody;
    [HideInInspector] public PlayerController playerController;
    

    protected override void OnSetup()
    {
#if UNITY_EDITOR
        Machine = base.Machine as PlayerStateMachine;
        playerMovementBody = Machine.GetComponent<PlayerMovementBody>();
        playerController = Machine.GetComponent<PlayerController>();
#else
        Machine = base.Machine as PlayerStateMachine;
        playerMovementBody = Player.MovementBody;
        playerController = Player.Controller;
#endif
    }
}
