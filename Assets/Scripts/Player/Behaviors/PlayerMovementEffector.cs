using EditorAttributes;
using SLS.StateMachineH;
using UnityEngine;
using static RageRooster.Player.Services;

public abstract class PlayerMovementEffector : StateBehavior
{
    protected override void OnFixedUpdate()
    {
        if(this.ForwardMovement(out float FS))
        {
            Player.MovementBody.Velocity.f = FS;
        }
        else if (this.DirectionalMovement(out float F, out float? S))
        {
            Player.MovementBody.Velocity.f = F;
            if (S.HasValue) Player.MovementBody.Velocity.s = S.Value;
        }

        if (this.HorizontalMovement(out float X, out float Z))
        {
            Player.MovementBody.Velocity.x = X;
            Player.MovementBody.Velocity.z = Z;
        }

        if (this.VerticalMovement(out float Y)) Player.MovementBody.Velocity.u = Y;
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
        return Player.MovementBody.Sweep(velocity, out hit);
    }

    protected float ApplyGravity(float gravity, float terminalVelocity, bool flatGravity = false)
    {
        return (!flatGravity
            ? Player.MovementBody.Velocity.y - (gravity * Time.deltaTime)
            : -gravity * Time.deltaTime
            ).Min(-terminalVelocity);
    }

}