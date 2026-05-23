using UnityEngine;
using SLS.StateMachineH;

[DefaultExecutionOrder(ExecutionOrders.PlayerBehaviors)]
public abstract class PlayerStateBehavior : StateBehavior
{
    [HideInInspector] public new PlayerStateMachine Machine;


    protected override void OnSetup() => Machine = base.Machine as PlayerStateMachine;
}
