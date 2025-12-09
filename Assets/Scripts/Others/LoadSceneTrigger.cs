using UnityEngine;
using UnityEngine.SceneManagement;
using Sirenix.OdinInspector;

[RequireComponent(typeof(Collider))]
public class LoadSceneTrigger : MonoBehaviour
{
    [BoxGroup("Settings")]
    [Tooltip("Nome della scena da caricare.")]
    [SerializeField] private string sceneName = "";

    [BoxGroup("Settings")]
    [Tooltip("Se true la scena verrà caricata asincronamente.")]
    [SerializeField] private bool loadAsync = true;

    private bool hasTriggered = false;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered)
            return;

        if (!other.CompareTag("Player"))
            return;

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[LoadSceneTrigger] Nessuna scena specificata.");
            return;
        }

        hasTriggered = true;

        // Blocca il player
        if (FirstPersonController.Instance != null)
            FirstPersonController.Instance.ControlsEnabled = false;

        // Ferma ogni audio nella scena corrente
        AudioListener.pause = true;

        // Carica la scena
        if (loadAsync)
            LoadAsync();
        else
            SceneManager.LoadScene(sceneName);
    }

    private async void LoadAsync()
    {
        var op = SceneManager.LoadSceneAsync(sceneName);
        op.allowSceneActivation = true;

        while (!op.isDone)
            await System.Threading.Tasks.Task.Yield();
    }
}
