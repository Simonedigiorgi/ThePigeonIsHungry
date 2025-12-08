using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using Sirenix.OdinInspector;

public class IntroManager : MonoBehaviour
{
    // ---------------- PLAYER ----------------
    [BoxGroup("Player"), ReadOnly, ShowInInspector]
    private FirstPersonController playerController;

    [BoxGroup("Player"), ReadOnly, ShowInInspector]
    private AudioSource playerAudio;


    // ---------------- INTRO PANEL ----------------
    [BoxGroup("Intro")]
    [SerializeField] private GameObject introPanel;

    [BoxGroup("Intro")]
    [SerializeField] private TextMeshProUGUI introHintText;

    [BoxGroup("Intro")]
    [SerializeField, MinValue(0f)]
    private float delayBeforeHint = 2f;


    // ---------------- INPUT ----------------
    [BoxGroup("Input")]
    [SerializeField] private InputActionReference continueActionReference;

    private InputAction continueAction;


    // ---------------- AUDIO ----------------
    [BoxGroup("Audio")]
    [Tooltip("Suono riprodotto quando premi il comando per chiudere il pannello.")]
    [SerializeField] private AudioClip continueSfx;

    [BoxGroup("Audio")]
    [Tooltip("Musica che parte quando l'intro si chiude.")]
    [SerializeField] private AudioClip musicOnStart;


    // ---------------- STATE ----------------
    [ShowInInspector, ReadOnly]
    private bool introActive = false;

    [ShowInInspector, ReadOnly]
    private bool hintVisible = false;

    private string keyboardBinding = "";
    private string gamepadBinding = "";
    private bool useGamepadHint = false;


    private void Awake()
    {
        FindPlayerAndAudio();
        SetupIntroPanel();
        SetupInput();

        introActive = true;
        hintVisible = false;

        StartCoroutine(ShowHintAfterDelay());
    }

    private void OnDestroy()
    {
        continueAction?.Disable();
    }


    // ---------------- SETUP ----------------

    private void FindPlayerAndAudio()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogError("[IntroManager] Nessun GameObject con tag 'Player' trovato in scena.");
            return;
        }

        playerController = playerObj.GetComponent<FirstPersonController>();
        if (playerController == null)
            Debug.LogError("[IntroManager] Il Player non ha un FirstPersonController.");

        playerAudio = playerObj.GetComponent<AudioSource>();
        if (playerAudio == null)
            Debug.LogWarning("[IntroManager] Il Player non ha un AudioSource, gli audio non verranno riprodotti.");

        // blocca subito i controlli
        if (playerController != null)
            playerController.ControlsEnabled = false;
    }

    private void SetupIntroPanel()
    {
        if (introPanel != null)
            introPanel.SetActive(true);

        if (introHintText != null)
            introHintText.text = "";
    }

    private void SetupInput()
    {
        if (continueActionReference == null)
        {
            Debug.LogWarning("[IntroManager] Nessuna InputActionReference assegnata per il Continue.");
            return;
        }

        continueAction = continueActionReference.action;
        if (continueAction == null)
        {
            Debug.LogWarning("[IntroManager] ContinueActionReference non ha una action valida.");
            return;
        }

        continueAction.Enable();
        CacheBindingDisplayStrings();
    }

    private void CacheBindingDisplayStrings()
    {
        keyboardBinding = "";
        gamepadBinding = "";

        if (continueAction == null)
            return;

        for (int i = 0; i < continueAction.bindings.Count; i++)
        {
            var binding = continueAction.bindings[i];

            if (binding.isComposite || binding.isPartOfComposite)
                continue;

            string display = continueAction.GetBindingDisplayString(
                i,
                InputBinding.DisplayStringOptions.DontIncludeInteractions
            );

            if (binding.path.Contains("Keyboard"))
                keyboardBinding = display;
            else if (binding.path.Contains("Gamepad"))
                gamepadBinding = display;
        }

        if (string.IsNullOrEmpty(keyboardBinding))
            keyboardBinding = continueAction.name;

        if (string.IsNullOrEmpty(gamepadBinding))
            gamepadBinding = keyboardBinding;
    }


    // ---------------- INTRO FLOW ----------------

    private IEnumerator ShowHintAfterDelay()
    {
        if (delayBeforeHint > 0f)
            yield return new WaitForSeconds(delayBeforeHint);

        hintVisible = true;
        UpdateHintText();
    }

    private void Update()
    {
        if (!introActive || continueAction == null)
            return;

        if (!continueAction.WasPressedThisFrame())
            return;

        var control = continueAction.activeControl;
        if (control != null)
            useGamepadHint = control.device is Gamepad;

        // SFX di conferma
        if (playerAudio != null && continueSfx != null)
            playerAudio.PlayOneShot(continueSfx);

        EndIntro();
    }

    private void UpdateHintText()
    {
        if (!hintVisible || introHintText == null)
            return;

        string key = useGamepadHint ? gamepadBinding : keyboardBinding;
        introHintText.text = $"{key} - Continue";
    }

    private void EndIntro()
    {
        introActive = false;

        if (introPanel != null)
            introPanel.SetActive(false);

        if (playerController != null)
            playerController.ControlsEnabled = true;

        // musica di gioco
        if (playerAudio != null && musicOnStart != null)
        {
            playerAudio.clip = musicOnStart;
            playerAudio.loop = true;
            playerAudio.Play();
        }
    }
}
