using UnityEngine;
using TMPro;
using System;
using UnityEngine.UI; // per LayoutRebuilder

public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject dialogueRoot;          // GameObject con Image
    [SerializeField] private RectTransform dialogueBackground; // RectTransform dell'Image (può essere lo stesso di dialogueRoot)
    [SerializeField] private TextMeshProUGUI dialogueText;     // unico TMP con nome + testo

    [Header("Layout")]
    [SerializeField] private float horizontalPadding = 40f;    // padding ai lati del testo

    private DialogueData currentData;
    private int currentIndex;
    private Action onDialogueComplete;

    // se true, è stato il dialogo a bloccare il player (es. Examine)
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

        UpdateDialogueTextAndSize();
    }

    /// <summary>
    /// Aggiorna il testo (speaker + linea corrente) e la larghezza dello sfondo.
    /// </summary>
    private void UpdateDialogueTextAndSize()
    {
        if (currentData == null || dialogueText == null)
            return;

        // --- TESTO SU UNA SOLA RIGA ---
        if (string.IsNullOrEmpty(currentData.speakerName))
        {
            // solo dialogo
            dialogueText.text = currentData.lines[currentIndex];
        }
        else
        {
            // Speaker: Dialogo  (tutto su una riga, senza bold)
            dialogueText.text = $"{currentData.speakerName}: {currentData.lines[currentIndex]}";
        }

        // forza TMP ad aggiornare
        dialogueText.ForceMeshUpdate();

        float preferredWidth = dialogueText.preferredWidth;

        if (dialogueBackground != null)
        {
            var size = dialogueBackground.sizeDelta;
            size.x = preferredWidth + horizontalPadding * 2f;
            dialogueBackground.sizeDelta = size;

            LayoutRebuilder.ForceRebuildLayoutImmediate(dialogueBackground);
        }
    }

    /// <summary>
    /// Chiamato esternamente (es. da PlayerInteraction) quando il giocatore preme Interact.
    /// </summary>
    public void Advance()
    {
        if (!IsOpen || currentData == null)
            return;

        // blocco avanza-dialogo durante cinematic
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
