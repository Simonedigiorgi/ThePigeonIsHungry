using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using Sirenix.OdinInspector;

public class PlayerInteraction : MonoBehaviour
{
    // ---------------- RIFERIMENTI AUTO ----------------
    private FirstPersonController playerController;
    private Camera playerCamera;

    // ---------------- INTERAZIONE ----------------
    [BoxGroup("Interaction")]
    [SerializeField] private float interactDistance = 3f;

    [BoxGroup("Interaction")]
    [SerializeField] private LayerMask interactLayerMask = ~0;

    // ---------------- INPUT ----------------
    [BoxGroup("Input")]
    [SerializeField] private InputActionReference interactActionReference;

    private InputAction interactAction;
    private string keyboardBinding = "";
    private string gamepadBinding = "";
    private bool useGamepadHint = false;

    // ---------------- UI ----------------
    [BoxGroup("UI")]
    [SerializeField] private GameObject promptRoot;

    [BoxGroup("UI")]
    [SerializeField] private TextMeshProUGUI promptText;        // Nome oggetto

    [BoxGroup("UI")]
    [SerializeField] private TextMeshProUGUI interactHintText;  // "E - Interact" / "X - Interact"


    private void Awake()
    {
        // prendi il controller dal Player
        playerController = GetComponent<FirstPersonController>();
        if (playerController == null)
            Debug.LogError("[PlayerInteraction] Nessun FirstPersonController trovato sullo stesso GameObject.");

        // prendi la camera dal Player → fallback Camera.main
        playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null)
            playerCamera = Camera.main;

        if (playerCamera == null)
            Debug.LogError("[PlayerInteraction] Nessuna Camera trovata (né figlio né Camera.main).");
    }

    private void OnEnable()
    {
        if (interactActionReference == null)
            return;

        interactAction = interactActionReference.action;

        if (interactAction != null)
        {
            interactAction.Enable();
            CacheBindingDisplayStrings();
        }
    }

    private void OnDisable()
    {
        interactAction?.Disable();
    }

    /// <summary>
    /// Legge i binding dell'azione Interact e salva stringhe leggibili
    /// per tastiera e gamepad (senza hardcode).
    /// </summary>
    private void CacheBindingDisplayStrings()
    {
        keyboardBinding = "";
        gamepadBinding = "";

        if (interactAction == null)
            return;

        for (int i = 0; i < interactAction.bindings.Count; i++)
        {
            var binding = interactAction.bindings[i];

            if (binding.isComposite || binding.isPartOfComposite)
                continue;

            string display = interactAction.GetBindingDisplayString(
                i,
                InputBinding.DisplayStringOptions.DontIncludeInteractions
            );

            if (binding.path.Contains("Keyboard"))
                keyboardBinding = display;
            else if (binding.path.Contains("Gamepad"))
                gamepadBinding = display;
        }

        if (string.IsNullOrEmpty(keyboardBinding))
            keyboardBinding = interactAction.name;

        if (string.IsNullOrEmpty(gamepadBinding))
            gamepadBinding = keyboardBinding;
    }


    private void Update()
    {
        if (playerController == null || playerCamera == null || interactAction == null)
            return;

        // 1) CINEMATICA IN CORSO
        if (CinematicSequence.IsAnyCinematicPlaying)
        {
            SetPromptVisible(false);
            return;
        }

        // 2) DIALOGO IN CORSO
        if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsOpen)
        {
            SetPromptVisible(false);

            if (interactAction.WasPressedThisFrame())
                DialogueSystem.Instance.Advance();

            return;
        }

        // se arriviamo qui, il prompt può essere visibile
        SetPromptVisible(true);

        // 3) PLAYER BLOCCATO
        if (!playerController.ControlsEnabled)
        {
            ClearTexts();
            return;
        }

        // 4) INTERAZIONE NORMALE
        bool interactPressed = interactAction.WasPressedThisFrame();

        if (interactPressed)
        {
            var control = interactAction.activeControl;
            if (control != null)
            {
                if (control.device is Gamepad)
                    useGamepadHint = true;
                else if (control.device is Keyboard || control.device is Mouse)
                    useGamepadHint = false;
            }
        }

        Ray ray = new(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayerMask, QueryTriggerInteraction.Collide))
        {
            // --- CINEMATIC ---
            foreach (var seq in CinematicSequence.AllSequences)
            {
                if (seq != null && seq.interactionCollider == hit.collider)
                {
                    ShowTexts(seq.interactionLabel);

                    if (interactPressed)
                        seq.PlayFromInteraction(playerController, playerCamera);

                    return;
                }
            }

            // --- EXAMINE ---
            foreach (var ex in ExamineObject.AllExaminables)
            {
                if (ex != null && ex.interactionCollider == hit.collider)
                {
                    string label = ex.dialogue != null ? ex.dialogue.speakerName : "Esamina";
                    ShowTexts(label);

                    if (interactPressed)
                        ex.Examine();

                    return;
                }
            }
        }

        // 5) NON STO GUARDANDO NIENTE
        ClearTexts();
    }


    private void ShowTexts(string objectLabel)
    {
        if (promptText != null)
            promptText.text = objectLabel;

        if (interactHintText != null)
        {
            string key = useGamepadHint ? gamepadBinding : keyboardBinding;
            interactHintText.text = $"{key} - Interact";
        }
    }

    private void ClearTexts()
    {
        if (promptText != null)
            promptText.text = "";

        if (interactHintText != null)
            interactHintText.text = "";
    }

    private void SetPromptVisible(bool visible)
    {
        if (promptRoot != null && promptRoot.activeSelf != visible)
            promptRoot.SetActive(visible);
    }
}
