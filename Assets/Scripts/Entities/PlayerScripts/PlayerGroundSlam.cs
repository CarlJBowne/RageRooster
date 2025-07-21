using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.StateMachineV3;

public class PlayerGroundSlam : PlayerMovementEffector
{
    //public float gravity;
    //public float terminalVelocity;
    //public bool flatGravity;
    public PlayerAirborneMovement bouncingState;
    private SphereCollider attackCollider;

    public override void OnAwake() => attackCollider = GetComponent<SphereCollider>();

    public override void OnFixedUpdate() => attackCollider.center = Vector3.up * (.6f + (playerMovementBody.velocity.y * Time.fixedDeltaTime * 2));

    //public override void VerticalMovement(out float? result)
    //{
    //    result = ApplyGravity(gravity, terminalVelocity, flatGravity);
    //    
    //
    //}
    //
    //public override void OnEnter(State prev, bool isFinal) 
    //{ 
    //    base.OnEnter(prev, isFinal); 
    //    playerMovementBody.VelocitySet(y: playerMovementBody.velocity.y > gravity ? gravity : playerMovementBody.velocity.y); 
    //}

    private void OnTriggerEnter(Collider other) => BounceShroom.AttemptBounce(other.gameObject, bouncingState);

}
