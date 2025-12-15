using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Sirenix.OdinInspector;
using UnityEngine.Events;

public class ExamineObject : MonoBehaviour
{
    public static readonly List<ExamineObject> AllExaminables = new();

    // ---------------- INTERACTION ----------------
    [BoxGroup("Interaction")]
    public Collider interactionCollider;

    [BoxGroup("Interaction")]
    [Tooltip("Se true, l'oggetto può essere interagito una sola volta.")]
    public bool interactOnlyOnce = false;

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

    // ---------------- QUEST ----------------
    [BoxGroup("Quest")]
    [Tooltip("Se true, quando l'esame è completo avanza di uno lo step della quest.")]
    public bool advanceQuestOnExamineEnd = false;

    // ---------------- EVENTS ----------------
    [BoxGroup("Events")]
    [Tooltip("Evento chiamato quando il player interagisce con questo oggetto.")]
    public UnityEvent onInteract;

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
        // ❌ già usato e limitato a una sola interazione
        if (hasInteracted && interactOnlyOnce)
            return;

        hasInteracted = true;

        // ✅ evento sempre chiamato su interazione valida
        onInteract?.Invoke();

        // ✅ SOLUZIONE A:
        // se è one-shot, disabilitiamo subito il collider
        // così il raycast non lo colpisce più e il prompt sparisce
        if (interactOnlyOnce && interactionCollider != null)
            interactionCollider.enabled = false;

        // ----------------------------------------------
        // CAMBIO SCENA
        // ----------------------------------------------
        if (changeSceneOnInteract)
        {
            if (string.IsNullOrEmpty(targetSceneName))
            {
                Debug.LogWarning($"[ExamineObject] targetSceneName non impostato su '{name}'");
                if (!interactOnlyOnce)
                    hasInteracted = false;
                return;
            }

            AudioListener.pause = true;
            SceneManager.LoadScene(targetSceneName);
            return;
        }

        // ----------------------------------------------
        // NESSUN DIALOGO → SOLO QUEST
        // ----------------------------------------------
        if (dialogue == null || DialogueSystem.Instance == null)
        {
            HandleQuestUpdate();

            if (!interactOnlyOnce)
                hasInteracted = false;

            return;
        }

        // ----------------------------------------------
        // DIALOGO + QUEST
        // ----------------------------------------------
        DialogueSystem.Instance.StartDialogue(dialogue, true, () =>
        {
            HandleQuestUpdate();

            if (!interactOnlyOnce)
                hasInteracted = false;
        });
    }

    private void HandleQuestUpdate()
    {
        if (!advanceQuestOnExamineEnd)
            return;

        if (QuestManager.Instance == null)
            return;

        QuestManager.Instance.AdvanceStep();
    }
}
