using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.StateMachineH;
using Utilities.Xtensions.Unity;

public class PlayerUppercut : PlayerMovementEffector
{
    public float gravity = 9.81f;
    public float ucPower;
    public float ucHeight;
    public float ucMinHeight;

    protected float targetUcHeight;
    protected float targetMinUcHeight;

    protected override void OnFixedUpdate() => UppercutJump();

    public void UppercutJump()
    {
        Debug.Log("Real?(1)");
        Player.MovementBody.velocity.y = ucPower;
        targetMinUcHeight = transform.position.y + ucMinHeight;
        targetUcHeight = (transform.position.y + ucHeight) - (ucPower.P()) / (2 * gravity);
        if (targetUcHeight <= transform.position.y)
        {
            Player.MovementBody.velocity.y = Mathf.Sqrt(2 * gravity * ucHeight);
            targetMinUcHeight = transform.position.y;
        }
        Debug.Log("Real?(2)");
    }
}
