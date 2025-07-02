using EditorAttributes;
using SLS.StateMachineH;
using UnityEngine;

public abstract class PlayerMovementEffector : PlayerStateBehavior
{
    [HideInEditMode, DisableInPlayMode] public bool trueActive;

    protected override void OnFixedUpdate()
    {
        if (!trueActive) return;
        this.HorizontalMovement(out float? X, out float? Z);
        this.VerticalMovement(out float? Y);
        playerMovementBody.VelocitySet(X, Y, Z);
    }

    public virtual void HorizontalMovement(out float? resultX, out float? resultZ) { resultX = null; resultZ = null; }
    public virtual void VerticalMovement(out float? result) { result = null; }

    //Probably not actually helpfull.
    protected virtual bool HorizontalCast(float vX, float vZ, out RaycastHit hit)
    {
        Vector3 velocity = new(vX, 0, vZ);
        return playerMovementBody.rb.DirectionCast(velocity.normalized, velocity.magnitude, 0, out hit);
    }

    protected float ApplyGravity(float gravity, float terminalVelocity, bool flatGravity = false)
    {
        return (!flatGravity
            ? playerMovementBody.velocity.y - (gravity * Time.deltaTime)
            : -gravity * Time.deltaTime
            ).Min(-terminalVelocity);
    }

    protected override void OnEnter(State prev, bool isFinal) => trueActive = isFinal;
}