using TMPro;
using UnityEngine;
using Sirenix.OdinInspector;

public class PlayerHUD : MonoBehaviour
{
    [Header("Crosshair")]
    [SerializeField] private GameObject crosshair;

    [Header("Interaction Prompt")]
    [SerializeField] private GameObject interactionGroup;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private TextMeshProUGUI hintText;

    [Header("Wall Text")]
    [SerializeField] private GameObject wallTextGroup;
    [SerializeField] private TextMeshProUGUI wallText;

    private PlayerInteraction playerInteraction;
    private PlayerWallTextHandler playerWall;

    private bool isInteractionVisible;
    private bool isWallTextVisible;
    private string cachedWallMessage;

    private void Awake()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("[PlayerHUD] Player non trovato");
            enabled = false;
            return;
        }

        playerInteraction = player.GetComponent<PlayerInteraction>();
        playerWall = player.GetComponent<PlayerWallTextHandler>();

        if (playerInteraction == null)
            Debug.LogError("[PlayerHUD] PlayerInteraction non trovato");

        if (playerWall == null)
            Debug.LogError("[PlayerHUD] PlayerWallTextHandler non trovato");

        // Stato iniziale UI
        SetInteractionVisible(false);
        SetWallTextVisible(false);
        cachedWallMessage = null;

        // Stato iniziale crosshair
        HandleCinematicChanged(CinematicSequence.IsAnyCinematicPlaying);
    }

    private void OnEnable()
    {
        // Subscribe gameplay -> UI
        if (playerInteraction != null)
        {
            playerInteraction.OnShowPrompt += ShowInteraction;
            playerInteraction.OnHidePrompt += HideInteraction;
        }

        if (playerWall != null)
        {
            playerWall.OnShowWallText += ShowWallText;
            playerWall.OnHideWallText += HideWallText;
        }

        // Subscribe cinematic -> crosshair
        CinematicSequence.OnAnyCinematicPlayingChanged += HandleCinematicChanged;

        // Sync immediato (nel caso OnEnable avvenga dopo l'avvio di una cinematic)
        HandleCinematicChanged(CinematicSequence.IsAnyCinematicPlaying);
    }

    private void OnDisable()
    {
        if (playerInteraction != null)
        {
            playerInteraction.OnShowPrompt -= ShowInteraction;
            playerInteraction.OnHidePrompt -= HideInteraction;
        }

        if (playerWall != null)
        {
            playerWall.OnShowWallText -= ShowWallText;
            playerWall.OnHideWallText -= HideWallText;
        }

        CinematicSequence.OnAnyCinematicPlayingChanged -= HandleCinematicChanged;
    }

    // ================= CINEMATIC -> CROSSHAIR =================

    private void HandleCinematicChanged(bool isPlaying)
    {
        if (crosshair == null)
            return;

        crosshair.SetActive(!isPlaying);
    }

    // ================= INTERACTION =================

    private void ShowInteraction(string label, string key)
    {
        isInteractionVisible = true;

        SetWallTextVisible(false); // priorità
        SetInteractionVisible(true);

        if (promptText != null) promptText.text = label;
        if (hintText != null) hintText.text = $"{key} - Interact";
    }

    private void HideInteraction()
    {
        isInteractionVisible = false;
        SetInteractionVisible(false);

        if (isWallTextVisible && !string.IsNullOrEmpty(cachedWallMessage))
        {
            SetWallTextVisible(true);
            if (wallText != null) wallText.text = cachedWallMessage;
        }
        else
        {
            SetWallTextVisible(false);
        }
    }

    // ================= WALL TEXT =================

    private void ShowWallText(string message)
    {
        isWallTextVisible = true;
        cachedWallMessage = message;

        if (isInteractionVisible)
            return;

        SetWallTextVisible(true);
        if (wallText != null) wallText.text = message;
    }

    private void HideWallText()
    {
        isWallTextVisible = false;
        cachedWallMessage = null;

        if (!isInteractionVisible)
            SetWallTextVisible(false);
    }

    // ================= HELPERS =================

    private void SetInteractionVisible(bool visible)
    {
        if (interactionGroup != null && interactionGroup.activeSelf != visible)
            interactionGroup.SetActive(visible);
    }

    private void SetWallTextVisible(bool visible)
    {
        if (wallTextGroup != null && wallTextGroup.activeSelf != visible)
            wallTextGroup.SetActive(visible);

        if (!visible && wallText != null)
            wallText.text = "";
    }
}
