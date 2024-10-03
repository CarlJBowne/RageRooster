using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class AnimationAndMovementController : MonoBehaviour
{
    PlayerInput _playerInput;
    CharacterController _characterController;
    Animator _animator;

    // parameter IDs
    int _isWalkingHash;
    int _isRunningHash;
    int _jumpCountHash;

    // player input values
    Vector2 _currentMovementInput;
    Vector3 _currentMovement;
    Vector3 _currentRunMovement;
    Vector3 _appliedMovement;
    Vector3 _cameraRelativeMovement;
    bool _isMovementPressed;
    bool _isRunPressed;

    // constants
    float _rotationFactorPerFrame = 15.0f;
    float _runMultiplier = 3.0f;
    int _zero = 0;

    // gravity variables
    float _gravity = -9.8f;
    float _groundedGravity = -0.05f;

    // jumping variables
    bool _isJumpPressed = false;
    float _initialJumpVelocity;
    float _maxJumpHeight = 2.0f;
    float _maxJumpTime = 0.75f;
    bool _isJumping = false;
    int _isJumpingHash;
    bool _isJumpAnimating = false;
    int _jumpCount = 0;

    // gliding variables
    bool _isGliding = false;
    float _glideGravity = -2.0f;
    int _isGlidingHash;

    Dictionary<int, float>  initialJumpVelocities = new Dictionary<int, float>();
    Dictionary<int, float>  jumpGravities = new Dictionary<int, float>();

    Coroutine currentJumpResetRoutine = null;

    void Awake() 
    {
        _playerInput = new PlayerInput();
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();

        _isWalkingHash = Animator.StringToHash("isWalking");
        _isRunningHash = Animator.StringToHash("isRunning");
        _isJumpingHash = Animator.StringToHash("isJumping");
        _jumpCountHash = Animator.StringToHash("jumpCount");
        _isGlidingHash = Animator.StringToHash("isGliding");

        _playerInput.CharacterControls.Move.started += onMovementInput;
        _playerInput.CharacterControls.Move.canceled += onMovementInput;
        _playerInput.CharacterControls.Move.performed += onMovementInput;
        _playerInput.CharacterControls.Run.started += onRun;
        _playerInput.CharacterControls.Run.canceled += onRun;
        _playerInput.CharacterControls.Jump.started += onJump;
        _playerInput.CharacterControls.Jump.canceled += onJump;

        SetupJumpVariables();
    }

    // Initializes the jump variables
    void SetupJumpVariables()
    {
        float timeToApex = _maxJumpTime / 2;
        _gravity = (-2 * _maxJumpHeight) / Mathf.Pow(timeToApex, 2);
        _initialJumpVelocity = (2 * _maxJumpHeight) / timeToApex;

        // Calculate the initial velocity and gravity for the second and third jump
        float secondJumpGravity = (-2 * (_maxJumpHeight + 2)) / Mathf.Pow(timeToApex * 1.25f, 2);
        float secondJumpInitialVelocity = (2 * (_maxJumpHeight + 2)) / (timeToApex * 1.25f);
        float thirdJumpGravity = (-2 * (_maxJumpHeight + 4)) / Mathf.Pow(timeToApex * 1.5f, 2);
        float thirdJumpInitialVelocity = (2 * (_maxJumpHeight + 4)) / (timeToApex * 1.5f);

        // Store initial jump velocities and gravities in a dictionary
        initialJumpVelocities.Add(1, _initialJumpVelocity);
        initialJumpVelocities.Add(2, secondJumpInitialVelocity);
        initialJumpVelocities.Add(3, thirdJumpInitialVelocity);

        jumpGravities.Add(0, _gravity);
        jumpGravities.Add(1, _gravity);
        jumpGravities.Add(2, secondJumpGravity);
        jumpGravities.Add(3, thirdJumpGravity);
    }

    // Handles the jump logic based on input and character state
    void HandleJump()
    {
        Debug.Log("Is Jumping: " + _isJumping);
        Debug.Log("Is Jump Pressed: " + _isJumpPressed);
        Debug.Log("Is Grounded: " + _characterController.isGrounded);
        Debug.Log("HandleJump called");

        // Check if the character can jump
        if (!_isJumping && _characterController.isGrounded && _isJumpPressed)
        {
            // Stops the jump reset routine if the player jumps before the jump reset time
            if (_jumpCount <3 && currentJumpResetRoutine != null)
            {
                StopCoroutine(currentJumpResetRoutine);
            }

            // Start the jump animation and update the jump state
            _animator.SetBool(_isJumpingHash, true);
            _isJumpAnimating = true;
            _isJumping = true;
            _jumpCount += 1;
            _animator.SetInteger(_jumpCountHash, _jumpCount);

            // Applies initial jump velocity to the character
            _currentMovement.y = initialJumpVelocities[_jumpCount];
            _appliedMovement.y = initialJumpVelocities[_jumpCount];
            Debug.Log($"Jump Count: {_jumpCount}, Initial Jump Velocity: {initialJumpVelocities[_jumpCount]}");
        }

        // Reset the jump state when the player lands
        else if (!_isJumpPressed && _isJumping && _characterController.isGrounded)
        {
            _isJumping = false;
        }
        
    }

    // Coroutine to reset the jump count after a delay
    IEnumerator JumpResetRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        _jumpCount = 0;
    }

    // Input action handlers
    void onJump(InputAction.CallbackContext context)
    {
        _isJumpPressed = context.ReadValueAsButton();

        if (_isJumpPressed && _jumpCount <3)
        {
            HandleJump();

            if (_jumpCount == 3 && !_characterController.isGrounded)
            {
                StartGliding();
            }
        }
    }

    // Input action handlers
    void onRun(InputAction.CallbackContext context)
    {
        _isRunPressed = context.ReadValueAsButton();
    }

    // Handles the character rotation based on the movement direction
    void HandleRotation()
    {
        Vector3 positionToLookAt;

        // Set the position to look at based on current movement
        positionToLookAt.x = _cameraRelativeMovement.x;

        // Ensure the y-axis is zero to avoid tilting the character
        positionToLookAt.y = _zero;
        positionToLookAt.z = _cameraRelativeMovement.z;

        Quaternion currentRotation = transform.rotation;

        // Rotate the character if movement is pressed
        if (_isMovementPressed)
        {
        Quaternion targetRotation = Quaternion.LookRotation(positionToLookAt);
        
        transform.rotation = Quaternion.Slerp(currentRotation, targetRotation, _rotationFactorPerFrame * Time.deltaTime);
        }
    }

    void onMovementInput(InputAction.CallbackContext context)
    {
        _currentMovementInput = context.ReadValue<Vector2>();
        _currentMovement.x = _currentMovementInput.x;
        _currentMovement.z = _currentMovementInput.y;
        _currentRunMovement.x = _currentMovementInput.x * _runMultiplier;
        _currentRunMovement.z = _currentMovementInput.y * _runMultiplier;
        _isMovementPressed = _currentMovementInput.x != 0 || _currentMovementInput.y != 0;
    }

    void HandleGravity()
    {
        bool isFalling = _currentMovement.y <= 0.0f || !_isJumpPressed;
        float fallMultiplier = 2.0f;

        // Apply grounded gravity when the character is on the ground
        if (_characterController.isGrounded)
        {
            if (_isJumpAnimating)
            {
                _animator.SetBool(_isJumpingHash, false);
                _isJumpAnimating = false;
                currentJumpResetRoutine = StartCoroutine(JumpResetRoutine());

                // Reset jump count if it reaches 3
                if (_jumpCount == 3)
                {
                    _jumpCount = 0;
                    _animator.SetInteger(_jumpCountHash, _jumpCount);
                }
            }
            _currentMovement.y = _groundedGravity;
            _appliedMovement.y = _groundedGravity;
        }
        else if (isFalling)
        {
            // Handle gravity when the character is falling
            float previousYVelocity = _currentMovement.y;
            _currentMovement.y = _currentMovement.y + (jumpGravities[_jumpCount] * fallMultiplier * Time.deltaTime);
            _appliedMovement.y = Mathf.Max((previousYVelocity + _currentMovement.y) * 0.5f, -20.0f);
        }
        else
        {
            // Handle gravity when the character is in the air but not falling
            float previousYVelocity = _currentMovement.y;
            _currentMovement.y = _currentMovement.y + (jumpGravities[_jumpCount] * Time.deltaTime);
            _appliedMovement.y = (previousYVelocity + _currentMovement.y) * .5f;
        }
    }

    void StartGliding()
    {
        _isGliding = true;
        _animator.SetBool(_isGlidingHash, true);
        _gravity = _glideGravity;
    }

    void StopGlide()
    {
        _isGliding = false;
        _animator.SetBool(_isGlidingHash, false);
        _gravity = -9.8f;
    }

    // Handles the animation states based on movement and run inputs
    void HandleAnimation()
    {
        bool isWalking = _animator.GetBool(_isWalkingHash);
        bool isRunning = _animator.GetBool(_isRunningHash);

        // Update walking animation state
        if (_isMovementPressed && !isWalking)
        {
            _animator.SetBool(_isWalkingHash, true);
        }
        else if (!_isMovementPressed && isWalking)
        {
            _animator.SetBool(_isWalkingHash, false);
        }

        // Update running animation state
        if (_isMovementPressed && _isRunPressed && !isRunning)
        {
            _animator.SetBool(_isRunningHash, true);
        }
        else if ((!_isMovementPressed || !_isRunPressed) && isRunning)
        {
            _animator.SetBool(_isRunningHash, false);
        }
    }

    void Update()
    {  
        HandleRotation();
        HandleAnimation();

        // Move the character based on run or walk state
        if (_isRunPressed)
        {
            _appliedMovement.x = _currentRunMovement.x;
            _appliedMovement.z = _currentRunMovement.z;
        } 
        else
        {
            _appliedMovement.x = _currentMovement.x;
            _appliedMovement.z = _currentMovement.z;
        }
        float verticalMovement = _appliedMovement.y;
        _cameraRelativeMovement.y = verticalMovement;
        _characterController.Move(_cameraRelativeMovement * Time.deltaTime);

        if (_characterController.isGrounded && _isGliding)
        {
            StopGlide();
        }

        _cameraRelativeMovement = ConvertToCameraSpace(_appliedMovement);
        Debug.Log($"_cameraRelativeMovement: {_cameraRelativeMovement}");

        HandleGravity();
        HandleJump();
    }

    Vector3 ConvertToCameraSpace(Vector3 vectorToRotate)
    {
        float currentYValue = vectorToRotate.y;

        Vector3 cameraForward = Camera.main.transform.forward;
        Vector3 cameraRight = Camera.main.transform.right;

        cameraForward.y = 0;
        cameraRight.y = 0;

        cameraForward = cameraForward.normalized;
        cameraRight = cameraRight.normalized;

        Vector3 cameraForwardZProduct = vectorToRotate.z * cameraForward;
        Vector3 cameraRightXProduct = vectorToRotate.x * cameraRight;

        Vector3 vectorRotatedToCameraSpace = cameraForwardZProduct + cameraRightXProduct;
        vectorRotatedToCameraSpace.y = currentYValue;
        return vectorRotatedToCameraSpace;
    }

    // Called when the script instance is being loaded
    void OnEnable()
    {
        _playerInput.CharacterControls.Enable();
    }

    // Called when the script instance is being disabled
    void OnDisable()
    {
        _playerInput.CharacterControls.Disable();
    }
}