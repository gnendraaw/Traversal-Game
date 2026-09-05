using System;
using UnityEngine;
using Unity.Cinemachine;

public class CameraManager : MonoBehaviour
{
    public Action OnPerspectiveChanged;

    public CameraState State = CameraState.ThirdPerson;

    [Header("Input")]
    [SerializeField] private InputManager _inputManager;

    [Header("Camera")]
    [SerializeField] private CinemachineCamera _tpsCamera;
    [SerializeField] private CinemachinePanTilt _fpsCamera;

    private void Start()
    {
        _inputManager.OnSwitchCameraInput += SwitchCamera;
    }

    private void SwitchCamera()
    {
        State = State == CameraState.FirstPerson ? CameraState.ThirdPerson : CameraState.FirstPerson;

        _fpsCamera.gameObject.SetActive(InFirstPersonView());
        _tpsCamera.gameObject.SetActive(!InFirstPersonView());

        OnPerspectiveChanged?.Invoke();
    }

    private bool InFirstPersonView() => State == CameraState.FirstPerson;

    public void SetThirdPersonCamFOV(float value = 40)
    {
        _tpsCamera.Lens.FieldOfView = value;
    }

    public float GetPanTiltAxis()
    {
        return _fpsCamera.PanAxis.Value;
    }

    private void OnDestroy()
    {
        _inputManager.OnSwitchCameraInput -= SwitchCamera;
    }
}
