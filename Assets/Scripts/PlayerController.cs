using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerController : MonoBehaviour
{
    public float speed = 20.0f;
    public float turnSpeed = 45.0f;

    void Update()
    {
        float forwardInput = GetVerticalInput();
        float horizontalInput = GetHorizontalInput();

        transform.Translate(Vector3.forward * forwardInput * speed * Time.deltaTime);
        transform.Rotate(Vector3.up * horizontalInput * turnSpeed * Time.deltaTime);
    }

    // Support both the newer Input System used by Unity 6 and the legacy
    // Input Manager used by the original course project.
    private float GetVerticalInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null)
        {
            return 0;
        }

        float input = 0;
        if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
        {
            input += 1;
        }
        if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
        {
            input -= 1;
        }
        return input;
#else
        return Input.GetAxis("Vertical");
#endif
    }

    private float GetHorizontalInput()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current == null)
        {
            return 0;
        }

        float input = 0;
        if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
        {
            input += 1;
        }
        if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
        {
            input -= 1;
        }
        return input;
#else
        return Input.GetAxis("Horizontal");
#endif
    }
}
