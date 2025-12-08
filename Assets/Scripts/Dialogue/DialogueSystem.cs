using UnityEngine;
using TMPro;
using System;

public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject dialogueRoot;
    [SerializeField] private TextMeshProUGUI speakerText;
    [SerializeField] private TextMeshProUGUI bodyText;

    private DialogueData currentData;
    private int currentIndex;
    private Action onDialogueComplete;

    // se true, è stato il dialogo a bloccare il player (es. Examine)
    // se false, il player è bloccato da altro (es. Cinematic)
    private bool lockedPlayerControls = false;

    public bool IsOpen => dialogueRoot != null && dialogueRoot.activeSelf;


    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (dialogueRoot != null)
            dialogueRoot.SetActive(false);
    }

    /// <summary>
    /// Avvia un dialogo.
    /// lockPlayerControls = true → disabilita i controlli del player finché il dialogo non termina.
    /// lockPlayerControls = false → lascia inalterato lo stato del player (utile per le cinematic).
    /// </summary>
    public void StartDialogue(DialogueData data, bool lockPlayerControls, Action onComplete = null)
    {
        if (data == null) return;

        currentData = data;
        currentIndex = 0;
        onDialogueComplete = onComplete;
        lockedPlayerControls = lockPlayerControls;

        if (dialogueRoot != null)
            dialogueRoot.SetActive(true);

        if (lockedPlayerControls && FirstPersonController.Instance != null)
            FirstPersonController.Instance.ControlsEnabled = false;

        if (speakerText != null)
            speakerText.text = currentData.speakerName;

        if (bodyText != null && currentData.lines.Length > 0)
            bodyText.text = currentData.lines[0];
    }

    /// <summary>
    /// Chiamato esternamente (es. da PlayerInteraction) quando il giocatore preme Interact.
    /// </summary>
    public void Advance()
    {
        if (!IsOpen || currentData == null)
            return;

        // BLOCCO avanza-dialogo durante cinematic
        if (CinematicSequence.IsAnyCinematicPlaying)
            return;

        currentIndex++;

        if (currentIndex >= currentData.lines.Length)
        {
            EndDialogue();
            return;
        }

        if (bodyText != null)
            bodyText.text = currentData.lines[currentIndex];
    }


    private void EndDialogue()
    {
        if (dialogueRoot != null)
            dialogueRoot.SetActive(false);

        if (lockedPlayerControls && FirstPersonController.Instance != null)
            FirstPersonController.Instance.ControlsEnabled = true;

        onDialogueComplete?.Invoke();

        currentData = null;
        currentIndex = 0;
        onDialogueComplete = null;
        lockedPlayerControls = false;
    }

    /// <summary>
    /// Chiusura forzata (es. quando finisce una cinematic).
    /// Non chiama la callback onDialogueComplete.
    /// </summary>
    public void ForceCloseDialogue()
    {
        if (!IsOpen)
            return;

        if (dialogueRoot != null)
            dialogueRoot.SetActive(false);

        if (lockedPlayerControls && FirstPersonController.Instance != null)
            FirstPersonController.Instance.ControlsEnabled = true;

        currentData = null;
        currentIndex = 0;
        onDialogueComplete = null;
        lockedPlayerControls = false;
    }
}
