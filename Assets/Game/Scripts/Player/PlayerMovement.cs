using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Cinemachine Camera")]
    [SerializeField]
    private Transform _cameraTransform;

    [SerializeField] private CameraManager _cameraManager;
    [Header("Inputs")][SerializeField] private InputManager _input;

    [Header("Audio")]
    [SerializeField] private PlayerAudioManager _audioManager;

    [Header("Walk & Sprint")]
    [SerializeField] private float _sprintSpeed;
    [SerializeField] private float _walkSpeed = 10f;
    [SerializeField] private float _acceleration = 10f;
    [SerializeField] private float _walkSprintTransition;
    [SerializeField] private float _jumpForce = 1000f;
    [SerializeField] private float _rotationSmoothTime = 0.1f;

    [Header("Ground Detection")]
    [SerializeField]
    private Transform _groundDetector;

    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private float _groundCheckRadius = 0.1f;

    [Header("Step Detection")]
    [SerializeField]
    private Vector3 _upperStepOffset = Vector3.zero;
    [SerializeField] private float _maxStepHeight;
    [SerializeField] private float _stepCheckDistance = 0.1f;
    [SerializeField] private float _stepForce = 400f;

    [Header("Climb Detection")]
    [SerializeField]
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

    [Header("Glide")]
    [SerializeField] private float _glideSpeed = 70f;
    [SerializeField] private float _airDrag = 5f;
    [SerializeField] private Vector3 _glideRotationSpeed;
    [SerializeField] private float _minGlideRotationX;
    [SerializeField] private float _maxGlideRotationX;

    [Header("Punch Detection")]
    [SerializeField] private Transform _hitDetector;
    [SerializeField] private float _hitDetectorRadius;
    [SerializeField] private LayerMask _hitLayer;

    private bool _isGrounded;

    private float _speed;
    private float _rotationSmoothVelocity;

    private bool _isPunching;
    private int _combo = 0;

    private float _currentAnimVelX;
    private float _currentAnimVelZ;
    private float _animVelXRef;
    private float _animVelZRef;
    private float _animSmoothTime = 1f;

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
        _input.OnGlideInput += StartGlide;
        _input.OnCancelGlideInput += CancelGlide;
        _input.OnAttackInput += Punch;

        _cameraManager.OnPerspectiveChanged += ChangePerspective;
    }

    private void FixedUpdate()
    {
        Move(_cachedMoveInput);
        CheckIsGrounded();
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
        _isGrounded = Physics.CheckSphere(_groundDetector.position, _groundCheckRadius, _groundLayer);

        if (!_isGrounded && _stance == PlayerStance.Glide)
        {
            CancelGlide();
            return;
        }

        if (!_isGrounded && _stance == PlayerStance.Crouch)
        {
            Crouch();
            return;
        }

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
        if (_isPunching) return;

        bool isStanding = _stance == PlayerStance.Stand;
        bool isClimbing = _stance == PlayerStance.Climb;
        bool isCrouching = _stance == PlayerStance.Crouch;
        bool isGliding = _stance == PlayerStance.Glide;

        if (isStanding || isCrouching)
        {
            Vector3 currentVelocity = _rigidbody.linearVelocity;
            Vector3 currentHorizontal = new Vector3(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);
            Vector3 velocity = Vector3.zero;

            Vector3 desiredDirection = Vector3.zero;

            switch (_cameraManager.State)
            {
                case CameraState.ThirdPerson:
                    float targetAngle = Mathf.Atan2(inputAxis.x, inputAxis.y) * Mathf.Rad2Deg + _cameraTransform.eulerAngles.y;
                    desiredDirection = Quaternion.Euler(Vector3.up * targetAngle).normalized * Vector3.forward;

                    if (inputAxis.magnitude > 0.01f)
                        RotateTowards(targetAngle, _rotationSmoothTime, ref _rotationSmoothVelocity);

                    break;

                case CameraState.FirstPerson:
                    desiredDirection = (inputAxis.x * transform.right) + (inputAxis.y * transform.forward).normalized;
                    break;
            }

            MoveTowards(desiredDirection);
            UpdateStandAnimator();
        }

        if (isClimbing)
        {
            Vector3 horizontal = inputAxis.x * transform.right;
            Vector3 vertical = inputAxis.y * transform.up;

            ClimbTowards(horizontal + vertical);
            UpdateClimbAnimator();
        }

        if (isGliding)
        {
            Vector3 rotationDegree = transform.eulerAngles;

            rotationDegree.x += _glideRotationSpeed.x * Time.fixedDeltaTime * inputAxis.y;
            rotationDegree.x = Mathf.Clamp(rotationDegree.x, _minGlideRotationX, _maxGlideRotationX);

            rotationDegree.y += _glideRotationSpeed.y * Time.fixedDeltaTime * inputAxis.x;
            rotationDegree.z += _glideRotationSpeed.z * Time.fixedDeltaTime * inputAxis.x;

            transform.rotation = Quaternion.Euler(rotationDegree);
        }
    }

    private void MoveTowards(Vector3 desiredDirection)
    {
        Vector3 velocity = _cachedMoveInput.magnitude > 0.01f
            ? desiredDirection.normalized * _speed
            : Vector3.zero;

        Vector3 currentVelocity = new Vector3(_rigidbody.linearVelocity.x, 0f, _rigidbody.linearVelocity.z);

        float t = 1f - Mathf.Exp(-_acceleration * Time.fixedDeltaTime);
        Vector3 smoothVelocity = Vector3.Lerp(currentVelocity, velocity, t);

        _rigidbody.linearVelocity = new Vector3(smoothVelocity.x, _rigidbody.linearVelocity.y, smoothVelocity.z);
    }

    private void UpdateStandAnimator()
    {
        Vector3 horizontal = _cachedMoveInput.x * transform.right;
        Vector3 forward = _cachedMoveInput.y * transform.forward;
        Vector3 velocity = (horizontal + forward).normalized * _speed;

        _animator.SetFloat("velocity", Mathf.Abs(velocity.magnitude));
        _animator.SetFloat("velocityX", velocity.magnitude * _cachedMoveInput.x);
        _animator.SetFloat("velocityZ", velocity.magnitude * _cachedMoveInput.y);
    }

    private void ClimbTowards(Vector3 desiredDirection)
    {
        Vector3 velocity = desiredDirection.normalized * _climbSpeed;
        _rigidbody.linearVelocity = velocity;
    }

    private void UpdateClimbAnimator()
    {
        Vector3 horizontal = _cachedMoveInput.x * transform.right;
        Vector3 vertical = _cachedMoveInput.y * transform.up;
        Vector3 velocity = (horizontal + vertical).normalized * _speed;

        _animator.SetFloat("climbVelocityX", velocity.magnitude * _cachedMoveInput.x);
        _animator.SetFloat("climbVelocityY", velocity.magnitude * _cachedMoveInput.y);
    }


    private void RotateTowards(float targetAngle, float rotationSpeed, ref float smoothRotationVelocity)
    {
        float smoothAngle = Mathf.SmoothDampAngle(_rigidbody.transform.eulerAngles.y, targetAngle, ref smoothRotationVelocity, rotationSpeed);
        _rigidbody.MoveRotation(Quaternion.Euler(0f, smoothAngle, 0f));
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

        Vector3 force = Vector3.up * _jumpForce;
        _rigidbody.AddForce(force, ForceMode.Impulse);

        _animator.SetTrigger("jump");
    }

    private void StartClimb()
    {
        bool isNotClimbing = _stance != PlayerStance.Climb;

        bool isInFrontOfClimbWall = Physics.Raycast(
            _climbDetector.position,
            transform.forward,
            out RaycastHit hit,
            _climbCheckDistance, _climbableLayer
        );

        if (isInFrontOfClimbWall && isNotClimbing && _isGrounded)
        {
            _rigidbody.useGravity = false;
            _cameraManager.SetThirdPersonCamFOV(70f);
            _stance = PlayerStance.Climb;
            _capsuleCollider.center = Vector3.up * 1.3f;

            Vector3 offset = (transform.forward * _climbOffset.z) + (Vector3.up * _climbOffset.y);
            transform.position = hit.point - offset;

            _animator.SetBool("isClimbing", true);
        }
    }

    private void CancelClimb()
    {
        if (_stance != PlayerStance.Climb) return;

        _rigidbody.useGravity = true;
        _cameraManager.SetThirdPersonCamFOV(40f);
        _stance = PlayerStance.Stand;
        _capsuleCollider.center = Vector3.up * 0.9f;

        transform.position -= transform.forward;

        _animator.SetBool("isClimbing", false);
    }

    private void ChangePerspective()
    {
        _animator.SetTrigger("switchPOV");
    }

    private void Crouch()
    {
        if (_stance == PlayerStance.Stand)
        {
            _animator.SetBool("isCrouch", true);
            _stance = PlayerStance.Crouch;
            _speed = _crouchSpeed;

            Debug.Log($"Player stance: {_stance}");
            return;
        }

        if (_stance == PlayerStance.Crouch)
        {
            _animator.SetBool("isCrouch", false);
            _stance = PlayerStance.Stand;
            _speed = _walkSpeed;

            Debug.Log($"Player stance: {_stance}");
            return;
        }
    }

    private void StartGlide()
    {
        if (_stance != PlayerStance.Glide && !_isGrounded)
        {
            _stance = PlayerStance.Glide;
            _animator.SetBool("isGliding", true);
            _audioManager.PlayGlideSFX();
        }
    }

    private void Glide()
    {
        if (_stance != PlayerStance.Glide) return;

        float lift = transform.eulerAngles.x;
        Vector3 upForce = transform.up * (lift + _airDrag);
        Vector3 forwardForce = transform.forward * _glideSpeed;
        Vector3 totalForce = upForce + forwardForce;

        _rigidbody.AddForce(totalForce * Time.fixedDeltaTime);
    }

    private void CancelGlide()
    {
        if (_stance == PlayerStance.Glide)
        {
            _stance = PlayerStance.Stand;
            _animator.SetBool("isGliding", false);
            _audioManager.StopGlideSFX();
        }
    }

    private void Punch()
    {
        if (!_isPunching && _stance == PlayerStance.Stand)
        {
            _isPunching = true;

            _combo += 1;
            if (_combo > 3) _combo = 1;

            _animator.SetInteger("combo", _combo);
            _animator.SetTrigger("punch");
        }
    }

    // INFO: Called by the attack animation event.
    private void EndPunch()
    {
        _isPunching = false;
    }

    private void Hit()
    {
        Collider[] hitObjects = Physics.OverlapSphere(_hitDetector.position, _hitDetectorRadius, _hitLayer);

        for (int i = 0; i < hitObjects.Length; i++)
        {
            if (hitObjects[i] == null) continue;
            Destroy(hitObjects[i].gameObject);
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
        _input.OnGlideInput -= StartGlide;
        _input.OnCancelGlideInput -= CancelGlide;
        _input.OnAttackInput -= Punch;

        _cameraManager.OnPerspectiveChanged -= ChangePerspective;
    }
}
