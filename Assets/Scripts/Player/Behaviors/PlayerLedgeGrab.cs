using EditorAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.StateMachineH;

public class PlayerLedgeGrab : PlayerMovementEffector
{
    public bool ledged;
    public State ledgeState;


    // Update is called once per frame
    void Update()
    {
        LedgeGrab();
    }

    public void LedgeGrab()
    {
        if (Player.MovementBody.Velocity.y < 0)
        {
            //Debug.Log("Are we passing this?");
            RaycastHit downHit;
            Vector3 LineDownStart = (transform.position + Vector3.up * 3.0f) + transform.forward;
            Vector3 LineDownEnd = (transform.position + Vector3.up * 0.7f) + transform.forward;
            Physics.Linecast(LineDownStart, LineDownEnd, out downHit, LayerMask.GetMask("Ledge"));
            Debug.DrawLine(LineDownStart, LineDownEnd);

            if (downHit.collider != null)
            {
                RaycastHit fwdHit;
                Vector3 LineFwdStart = new Vector3(transform.position.x, downHit.point.y - 0.1f, transform.position.z);
                Vector3 LineFwdEnd = new Vector3(transform.position.x, downHit.point.y - 0.1f, transform.position.z) + transform.forward;
                Physics.Linecast(LineFwdStart, LineFwdEnd, out fwdHit, LayerMask.GetMask("Ledge"));
                Debug.DrawLine(LineFwdStart, LineFwdEnd);

                if (fwdHit.collider != null)
                {
                    Machine.SendSignal("EndFall");
                    Player.MovementBody.Velocity.ZeroOut();

                    ledged = true;
                    //fallState = null;



                    Vector3 hangPos = new Vector3(fwdHit.point.x, downHit.point.y, fwdHit.point.z);
                    Vector3 offset = transform.forward * -0.1f + transform.up * -2.0f;
                    hangPos += offset;
                    transform.position = hangPos;
                    transform.forward = -fwdHit.normal;
                }
            }
        }
    }
}
