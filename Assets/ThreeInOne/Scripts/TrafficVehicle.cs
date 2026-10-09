using UnityEngine;

// Oncoming traffic: starts driving when the player gets close.
public class TrafficVehicle : MonoBehaviour
{
    public Transform player;
    public float speed = 10;
    public float startDistance = 120;

    void Update()
    {
        float distance = transform.position.z - player.position.z;
        if (distance < startDistance)
        {
            transform.Translate(Vector3.forward * speed * Time.deltaTime);
        }
        if (distance < -40)
        {
            gameObject.SetActive(false);
        }
    }
}
