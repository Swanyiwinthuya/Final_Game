using UnityEngine;

public class PlayerCrashDetector : MonoBehaviour
{
    public DrivingGame game;

    private void OnCollisionEnter(Collision other)
    {
        if (other.gameObject.GetComponentInParent<Hazard>() != null)
        {
            game.HitHazard();
        }
    }
}
