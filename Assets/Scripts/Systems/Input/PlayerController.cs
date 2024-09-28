using System.Collections;
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
    private Transform cameraTransform;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        cameraTransform = Camera.main.transform;
    }

    void Update()
    {
        Vector2 movementInput = Input.Movement;
        Vector3 movement = new Vector3(movementInput.x, 0, movementInput.y);

        // Adjust movement direction based on camera's forward direction
        Vector3 cameraForward = cameraTransform.forward;
        cameraForward.y = 0; // Ignore vertical component
        cameraForward.Normalize();

        Vector3 cameraRight = cameraTransform.right;
        cameraRight.y = 0; // Ignore vertical component
        cameraRight.Normalize();

        Vector3 adjustedMovement = (cameraForward * movement.z + cameraRight * movement.x) * moveSpeed * Time.deltaTime;
        transform.Translate(adjustedMovement, Space.World);

        bool jump = Input.Jump.triggered;

        isGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);

        if (jump && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }
}
