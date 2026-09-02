using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [Header("Cinemachine Camera")]
    [SerializeField] private Transform _cameraTransform;
    [SerializeField] private CameraManager _cameraManager;

    [Header("Inputs")]
    [SerializeField] private InputManager _input;

    [Header("Walk & Sprint")]
    [SerializeField] private float _sprintSpeed;
    [SerializeField] private float _walkSprintTransition;
    [SerializeField] private float _walkSpeed = 10f;
    [SerializeField] private float _jumpForce = 1000f;
    [SerializeField] private float _rotationSmoothTime = 0.1f;

    [Header("Ground Detection")]
    [SerializeField] private Transform _groundDetector;
    [SerializeField] private LayerMask _groundLayer;
    [SerializeField] private float _groundCheckRadius = 0.1f;

    [Header("Step Detection")]
    [SerializeField] private Vector3 _upperStepOffset = Vector3.zero;
    [SerializeField] private float _stepCheckDistance = 0.1f;
    [SerializeField] private float _stepForce = 400f;

    [Header("Climb Detection")]
    [SerializeField] private float _climbSpeed;
    [SerializeField] private Transform _climbDetector;
    [SerializeField] private float _climbCheckDistance = 0.1f;
    [SerializeField] private LayerMask _climbableLayer;
    [SerializeField] private Vector3 _climbOffset = Vector3.zero;

    private float _speed;
    private bool _isGrounded;
    private PlayerStance _stance;

    private float _rotationSmoothVelocity;

    private Rigidbody _rigidbody;

    private void Awake()
    {
        HideAndLockCursor();

        _rigidbody = GetComponent<Rigidbody>();
        _speed = _walkSpeed;
        _stance = PlayerStance.Stand;
    }

    private void Start()
    {
        _input.OnMoveInput += Move;
        _input.OnSprintInput += Sprint;
        _input.OnJumpInput += Jump;
        _input.OnClimbInput += StartClimb;
        _input.OnCancelClimbInput += CancelClimb;
    }

    private void Update()
    {
        CheckIsGrounded();
        CheckStep();
    }

    private void HideAndLockCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void CheckIsGrounded()
    {
        if (Physics.CheckSphere(_groundDetector.position, _groundCheckRadius, _groundLayer))
            _isGrounded = true;
        else _isGrounded = false;
    }

    private void CheckStep()
    {
        bool isHittingLowerStep = Physics.Raycast(
            _groundDetector.position,
            transform.forward,
            _stepCheckDistance
        );
        bool isHittingUpperStep = Physics.Raycast(
            _groundDetector.position + _upperStepOffset,
            transform.forward,
            _stepCheckDistance
        );
        Debug.Log($"IsHittingLowerStep: {isHittingLowerStep}");
        Debug.Log($"IsHittingUpperStep: {isHittingUpperStep}");

        if (isHittingLowerStep && !isHittingUpperStep)
            _rigidbody.AddForce(0f, _stepForce, 0);
    }

    private void Move(Vector2 inputAxis)
    {
        bool isClimbing = _stance == PlayerStance.Climb;
        bool isStanding = _stance == PlayerStance.Stand;
        Vector3 moveDirection = Vector3.zero;

        if (isStanding)
        {
            switch (_cameraManager.State)
            {
                case CameraState.ThirdPerson:
                    if (inputAxis.magnitude >= 0.1f)
                    {
                        float rotationAngle = Mathf.Atan2(inputAxis.x, inputAxis.y) * Mathf.Rad2Deg + _cameraTransform.eulerAngles.y;
                        float smoothAngle = Mathf.SmoothDampAngle(transform.eulerAngles.y, rotationAngle, ref _rotationSmoothVelocity, _rotationSmoothTime);
                        transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);

                        moveDirection = Quaternion.Euler(0f, rotationAngle, 0f) * Vector3.forward;
                        _rigidbody.AddForce(moveDirection * (_speed * Time.deltaTime));
                    }

                    break;

                case CameraState.FirstPerson:
                    if (inputAxis.magnitude >= 0.1f)
                    {
                        // FIXME: After rotating player's y angle, the FirstPersonCamera's Pan
                        // value didn't reset back to 0f. causing weird panning limit.
                        transform.rotation = Quaternion.Euler(0f, _cameraTransform.eulerAngles.y, 0f);
                        Vector3 verticalDirection = inputAxis.y * transform.forward;
                        Vector3 horizontalDirection = inputAxis.x * transform.right;
                        moveDirection = verticalDirection + horizontalDirection;
                        _rigidbody.AddForce(moveDirection * (Time.deltaTime * _speed));
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
        Debug.Log($"Is Sprinting: {isSprinting}");
        if (isSprinting)
        {
            if (_speed < _sprintSpeed) _speed += _walkSprintTransition * Time.deltaTime;
        }
        else
        {
            if (_speed > _walkSpeed) _speed -= _walkSprintTransition * Time.deltaTime;
        }
    }

    private void Jump()
    {
        if (!_isGrounded) return;
        Vector3 jumpDirection = Vector3.up;
        _rigidbody.AddForce(jumpDirection * _jumpForce * Time.deltaTime);
    }

    private void StartClimb()
    {
        bool isInFrontOfClimbWall = Physics.Raycast
        (
            _climbDetector.position,
            transform.forward,
            out RaycastHit hit,
            _climbCheckDistance,
            _climbableLayer
        );

        bool isNotClimbing = _stance != PlayerStance.Climb;
        if (isInFrontOfClimbWall && isNotClimbing)
        {
            Vector3 offset = (transform.forward * _climbOffset.z) + (Vector3.up * _climbOffset.y);
            _stance = PlayerStance.Climb;
            _rigidbody.useGravity = false;
            transform.position = hit.point - offset;
        }
    }

    private void CancelClimb()
    {
        if (_stance != PlayerStance.Climb) return;

        _stance = PlayerStance.Stand;
        _rigidbody.useGravity = true;
        transform.position -= transform.forward;
    }

    private void OnDestroy()
    {
        _input.OnMoveInput -= Move;
        _input.OnSprintInput -= Sprint;
        _input.OnJumpInput -= Jump;
        _input.OnClimbInput -= StartClimb;
        _input.OnCancelClimbInput -= CancelClimb;
    }
}
