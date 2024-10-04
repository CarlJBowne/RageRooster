using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class IdleState : ICharacterState
{
    public void EnterState(AnimationAndMovementController controller)
    {
        controller._animator.SetBool(controller._isWalkingHash, false);
        controller._animator.SetBool(controller._isRunningHash, false);
    }

    public void UpdateState(AnimationAndMovementController controller)
    {
        if (controller._isMovementPressed)
        {
            controller.TransitionToState(controller.WalkingState);
        }
    }

    public void ExitState(AnimationAndMovementController controller) { }
}

public class WalkingState : ICharacterState
{
    public void EnterState(AnimationAndMovementController controller)
    {
        controller._animator.SetBool(controller._isWalkingHash, true);
    }

    public void UpdateState(AnimationAndMovementController controller)
    {
        if (!controller._isMovementPressed)
        {
            controller.TransitionToState(controller.IdleState);
        }
        else if (controller._isRunPressed)
        {
            controller.TransitionToState(controller.RunningState);
        }
    }

    public void ExitState(AnimationAndMovementController controller)
    {
        controller._animator.SetBool(controller._isWalkingHash, false);
    }
}