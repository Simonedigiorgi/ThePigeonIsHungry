using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Riferimenti")]
    [SerializeField] private FirstPersonController playerController;
    [SerializeField] private Camera playerCamera;

    [Header("Interazione")]
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactLayerMask = ~0;

    [Header("Input")]
    [SerializeField] private InputActionReference interactAction;

    [Header("UI")]
    [SerializeField] private GameObject promptRoot;
    [SerializeField] private TextMeshProUGUI promptText;


    private void Update()
    {
        if (!playerController || !playerCamera)
            return;

        // ---------- 1) CINEMATICA IN CORSO ----------
        if (CinematicSequence.IsAnyCinematicPlaying)
        {
            promptRoot.SetActive(false);
            return;
        }

        // ---------- 2) DIALOGO IN CORSO ----------
        if (DialogueSystem.Instance != null && DialogueSystem.Instance.IsOpen)
        {
            promptRoot.SetActive(false);

            if (interactAction.action.WasPressedThisFrame())
                DialogueSystem.Instance.Advance();

            return;
        }

        // Se arriviamo qui, possiamo riaccendere il promptRoot
        if (!promptRoot.activeSelf)
            promptRoot.SetActive(true);

        // ---------- 3) PLAYER BLOCCATO ----------
        if (!playerController.ControlsEnabled)
        {
            promptText.text = "";
            return;
        }

        // ---------- 4) INTERRAZIONE NORMALE ----------
        bool interactPressed = interactAction.action.WasPressedThisFrame();

        Ray ray = new(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactLayerMask, QueryTriggerInteraction.Collide))
        {
            // ---- CINEMATIC ----
            foreach (var seq in CinematicSequence.AllSequences)
            {
                if (seq != null && seq.interactionCollider == hit.collider)
                {
                    promptText.text = seq.interactionLabel;

                    if (interactPressed)
                        seq.Play(playerController, playerCamera);

                    return;
                }
            }

            // ---- EXAMINE ----
            foreach (var ex in ExamineObject.AllExaminables)
            {
                if (ex != null && ex.interactionCollider == hit.collider)
                {
                    string label = ex.dialogue != null ? ex.dialogue.speakerName : "Esamina";
                    promptText.text = label;

                    if (interactPressed)
                        ex.Examine();

                    return;
                }
            }
        }

        // ---------- 5) NON STO GUARDANDO NIENTE ----------
        promptText.text = "";
    }
}
