using UnityEngine;

public class FollowPlayerX : MonoBehaviour
{

    public GameObject plane;


    private Vector3 offset = new Vector3(30.0f, 0.0f, 10.0f);

    void LateUpdate()
    {

        if (plane != null)
        {
            transform.position = plane.transform.position + offset;
        }
    }
}