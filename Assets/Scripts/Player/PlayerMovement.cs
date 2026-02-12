using System.Diagnostics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] float controlSpeed = 30f;
    Vector2 movementInput;
    [SerializeField] float xClampedRange = 30f;
    [SerializeField] float yClampedRange = 25f;

    [SerializeField] float controlRoll = 20f;
    [SerializeField] float controlPitch = 20f;
    [SerializeField] float rotationSpeed = 20f;

    void Update()
    {
        ProcessTranslation();
        ProcessRotation();
    }

    private void ProcessTranslation()
    {
        float xOffset = movementInput.x * controlSpeed * Time.deltaTime;
        float rawXPos = transform.localPosition.x + xOffset;
        float clampedXPos = Mathf.Clamp(rawXPos, -xClampedRange, xClampedRange);

        float yOffset = movementInput.y * controlSpeed * Time.deltaTime;
        float rawYPos = transform.localPosition.y + yOffset;
        float clampedYPos = Mathf.Clamp(rawYPos, -yClampedRange, yClampedRange);

        transform.localPosition = new Vector3(clampedXPos, clampedYPos, 0f);
    }

    void ProcessRotation()
    {
        Quaternion rotationRollAngle = Quaternion.Euler(0f, 0f, movementInput.x * -controlRoll);
        Quaternion rotationPitchAngle = Quaternion.Euler(movementInput.y * -controlPitch, 0f, 0f);
        transform.localRotation = Quaternion.Lerp(transform.localRotation, rotationRollAngle * rotationPitchAngle, rotationSpeed * Time.deltaTime);
    }

    public void OnMove(InputValue value)
    {
        movementInput = value.Get<Vector2>();
    }
}
