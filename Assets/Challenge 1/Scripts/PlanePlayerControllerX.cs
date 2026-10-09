using UnityEngine;
using UnityEngine.InputSystem;

public class PlanePlayerControllerX : MonoBehaviour
{
    [Header("Plane Movement")]
    public float speed = 15.0f;

    [Header("Pitch Control")]
    public float pitchSpeed = 80.0f;
    public float maximumPitch = 35.0f;

    private float verticalInput;
    private float currentPitch;

    void Start()
    {
        currentPitch = transform.eulerAngles.x;

        if (currentPitch > 180)
        {
            currentPitch -= 360;
        }
    }

    void Update()
    {
        verticalInput = 0.0f;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.upArrowKey.isPressed)
            {
                verticalInput = -1.0f;
            }
            else if (Keyboard.current.downArrowKey.isPressed)
            {
                verticalInput = 1.0f;
            }
        }

        // Move the plane forward
        transform.Translate(
            Vector3.forward * speed * Time.deltaTime
        );

        // Change the plane's pitch faster
        currentPitch += verticalInput * pitchSpeed * Time.deltaTime;

        // Prevent the plane from rotating too far up or down
        currentPitch = Mathf.Clamp(
            currentPitch,
            -maximumPitch,
            maximumPitch
        );

        transform.rotation = Quaternion.Euler(
            currentPitch,
            transform.eulerAngles.y,
            transform.eulerAngles.z
        );
    }
}