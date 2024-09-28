using System.Collections;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float jumpForce = 5f;
    public Transform groundCheck;
    public float groundCheckRadius = 0.2f;
    public LayerMask groundLayer;

    private Rigidbody rb;
    private bool isGrounded;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

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

        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);

        if (jump && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }
}
