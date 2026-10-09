using UnityEngine;

public class SpinPropellerX : MonoBehaviour
{
    [Header("Propeller Rotation")]
    public float spinSpeed = 1000.0f;

    void Update()
    {
        // Spin around the local Z axis
        transform.Rotate(
            Vector3.forward
            * spinSpeed
            * Time.deltaTime,
            Space.Self
        );
    }
}
