using UnityEngine;
using Unity.Cinemachine;

public class CameraManager : MonoBehaviour
{
    public CameraState State = CameraState.ThirdPerson;

    [Header("Input")]
    [SerializeField] private InputManager _inputManager;

    [Header("Camera")]
    [SerializeField] private CinemachineCamera _tpsCamera;
    [SerializeField] private CinemachineCamera _fpsCamera;

    private void Start()
    {
	_inputManager.OnSwitchCameraInput += SwitchCamera;
    }

    private void SwitchCamera()
    {
	State = State == CameraState.FirstPerson ? CameraState.ThirdPerson : CameraState.FirstPerson;

	_fpsCamera.gameObject.SetActive(InFirstPersonView());
	_tpsCamera.gameObject.SetActive(!InFirstPersonView());
    }

    private bool InFirstPersonView() => State == CameraState.FirstPerson;

    private void OnDestroy()
    {
	_inputManager.OnSwitchCameraInput -= SwitchCamera;
    }
}
