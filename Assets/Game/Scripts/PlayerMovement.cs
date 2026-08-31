using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private InputManager _input;
    [SerializeField] private float _moveSpeed = 10f;

    private Vector3 velocity = Vector3.zero;
    private Rigidbody _rigidbody;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    private void Start()
    {
        _input.OnMoveInput += Move;
    }

    private void Update()
    {
        transform.position += velocity * Time.deltaTime;
    }

    private void Move(Vector2 inputAxis)
    {
        Vector3 moveDirection = new Vector3(inputAxis.x, 0f, inputAxis.y);
        Debug.Log("Move direction: " + moveDirection);

	_rigidbody.AddForce(moveDirection * _moveSpeed * Time.deltaTime);
    }

    private void OnDestroy()
    {
        _input.OnMoveInput -= Move;
    }
}
