using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICharacterState
{
    void EnterState(AnimationAndMovementController controller);
    void UpdateState(AnimationAndMovementController controller);
    void ExitState(AnimationAndMovementController controller);
}
