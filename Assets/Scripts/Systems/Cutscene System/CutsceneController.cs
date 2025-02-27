using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Animator))]
public class CutsceneController : MonoBehaviour
{
    /*
        Author - Jacob Dreyer
        2-27-2025

        GOAL:
            The function of this script is to handle triggering the cameras necessary for cutscenes
            This includes simple camera focus on a spot in the stage for emphasis, or for more complex scenes

        USEAGE:
            This script is applied to any object that needs to handle the given cutscene, therefore, there will be many of them in the scene. This component should ONLY be enabled once the player steps within a given trigger
    */

    private Animator animator; // Camera animator for camera states

    private InputAction action;

    [Tooltip("If true, cameras will move between eachother in a linear order.")]
    public bool fixedCameraChanges = true;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        action.Enable();
    }

    private void OnDisable()
    {
        action.Disable();
    }

    void Start()
    {
        action.performed += _ => SwitchState();
    }

    private void SwitchState()
    {
        //However the camera is advanced, we want it to move through each camera linearly
        //Leave room for non-linear camera changes

        //Send a trigger to the animator to cause a linear change
        //animator.SetTrigger("NextCamera");
    }

    //This function is for controlling the specific camera that should be advanced toward, based on the current system.
    private void ChangeCamera(string cameraName)
    {
        animator.Play(cameraName);
    }
}
