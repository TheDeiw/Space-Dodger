using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float _controlSpeed = 30f;
    private Vector2 _movementInput;
    [SerializeField] private float _xClampedRange = 30f;
    [SerializeField] private float _yClampedRange = 25f;

    public float XClampedRange => _xClampedRange;
    public float YClampedRange => _yClampedRange;

    [SerializeField] private float _controlRoll = 20f;
    [SerializeField] private float _controlPitch = 20f;
    [SerializeField] private float _rotationSpeed = 20f;

    [SerializeField] private GameObject _playerModel;

    // When true, Input System callbacks are blocked (agent controls movement)
    private bool _isAgentControlled = false;

    public void SetAgentControlled(bool value)
    {
        _isAgentControlled = value;
    }

    void Update()
    {
        ProcessTranslation();
        ProcessRotation();
    }

    public void SetMovementInput(Vector2 newMovement)
    {
        _movementInput = newMovement;
    }

    private void ProcessTranslation()
    {
        float xOffset = _movementInput.x * _controlSpeed * Time.deltaTime;
        float rawXPos = transform.localPosition.x + xOffset;
        float clampedXPos = Mathf.Clamp(rawXPos, -_xClampedRange, _xClampedRange);

        float yOffset = _movementInput.y * _controlSpeed * Time.deltaTime;
        float rawYPos = transform.localPosition.y + yOffset;
        float clampedYPos = Mathf.Clamp(rawYPos, -_yClampedRange, _yClampedRange);

        transform.localPosition = new Vector3(clampedXPos, clampedYPos, 0f);
    }

    void ProcessRotation()
    {
        Quaternion rotationRollAngle = Quaternion.Euler(0f, 0f, _movementInput.x * -_controlRoll);
        Quaternion rotationPitchAngle = Quaternion.Euler(_movementInput.y * -_controlPitch, 0f, 0f);
        _playerModel.transform.localRotation = Quaternion.Lerp(_playerModel.transform.localRotation, rotationRollAngle * rotationPitchAngle, _rotationSpeed * Time.deltaTime);
    }

    public void OnMove(InputValue value)
    {
        // Block human input when agent is controlling the ship
        if (_isAgentControlled) return;
        _movementInput = value.Get<Vector2>();
    }
}
