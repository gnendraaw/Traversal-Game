using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    public Action<Vector2> OnMoveInput;

    private InputAction moveAction;

    private void Start()
    {
        if (InputSystem.actions)
        {
            moveAction = InputSystem.actions.FindAction("Player/Move");
            moveAction.Enable();
        }
    }

    private void Update()
    {
        CheckMovementInput();
    }

    private void CheckMovementInput() 
    {
        Vector2 inputAxis = moveAction.ReadValue<Vector2>();
        if (OnMoveInput != null) {
            OnMoveInput.Invoke(inputAxis);
        }
    }
}
