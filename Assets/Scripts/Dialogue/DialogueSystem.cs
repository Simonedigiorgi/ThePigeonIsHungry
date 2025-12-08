using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI;
using Sirenix.OdinInspector;

/// <summary>
/// Sistema di dialogo minimal: mostra "Speaker: Linea" in un pannello.
/// Blocca opzionalmente il player finché il dialogo non finisce.
/// </summary>
public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance { get; private set; }

    // ---------- UI ----------
    [BoxGroup("UI")]
    [SerializeField] private GameObject dialogueRoot;  // contiene Image + TMP

    [BoxGroup("UI")]
    [SerializeField, MinValue(0f)]
    private float horizontalPadding = 40f;

    private RectTransform backgroundRect; // ottenuto automaticamente
    private TextMeshProUGUI dialogueText; // ottenuto automaticamente

    // ---------- RUNTIME (hidden) ----------
    [HideInInspector] private DialogueData currentData;
    [HideInInspector] private int currentIndex;
    [HideInInspector] private Action onDialogueComplete;
    [HideInInspector] private bool lockedPlayerControls;

    public bool IsOpen => dialogueRoot != null && dialogueRoot.activeSelf;


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (dialogueRoot == null)
        {
            Debug.LogError("DialogueSystem: dialogueRoot non assegnato!");
            enabled = false;
            return;
        }

        // Prendi automaticamente i componenti necessari
        backgroundRect = dialogueRoot.GetComponent<RectTransform>();
        dialogueText = dialogueRoot.GetComponentInChildren<TextMeshProUGUI>(true);

        if (dialogueText == null)
            Debug.LogError("DialogueSystem: impossibile trovare un TextMeshProUGUI dentro dialogueRoot!");

        dialogueRoot.SetActive(false);
    }


    /// <summary>
    /// Avvia il dialogo.
    /// </summary>
    public void StartDialogue(DialogueData data, bool lockControls, Action onComplete = null)
    {
        if (data == null || data.lines == null || data.lines.Length == 0)
            return;

        currentData = data;
        currentIndex = 0;
        onDialogueComplete = onComplete;
        lockedPlayerControls = lockControls;

        dialogueRoot.SetActive(true);

        if (lockedPlayerControls && FirstPersonController.Instance != null)
            FirstPersonController.Instance.ControlsEnabled = false;

        UpdateDialogueTextAndSize();
    }


    /// <summary>
    /// Aggiorna il testo e ridimensiona il pannello.
    /// </summary>
    private void UpdateDialogueTextAndSize()
    {
        if (currentData == null || dialogueText == null)
            return;

        string speaker = currentData.speakerName;
        string line = currentData.lines[currentIndex];

        dialogueText.text = string.IsNullOrEmpty(speaker)
            ? line
            : $"{speaker}: {line}";

        dialogueText.ForceMeshUpdate();
        float width = dialogueText.preferredWidth + horizontalPadding * 2f;

        if (backgroundRect != null)
        {
            Vector2 size = backgroundRect.sizeDelta;
            size.x = width;
            backgroundRect.sizeDelta = size;

            LayoutRebuilder.ForceRebuildLayoutImmediate(backgroundRect);
        }
    }


    /// <summary>
    /// Avanza alla prossima linea.
    /// </summary>
    public void Advance()
    {
        if (!IsOpen || currentData == null)
            return;

        if (CinematicSequence.IsAnyCinematicPlaying)
            return;

        currentIndex++;

        if (currentIndex >= currentData.lines.Length)
        {
            EndDialogue();
            return;
        }

        UpdateDialogueTextAndSize();
    }


    /// <summary>
    /// Chiude il dialogo normalmente.
    /// </summary>
    private void EndDialogue()
    {
        dialogueRoot.SetActive(false);

        if (lockedPlayerControls && FirstPersonController.Instance != null)
            FirstPersonController.Instance.ControlsEnabled = true;

        onDialogueComplete?.Invoke();

        ResetState();
    }


    /// <summary>
    /// Chiusura forzata senza callback.
    /// </summary>
    public void ForceCloseDialogue()
    {
        if (!IsOpen)
            return;

        dialogueRoot.SetActive(false);

        if (lockedPlayerControls && FirstPersonController.Instance != null)
            FirstPersonController.Instance.ControlsEnabled = true;

        ResetState();
    }


    private void ResetState()
    {
        currentData = null;
        currentIndex = 0;
        onDialogueComplete = null;
        lockedPlayerControls = false;
    }
}
