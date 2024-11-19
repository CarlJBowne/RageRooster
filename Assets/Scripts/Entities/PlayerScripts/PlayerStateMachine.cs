using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.StateMachineV2;

public class PlayerStateMachine : StateMachine
{
    #region Config
    #endregion

    #region Data
    [HideInInspector] public Animator animator;
    [HideInInspector] public Input input;
    [HideInInspector] public PlayerMovementBody body;
    [HideInInspector] public PlayerController controller;
    public Transform cameraTransform;

    #endregion



    protected override void Initialize()
    {
        animator = GetComponent<Animator>();
        input = Input.Get();
        body = GetGlobalBehavior<PlayerMovementBody>();
        controller = GetGlobalBehavior<PlayerController>();
    }

    //private void OnCollisionStay() => body.Collision();
    //private void OnCollisionExit() => body.Collision();


    [SerializeField]
    private AYellowpaper.SerializedCollections.SerializedDictionary<string, bool> upgrades = new()
    {
        { "GroundSlam", true },
        { "DropLaunch", true },
        { "WallJump", true },
        { "RageCharge", true }
    }; 
    public bool GroundSlam => upgrades["GroundSlam"];
    public bool DropLaunch => upgrades["DropLaunch"];
    public bool WallJump => upgrades["WallJump"];
    public bool RageCharge => upgrades["RageCharge"];
    public void SetUpgrade(string id, bool value)
    {
        upgrades[id] = value;

    }


}
public abstract class PlayerStateBehavior : StateBehavior
{
    [HideInInspector] public new PlayerStateMachine M;
    [HideInInspector] public Input input;
    [HideInInspector] public PlayerMovementBody body;
    [HideInInspector] public PlayerController controller;

    protected override void Initialize(StateMachine machine)
    {
        M = machine as PlayerStateMachine;
        input = M.input;
        body = M.body;
        controller = M.controller;
    }

}
