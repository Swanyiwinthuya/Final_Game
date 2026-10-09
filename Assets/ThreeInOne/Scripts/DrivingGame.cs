using UnityEngine;

// Mad Driver: reach the finish line before the time runs out without wrecking the car.
public class DrivingGame : MonoBehaviour
{
    public Transform player;
    public GameUI ui;
    public Transform[] scenery; // sky and mountains travel with the player

    public float finishZ = 550;
    public float timeLimit = 50;
    public int lives = 3;
    public float hitCooldown = 1.5f;

    private float timeLeft;
    private float startZ;
    private float lastPlayerZ;
    private float safeUntil;
    private float messageUntil;

    void Start()
    {
        timeLeft = timeLimit;
        startZ = player.position.z;
        lastPlayerZ = startZ;
        ui.SetCenterMessage("Reach the finish line!");
        messageUntil = Time.time + 3;
    }

    void Update()
    {
        if (ui.GameEnded)
        {
            return;
        }

        timeLeft -= Time.deltaTime;
        if (Time.time > messageUntil)
        {
            ui.SetCenterMessage("");
        }

        ui.SetProgress((player.position.z - startZ) / (finishZ - startZ));
        ui.SetHud("Lives " + lives + "     Time " + Mathf.CeilToInt(Mathf.Max(timeLeft, 0)));

        if (player.position.z >= finishZ)
        {
            ui.ShowWin("Finished with " + timeLeft.ToString("0.0") + " seconds and " + lives + (lives == 1 ? " life" : " lives") + " left!");
        }
        else if (timeLeft <= 0)
        {
            ui.ShowLose("Out of time!");
        }
        else if (player.position.y < -5)
        {
            ui.ShowLose("You fell off the road!");
        }
    }

    void LateUpdate()
    {
        float delta = player.position.z - lastPlayerZ;
        lastPlayerZ = player.position.z;
        foreach (Transform item in scenery)
        {
            item.position += new Vector3(0, 0, delta);
        }
    }

    public void HitHazard()
    {
        if (ui.GameEnded || Time.time < safeUntil)
        {
            return;
        }

        lives--;
        safeUntil = Time.time + hitCooldown;
        if (lives <= 0)
        {
            ui.SetHud("Lives 0");
            ui.ShowLose("You wrecked the car!");
        }
        else
        {
            ui.SetCenterMessage("CRASH!");
            messageUntil = Time.time + 1;
        }
    }
}
