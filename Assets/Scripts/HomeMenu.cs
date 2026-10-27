
using UnityEngine;
using UnityEngine.SceneManagement;

public class HomeMenu : MonoBehaviour
{
    [Header("Scene Settings")]
    [SerializeField] private string gameSceneName = "GameScene";

    [Header("UI GameObjects")]
    [SerializeField] private GameObject gameScreen;
    [SerializeField] private GameObject settingsScreen;

    // Play Button
    public void PlayGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    // Settings Button
    public void OpenSettings()
    {
        if (gameScreen != null)
            gameScreen.SetActive(false);

        if (settingsScreen != null)
            settingsScreen.SetActive(true);
    }

    // Close Settings Button
    public void CloseSettings()
    {
        if (settingsScreen != null)
            settingsScreen.SetActive(false);

        if (gameScreen != null)
            gameScreen.SetActive(true);
    }

    // Quit Button
    public void QuitGame()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();
    }
}
