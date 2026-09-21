using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The main menu scene. PLAY loads the game scene; QUIT closes the game.
//
// How switching works: every scene listed in File > Build Profiles (Build Settings)
// can be loaded by name. SceneManager.LoadScene unloads the current scene
// (this menu) and loads the new one from scratch.
public class MainMenu : MonoBehaviour
{
    [Tooltip("Name of the gameplay scene file (without .unity).")]
    [SerializeField] string gameSceneName = "Game";

    [SerializeField] Button playButton;
    [SerializeField] Button quitButton;

    void Awake()
    {
        Time.timeScale = 1f; // in case we came back from a paused / game-over state
        playButton.onClick.AddListener(Play);
        quitButton.onClick.AddListener(Quit);
    }

    public void Play()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        // In the editor there's no game window to close, so just stop Play mode.
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
