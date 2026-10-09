using UnityEngine;
using UnityEngine.SceneManagement;

// Main Menu: lets the player pick one of the three games or exit.
public class MainMenu : MonoBehaviour
{
    public string drivingScene = "Prototype 1";
    public string flyingScene = "Challenge 1";
    public string sumoScene = "Challenge 4";

    void Start()
    {
        // A game may have been left while paused, so make sure time is running.
        Time.timeScale = 1;
        AudioListener.pause = false;
    }

    public void PlayDriving()
    {
        SceneManager.LoadScene(drivingScene);
    }

    public void PlayFlying()
    {
        SceneManager.LoadScene(flyingScene);
    }

    public void PlaySumo()
    {
        SceneManager.LoadScene(sumoScene);
    }

    public void ExitGame()
    {
        Debug.Log("Exit game");
        Application.Quit();
#if UNITY_EDITOR
        // Application.Quit does nothing in the Editor, so stop Play mode instead.
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
