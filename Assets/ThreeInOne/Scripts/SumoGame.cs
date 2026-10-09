using UnityEngine;

// I'm a Sumo and a Ball: knock every wave of balls away from your goal to win.
public class SumoGame : MonoBehaviour
{
    public SpawnManagerX spawnManager;
    public GameUI ui;

    public int wavesToWin = 5;
    public int lives = 3;

    private int shownWave;
    private float messageUntil;

    public int Goals { get; private set; }

    void Update()
    {
        if (ui.GameEnded)
        {
            return;
        }

        // The spawn manager counts up as soon as it has spawned a wave
        int wave = Mathf.Clamp(spawnManager.waveCount - 1, 1, wavesToWin);
        if (wave != shownWave)
        {
            shownWave = wave;
            ShowMessage(wave == 1 ? "Knock the balls into the far goal!" : "Wave " + wave, 2.5f);
        }

        if (Time.time > messageUntil)
        {
            ui.SetCenterMessage("");
        }

        ui.SetHud("Wave " + wave + " / " + wavesToWin + "     Goals " + Goals + "     Lives " + lives);
    }

    // An enemy ball went into the far goal
    public void PlayerScored()
    {
        Goals++;
        ShowMessage("GOAL!", 1.2f);
    }

    // An enemy ball got into the player's goal
    public void EnemyScored()
    {
        if (ui.GameEnded)
        {
            return;
        }

        lives--;
        if (lives <= 0)
        {
            ui.SetHud("Lives 0");
            ui.ShowLose("Too many balls got into your goal!\nYou scored " + Goals + (Goals == 1 ? " goal" : " goals"));
        }
        else
        {
            ShowMessage("They scored!", 1.2f);
        }
    }

    public void AllWavesCleared()
    {
        ui.ShowWin("You cleared all " + wavesToWin + " waves and scored " + Goals + (Goals == 1 ? " goal!" : " goals!"));
    }

    private void ShowMessage(string message, float seconds)
    {
        ui.SetCenterMessage(message);
        messageUntil = Time.time + seconds;
    }
}
