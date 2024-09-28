using System.Collections;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;

    void Update()
    {
        Vector2 movementInput = Input.Movement;
        Vector3 movement = new Vector3(movementInput.x, 0, movementInput.y) * moveSpeed * Time.deltaTime;
        transform.Translate(movement, Space.World);

        Vector2 camera = Input.Camera;
        bool jump = Input.Jump.triggered;
        bool attack = Input.Attack.triggered;
        bool block = Input.Block.triggered;
        bool grab = Input.Grab.triggered;
        bool shootMode = Input.ShootMode.triggered;
        bool shoot = Input.Shoot.triggered;
        bool charge = Input.Charge.triggered;
        bool sonic = Input.Sonic.triggered;      
    }
}
