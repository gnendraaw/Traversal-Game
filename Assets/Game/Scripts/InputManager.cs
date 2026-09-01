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

    private InputAction _moveAction;
    private InputAction _sprintAction;
    private InputAction _jumpAction;
    private InputAction _climbAction;
    private InputAction _cancelClimbAction;

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
        }
    }

    private void Update()
    {
        CheckMovementInput();
        CheckSprintInput();
        CheckJumpInput();
        CheckClimbInput();
        CheckCancelClimbInput();
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
}
