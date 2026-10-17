using System;
using System.Collections.Generic;
using System.Text;
using static RageRooster.Player.Services;
public class PlayerGravityApplier : PlayerMovementEffector
{
    public bool Active = true;
    public float gravity = 9.81f;
    public float terminalVelocity = 100f;
    public bool flatGravity = false;

    public override bool VerticalMovement(out float result)
    {
        result = Player.MovementBody.Velocity.u;
        if (!Active) return false;
        result = ApplyGravity(gravity, terminalVelocity, flatGravity);
        return true;
    }
}