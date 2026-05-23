using EditorAttributes;
using SLS.StateMachineH;
using UnityEngine;

[System.Obsolete]
public class PlayerAirborn : PlayerStateBehavior
{

    public float gravity = 9.81f;
    public float terminalVelocity = 100f;
    public bool flatGravity = false;
    public float jumpHeight;
    public float jumpPower;
    public float jumpMinHeight;
    public bool allowMidFall = true;
    public PlayerAirborn fallState;

    protected float targetMinHeight;
    protected float targetHeight;

    [SerializeField, DisableInEditMode, DisableInPlayMode] protected int phase = -1; 
    //-1 = Inactive
    //0 = PreMinHeight
    //1 = PreMaxHeight
    //2 = SlowingDown
    //3 = Falling


    protected override void OnFixedUpdate()
    {
        playerMovementBody.VelocitySet(y: ApplyGravity());

        if (jumpHeight <= 0) return;

        if (phase == 0 && transform.position.y >= targetMinHeight) phase = 1;
        if (phase == 1 && transform.position.y >= targetHeight) phase = 2;
        if (phase == 2 && playerMovementBody.velocity.y < 0) phase = 3; 

        if (phase < 2) playerMovementBody.VelocitySet(y: jumpPower);
        if (playerMovementBody.velocity.y <= 0 || (allowMidFall && phase > 0 && !Input.Jump.IsPressed()))
        {
            if (playerMovementBody.velocity.y > 0) playerMovementBody.VelocitySet(y: 0);
            phase = 3;
            if (fallState != null) fallState.State.Enter();
        }
        
    }

    protected override void OnEnter(State prev, bool isFinal)
    {
        if (jumpPower == 0) return;
        phase = 0;
        playerMovementBody.VelocitySet(y: jumpPower);
        if (jumpPower <= 0) return;
        targetMinHeight = transform.position.y + jumpMinHeight;
        targetHeight = (transform.position.y + jumpHeight) - (jumpPower.P()) / (2 * gravity);
        if (targetHeight <= transform.position.y)
        {
            playerMovementBody.VelocitySet(y: Mathf.Sqrt(2 * gravity * jumpHeight));
            targetMinHeight = transform.position.y;
        }

#if UNITY_EDITOR
        playerMovementBody.jumpMarkers = new()
        {
            transform.position,
            transform.position + Vector3.up * targetHeight,
            transform.position + Vector3.up * jumpHeight
        };
#endif
    }
    protected override void OnExit(State next) => phase = -1;

    protected float ApplyGravity()
    {
        return  (!flatGravity 
            ? playerMovementBody.velocity.y - (gravity * Time.deltaTime) 
            : -gravity * Time.deltaTime
            ).Min(-terminalVelocity);
    }

    protected void BeginJump() => State.Enter();
    public void BeginJump(float power, float height, float minHeight)
    {
        jumpPower = power;
        jumpHeight = height;
        jumpMinHeight = minHeight;

        BeginJump();
    }
}
