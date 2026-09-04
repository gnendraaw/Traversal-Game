using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public Action<Vector2> OnMoveInput;
    public Action<bool> OnSprintInput;
    public Action OnJumpInput;
    public Action OnClimbInput;
    public Action OnCancelClimbInput;
    public Action OnSwitchCameraInput;
    public Action OnCrouchInput;
    public Action OnGlideInput;
    public Action OnCancelGlideInput;

    private InputAction _moveAction;
    private InputAction _sprintAction;
    private InputAction _jumpAction;
    private InputAction _climbAction;
    private InputAction _cancelClimbAction;
    private InputAction _switchCameraAction;
    private InputAction _crouchAction;
    private InputAction _glideAction;
    private InputAction _cancelGlideAction;

    private void Start()
    {
        if (InputSystem.actions)
        {
            _moveAction = InputSystem.actions.FindAction("Player/Move");
            _moveAction.Enable();

            _sprintAction = InputSystem.actions.FindAction("Player/Sprint");
            _sprintAction.Enable();

            _jumpAction = InputSystem.actions.FindAction("Player/Jump");
            _jumpAction.Enable();

            _climbAction = InputSystem.actions.FindAction("Player/Climb");
            _climbAction.Enable();

            _cancelClimbAction = InputSystem.actions.FindAction("Player/CancelClimb");
            _cancelClimbAction.Enable();

            _switchCameraAction = InputSystem.actions.FindAction("Player/SwitchCamera");
            _switchCameraAction.Enable();

            _crouchAction = InputSystem.actions.FindAction("Player/Crouch");
            _crouchAction.Enable();


            _glideAction = InputSystem.actions.FindAction("Player/Glide");
            _glideAction.Enable();

            _cancelGlideAction = InputSystem.actions.FindAction("Player/CancelGlide");
            _cancelGlideAction.Enable();
        }
    }

    private void Update()
    {
        CheckMovementInput();
        CheckSprintInput();
        CheckJumpInput();
        CheckClimbInput();
        CheckCancelClimbInput();
        CheckSwitchCameraInput();
        CheckCrouchInput();
        CheckGlideInput();
        CheckCancelGlideInput();
    }

    private void CheckMovementInput()
    {
        Vector2 inputAxis = _moveAction.ReadValue<Vector2>();
        OnMoveInput?.Invoke(inputAxis);
    }

    private void CheckSprintInput()
    {
        bool isSprinting = _sprintAction.ReadValue<float>() > 0;
        OnSprintInput?.Invoke(isSprinting);
    }

    private void CheckJumpInput()
    {
        bool jumpInputPerformed = _jumpAction.WasPerformedThisFrame();
        if (jumpInputPerformed) OnJumpInput?.Invoke();
    }

    private void CheckClimbInput()
    {
        bool isClimbInputPerformed = _climbAction.WasPerformedThisFrame();
        if (isClimbInputPerformed) OnClimbInput?.Invoke();
    }

    private void CheckCancelClimbInput()
    {
        bool isCancelClimbInputPerformed = _cancelClimbAction.WasPerformedThisFrame();
        if (isCancelClimbInputPerformed) OnCancelClimbInput?.Invoke();
    }

    private void CheckSwitchCameraInput()
    {
        bool isSwitchCameraInputPerformed = _switchCameraAction.WasPerformedThisFrame();
        if (isSwitchCameraInputPerformed) OnSwitchCameraInput?.Invoke();
    }

    private void CheckCrouchInput()
    {
        bool isCrouchActionPerformed = _crouchAction.WasPerformedThisFrame();
        if (isCrouchActionPerformed) OnCrouchInput?.Invoke();
    }

    private void CheckGlideInput()
    {
        bool isGlideActionPerformed = _glideAction.WasPerformedThisFrame();
        if (isGlideActionPerformed) OnGlideInput?.Invoke();
    }

    private void CheckCancelGlideInput()
    {
        bool isCancelGlideActionPerformed = _cancelGlideAction.WasPerformedThisFrame();
        if (isCancelGlideActionPerformed) OnCancelGlideInput?.Invoke();
    }
}
