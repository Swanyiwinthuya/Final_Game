using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// In-Game Menu: press Escape to pause, then Resume, Restart or go Back to Main Menu.
public class PauseMenu : MonoBehaviour
{
    public GameObject pausePanel;
    public GameObject firstButton;
    public string mainMenuScene = "MainMenu";

    public bool IsPaused { get; private set; }

    void Start()
    {
        SetPaused(false);
    }

    void Update()
    {
        if (EscapePressedThisFrame())
        {
            SetPaused(!IsPaused);
        }
    }

    public void Resume()
    {
        SetPaused(false);
    }

    public void Restart()
    {
        SetPaused(false);
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void BackToMainMenu()
    {
        SetPaused(false);
        SceneManager.LoadScene(mainMenuScene);
    }

    public void SetPaused(bool paused)
    {
        IsPaused = paused;
        Time.timeScale = paused ? 0 : 1;
        AudioListener.pause = paused;
        pausePanel.SetActive(paused);

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(paused ? firstButton : null);
        }
    }

    // Support Unity 6's Input System as well as the original Input Manager.
    private bool EscapePressedThisFrame()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Escape);
#endif
    }
}
