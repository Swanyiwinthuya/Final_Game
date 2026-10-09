using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// HUD and the win / game over screen shared by the games.
public class GameUI : MonoBehaviour
{
    public PauseMenu pauseMenu;
    public Text hudText;
    public Text centerText;
    public GameObject progressBar;
    public RectTransform progressFill;
    public GameObject resultPanel;
    public Text resultTitle;
    public Text resultMessage;
    public GameObject firstButton;

    public Color winColor = new Color(1f, 0.82f, 0.25f);
    public Color loseColor = new Color(1f, 0.35f, 0.3f);

    public bool GameEnded { get; private set; }

    public void SetHud(string text)
    {
        hudText.text = text;
    }

    // Shows how far through the level the player is (0 to 1)
    public void SetProgress(float progress)
    {
        progressBar.SetActive(true);
        progressFill.anchorMax = new Vector2(Mathf.Clamp01(progress), 1);
    }

    public void SetCenterMessage(string text)
    {
        centerText.text = text;
    }

    public void ShowWin(string message)
    {
        ShowResult("YOU WIN!", message, winColor);
    }

    public void ShowLose(string message)
    {
        ShowResult("GAME OVER", message, loseColor);
    }

    public void PlayAgain()
    {
        pauseMenu.Restart();
    }

    public void MainMenu()
    {
        pauseMenu.BackToMainMenu();
    }

    private void ShowResult(string title, string message, Color color)
    {
        if (GameEnded)
        {
            return;
        }

        GameEnded = true;
        pauseMenu.enabled = false; // Escape should not open the pause menu on the result screen
        Time.timeScale = 0;
        centerText.text = "";
        resultTitle.text = title;
        resultTitle.color = color;
        resultMessage.text = message;
        resultPanel.SetActive(true);

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(firstButton);
        }
    }
}
