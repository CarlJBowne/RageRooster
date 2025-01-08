using SLS.StateMachineV3;
using UnityEngine;

public class PlayerGroundSlam : PlayerAirborn
{

    public PlayerAirborn bouncingState;
    private SphereCollider attackCollider;

    public override void OnAwake() => attackCollider = GetComponent<SphereCollider>();

    public override void OnFixedUpdate()
    {
        body.VelocitySet(y: ApplyGravity());
        attackCollider.center = Vector3.up * (.6f + (body.velocity.y * Time.fixedDeltaTime * 2));
    }

    public override void OnEnter(State prev) => body.VelocitySet(y: body.velocity.y > jumpPower ? jumpPower : body.velocity.y);

    private void OnTriggerEnter(Collider other) => BounceShroom.AttemptBounce(other.gameObject, bouncingState); 
}
