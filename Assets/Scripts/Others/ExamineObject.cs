using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Sirenix.OdinInspector;

public class ExamineObject : MonoBehaviour
{
    public static readonly List<ExamineObject> AllExaminables = new();

    // ---------------- INTERACTION ----------------
    [BoxGroup("Interaction")]
    public Collider interactionCollider;

    // ---------------- DIALOGUE ----------------
    [BoxGroup("Dialogue")]
    public DialogueData dialogue;

    // ---------------- SCENE CHANGE ----------------
    [BoxGroup("Scene Change")]
    [Tooltip("Se true, l'interazione carica una nuova scena invece di avviare un dialogo.")]
    public bool changeSceneOnInteract = false;

    [BoxGroup("Scene Change"), ShowIf("changeSceneOnInteract")]
    [Tooltip("Nome della scena da caricare.")]
    public string targetSceneName;

    // ---------------- INTERNAL ----------------
    [ShowInInspector, ReadOnly]
    private bool hasInteracted = false;


    private void Reset()
    {
        interactionCollider = GetComponent<Collider>();
    }

    private void OnEnable() => AllExaminables.Add(this);
    private void OnDisable() => AllExaminables.Remove(this);


    public void Examine()
    {
        if (hasInteracted)
            return;

        hasInteracted = true;

        // ----------------------------------------------
        // CAMBIO SCENA
        // ----------------------------------------------
        if (changeSceneOnInteract)
        {
            if (string.IsNullOrEmpty(targetSceneName))
            {
                Debug.LogWarning($"[ExamineObject] targetSceneName non impostato su '{name}'");
                return;
            }

            // 🔇 FERMA TUTTI GLI AUDIO
            AudioListener.pause = true;

            SceneManager.LoadScene(targetSceneName);
            return;
        }

        // ----------------------------------------------
        // DIALOGO
        // ----------------------------------------------
        if (dialogue == null || DialogueSystem.Instance == null)
            return;

        DialogueSystem.Instance.StartDialogue(dialogue, true, () =>
        {
            hasInteracted = false;
        });
    }
}
