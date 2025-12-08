using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("La scena da caricare quando si clicca 'Start Game'")]
    public string startSceneName = "GameScene";

    // ---------------- BUTTON EVENTS ----------------

    public void StartGame()
    {
        if (string.IsNullOrEmpty(startSceneName))
        {
            Debug.LogError("⚠️ StartGame error: nessun nome scena impostato!");
            return;
        }

        SceneManager.LoadScene(startSceneName);
    }

    public void OpenOptions()
    {
        Debug.Log("Opzioni non implementate.");
        // qui in futuro potremo aprire un pannello 
        // oppure caricare una scena dedicata
    }

    public void QuitGame()
    {
        Debug.Log("Quit pressed → uscita dall'app");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
