using EditorAttributes;
using SLS.StateMachineH;
using UnityEngine;

public abstract class PlayerMovementEffector : PlayerStateBehavior
{
    protected override void OnFixedUpdate()
    {
        if(this.ForwardMovement(out float FS))
        {
            Player.MovementBody.velocity.f = FS;
        }
        else if (this.DirectionalMovement(out float F, out float? S))
        {
            Player.MovementBody.velocity.f = F;
            if (S.HasValue) Player.MovementBody.velocity.s = S.Value;
        }

        if (this.HorizontalMovement(out float X, out float Z))
        {
            Player.MovementBody.velocity.x = X;
            Player.MovementBody.velocity.z = Z;
        }

        if (this.VerticalMovement(out float Y)) Player.MovementBody.velocity.u = Y;
    }

    public virtual bool ForwardMovement(out float resultF)
    {
        resultF = 0;
        return false;
    }
    public virtual bool DirectionalMovement(out float resultF, out float? resultS)
    {
        resultF = 0;
        resultS = null;
        return false;
    }
    public virtual bool HorizontalMovement(out float resultX, out float resultZ)
    {
        resultX = 0;
        resultZ = 0;
        return false;
    }
    public virtual bool VerticalMovement(out float result)
    {
        result = 0;
        return false;
    }

    //Probably not actually helpfull.
    protected virtual bool HorizontalCast(float vX, float vZ, out RaycastHit hit)
    {
        Vector3 velocity = new(vX, 0, vZ);
        return Player.MovementBody.SweepBody(velocity, out hit);
    }

    protected float ApplyGravity(float gravity, float terminalVelocity, bool flatGravity = false)
    {
        return (!flatGravity
            ? Player.MovementBody.velocity.y - (gravity * Time.deltaTime)
            : -gravity * Time.deltaTime
            ).Min(-terminalVelocity);
    }

}