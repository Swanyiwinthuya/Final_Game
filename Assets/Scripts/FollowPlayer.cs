using UnityEngine;

public class FollowPlayer : MonoBehaviour
{
    public GameObject player;
    private Vector3 offset = new Vector3(0, 6, -11);

    void LateUpdate()
    {
        // Offset the camera behind the player by adding to the player's position
        transform.position = player.transform.position + offset;
    }
}
