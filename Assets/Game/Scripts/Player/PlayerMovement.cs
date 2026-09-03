using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Cinemachine Camera")] [SerializeField]
    private Transform _cameraTransform;

    [SerializeField] private CameraManager _cameraManager;
    [Header("Inputs")] [SerializeField] private InputManager _input;

    [Header("Walk & Sprint")] [SerializeField]
    private float _sprintSpeed;

    [SerializeField] private float _walkSprintTransition;
    [SerializeField] private float _walkSpeed = 10f;
    [SerializeField] private float _jumpForce = 1000f;
    [SerializeField] private float _rotationSmoothTime = 0.1f;

    [Header("Ground Detection")] [SerializeField]
    private Transform _groundDetector;

    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private float _groundCheckRadius = 0.1f;

    [Header("Step Detection")] [SerializeField]
    private Vector3 _upperStepOffset = Vector3.zero;
    [SerializeField] private float _maxStepHeight;
    [SerializeField] private float _stepCheckDistance = 0.1f;
    [SerializeField] private float _stepForce = 400f;

    [Header("Climb Detection")] [SerializeField]
    private float _climbSpeed;

    [SerializeField] private Transform _climbDetector;
    [SerializeField] private float _climbCheckDistance = 0.1f;
    [SerializeField] private LayerMask _climbableLayer;
    [SerializeField] private Vector3 _climbOffset = Vector3.zero;

    [Header("Crouch")]
    [SerializeField] private float _crouchSpeed;
    
    private PlayerStance _stance;
    private Rigidbody _rigidbody;
    private Vector2 _cachedMoveInput = Vector2.zero;
    private CapsuleCollider _capsuleCollider;
    private Animator _animator;

    private float _speed;
    private float _rotationSmoothVelocity;
    private bool _isGrounded;

    private void Awake()
    {
        HideAndLockCursor();
        _rigidbody = GetComponent<Rigidbody>();
        _speed = _walkSpeed;
        _stance = PlayerStance.Stand;
        _capsuleCollider = GetComponent<CapsuleCollider>();
        _animator = GetComponent<Animator>();
    }

    private void Start()
    {
        _input.OnMoveInput += HandleMoveInput;
        _input.OnSprintInput += Sprint;
        _input.OnJumpInput += Jump;
        _input.OnClimbInput += StartClimb;
        _input.OnCancelClimbInput += CancelClimb;
        _input.OnCrouchInput += Crouch;

        _cameraManager.OnPerspectiveChanged += ChangePerspective;
    }

    private void Update()
    {
        CheckIsGrounded();
    }

    private void FixedUpdate()
    {
        Move(_cachedMoveInput);
        CheckStep();
        SyncBodyRotateWithCamera();
    }

    private void SyncBodyRotateWithCamera()
    {
        if (_cameraManager.State != CameraState.FirstPerson) return;
        float panAxisValue = _cameraManager.GetPanTiltAxis();
        transform.rotation = Quaternion.Euler(0f, panAxisValue, 0f);
    }

    private void HideAndLockCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void HandleMoveInput(Vector2 input)
    {
        _cachedMoveInput = input;
    }

    private void CheckIsGrounded()
    {
        if (Physics.CheckSphere(_groundDetector.position, _groundCheckRadius, _groundLayer))
            _isGrounded = true;
        else
            _isGrounded = false;

        _animator.SetBool("isGrounded", _isGrounded);
    }

    private void CheckStep()
    {
        bool notMoving = _cachedMoveInput.sqrMagnitude <= 0.01f;
        if (notMoving) return;
    
        Vector3 lowerRayOrigin = _groundDetector.position;
        Vector3 upperRayOrigin = _groundDetector.position + _upperStepOffset;
    
        Vector3 forwardDirection = transform.forward;
    
        if (!Physics.Raycast(lowerRayOrigin, forwardDirection, out RaycastHit hit, _stepCheckDistance)) return;
        if (Physics.Raycast(upperRayOrigin, forwardDirection, _stepCheckDistance)) return;
        
        Vector3 downRayOrigin = upperRayOrigin + (forwardDirection * _stepCheckDistance);
        if (!Physics.Raycast(downRayOrigin, Vector3.down, _upperStepOffset.y)) return;
        
        float stepHeightDifference = hit.point.y - lowerRayOrigin.y;
        Vector3 targetPosition = _rigidbody.position + new Vector3(0f, stepHeightDifference + 0.1f, 0f);
        _rigidbody.MovePosition(targetPosition);
    }

    private void Move(Vector2 inputAxis)
    {
        bool isClimbing = _stance == PlayerStance.Climb;
        bool isStanding = _stance == PlayerStance.Stand;
        bool isCrouching =  _stance == PlayerStance.Crouch;
        Vector3 moveDirection = Vector3.zero;

        if (isStanding || isCrouching)
        {
            Vector3 currentVelocity = new Vector3(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);
            _animator.SetFloat("velocity", currentVelocity.magnitude * inputAxis.magnitude);
            _animator.SetFloat("velocityX", currentVelocity.magnitude * inputAxis.x);
            _animator.SetFloat("velocityZ", currentVelocity.magnitude * inputAxis.y);
                
            switch (_cameraManager.State)
            {
                case CameraState.ThirdPerson:
                    if (inputAxis.magnitude >= 0.1f)
                    {
                        float targetAngle = Mathf.Atan2(inputAxis.x, inputAxis.y) * Mathf.Rad2Deg +
                                            _cameraTransform.eulerAngles.y;
                        float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref _rotationSmoothVelocity, _rotationSmoothTime);
                        
                        transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
                        
                        Vector3 velocity = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward * _speed;
                        _rigidbody.AddForce(velocity * Time.fixedDeltaTime);
                    }
                    break;
                
                case CameraState.FirstPerson:
                    if (inputAxis.magnitude >= 0.1f)
                    {
                        Vector3 verticalDirection = inputAxis.y * transform.forward;
                        Vector3 horizontalDirection = inputAxis.x * transform.right;
                        
                        moveDirection = verticalDirection + horizontalDirection;
                        Vector3 velocity = moveDirection * _speed;
                        
                        _rigidbody.AddForce(velocity * Time.fixedDeltaTime);
                    }
                    break;
            }

            return;
        }

        if (isClimbing)
        {
            Vector3 horizontal = inputAxis.x * transform.right;
            Vector3 vertical = inputAxis.y * transform.up;
            moveDirection = horizontal + vertical;
            _rigidbody.AddForce(moveDirection * Time.deltaTime * _climbSpeed);
        }
    }

    private void Sprint(bool isSprinting)
    {
        if (isSprinting)
        {
            if (_speed < _sprintSpeed)
            {
                _speed = Mathf.Lerp(_speed, _sprintSpeed, _walkSprintTransition * Time.deltaTime);
            }
        }
        else
        {
            if (_speed > _walkSpeed)
            {
                _speed = Mathf.Lerp(_speed, _walkSpeed, _walkSprintTransition * Time.deltaTime);
            }
        }
    }

    private void Jump()
    {
        if (!_isGrounded) return;

        Vector3 jumpDirection = Vector3.up;
        _animator.SetTrigger("jump");
        _rigidbody.AddForce(jumpDirection * _jumpForce * Time.deltaTime);
    }

    private void StartClimb()
    {
        bool isInFrontOfClimbWall = Physics.Raycast(_climbDetector.position, transform.forward, out RaycastHit hit,
            _climbCheckDistance, _climbableLayer);
        bool isNotClimbing = _stance != PlayerStance.Climb;
        if (isInFrontOfClimbWall && isNotClimbing)
        {
            _cameraManager.SetThirdPersonCamFOV(70f);
            Vector3 offset = (transform.forward * _climbOffset.z) + (Vector3.up * _climbOffset.y);
            _stance = PlayerStance.Climb;
            _rigidbody.useGravity = false;
            transform.position = hit.point - offset;
        }
    }

    private void CancelClimb()
    {
        if (_stance != PlayerStance.Climb) return;
        _cameraManager.SetThirdPersonCamFOV(40f);
        _stance = PlayerStance.Stand;
        _rigidbody.useGravity = true;
        transform.position -= transform.forward;
    }

    private void ChangePerspective()
    {
        _animator.SetTrigger("switchPOV");
    }

    private void Crouch()
    {
        if (_stance == PlayerStance.Stand)
        {
            _stance = PlayerStance.Crouch;
            _speed = _crouchSpeed;
            _animator.SetBool("isCrouch", true);

            _capsuleCollider.height = 1.3f;
            _capsuleCollider.center = Vector3.up * 0.66f;

            return;
        }

        if (_stance == PlayerStance.Crouch)
        {
            _stance = PlayerStance.Stand;
            _speed = _walkSpeed;
            _animator.SetBool("isCrouch", false);

            _capsuleCollider.height = 1.8f;
            _capsuleCollider.center = Vector3.up * 0.99f;
        }
    }

    private void OnDestroy()
    {
        _input.OnMoveInput -= HandleMoveInput;
        _input.OnSprintInput -= Sprint;
        _input.OnJumpInput -= Jump;
        _input.OnClimbInput -= StartClimb;
        _input.OnCancelClimbInput -= CancelClimb;
        _input.OnCrouchInput -= Crouch;

        _cameraManager.OnPerspectiveChanged -= ChangePerspective;
    }
}
