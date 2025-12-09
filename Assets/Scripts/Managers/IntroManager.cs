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
    [BoxGroup("Intro Panel")]
    [SerializeField] private GameObject introPanel;

    [BoxGroup("Intro Panel")]
    [SerializeField] private TextMeshProUGUI introMainText;

    [BoxGroup("Intro Panel")]
    [SerializeField, TextArea] private string introText;

    [BoxGroup("Intro Panel")]
    [SerializeField, MinValue(0f)]
    private float introMainTextDelay = 0.5f;

    [BoxGroup("Intro Panel")]
    [SerializeField] private TextMeshProUGUI introHintText;

    [BoxGroup("Intro Panel")]
    [SerializeField, MinValue(0f)]
    private float delayBeforeHint = 2f;



    // ---------------- EXTRA MESSAGE ----------------
    [BoxGroup("Extra Message")]
    [SerializeField] private TextMeshProUGUI extraMessageText;

    [BoxGroup("Extra Message")]
    [SerializeField, TextArea] private string extraMessage;

    [BoxGroup("Extra Message")]
    [SerializeField, MinValue(0f)] private float extraMessageDelay = 0.5f;

    [BoxGroup("Extra Message")]
    [SerializeField, MinValue(0f)] private float extraMessageDuration = 2f;

    [BoxGroup("Extra Message")]
    [SerializeField] private AudioClip extraMessageSfx;


    // ---------------- INPUT ----------------
    [BoxGroup("Input")]
    [SerializeField] private InputActionReference continueActionReference;

    private InputAction continueAction;


    // ---------------- AUDIO ----------------
    [BoxGroup("Audio")]
    [SerializeField] private AudioClip continueSfx;

    [BoxGroup("Audio")]
    [SerializeField] private AudioClip musicOnStart;


    // ---------------- INTERNAL STATE ----------------
    private bool introActive = false;
    private string keyboardBinding = "";
    private string gamepadBinding = "";
    private bool useGamepadHint = false;

    // ⬇️ nuovo: possiamo continuare solo dopo che l’hint è apparso
    private bool canContinue = false;


    // ======================================
    //                SETUP
    // ======================================

    private void Awake()
    {
        FindPlayerAndAudio();
        SetupUI();
        SetupInput();

        introActive = true;
        canContinue = false;   // all’inizio NO

        // Sequenza di attivazione UI
        StartCoroutine(IntroSequenceRoutine());
    }

    private void OnDestroy()
    {
        continueAction?.Disable();
    }


    private void FindPlayerAndAudio()
    {
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null)
        {
            Debug.LogError("[IntroManager] Nessun Player trovato.");
            return;
        }

        playerController = playerObj.GetComponent<FirstPersonController>();
        playerAudio = playerObj.GetComponent<AudioSource>();

        if (playerController != null)
            playerController.ControlsEnabled = false;
    }


    private void SetupUI()
    {
        introPanel?.SetActive(true);

        if (introMainText != null)
        {
            introMainText.gameObject.SetActive(false); // ora ha un delay
            introMainText.text = introText;
        }

        if (introHintText != null)
        {
            introHintText.gameObject.SetActive(false);
            introHintText.text = "";
        }

        if (extraMessageText != null)
        {
            extraMessageText.gameObject.SetActive(false);
            extraMessageText.text = "";
        }
    }


    private void SetupInput()
    {
        continueAction = continueActionReference?.action;

        if (continueAction == null)
        {
            Debug.LogWarning("[IntroManager] Nessuna InputAction valida per Continue.");
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


    // ======================================
    //           INTRO SEQUENCE
    // ======================================

    private IEnumerator IntroSequenceRoutine()
    {
        // 1) Mostra testo principale dopo un delay
        if (introMainTextDelay > 0)
            yield return new WaitForSeconds(introMainTextDelay);

        introMainText?.gameObject.SetActive(true);

        // 2) Mostra hint dopo il delay
        if (delayBeforeHint > 0)
            yield return new WaitForSeconds(delayBeforeHint);

        introHintText?.gameObject.SetActive(true);
        UpdateHintText();

        // ⬇️ SOLO ORA si può premere per continuare
        canContinue = true;
    }


    private void Update()
    {
        if (!introActive || continueAction == null)
            return;

        // 🔒 blocca l’input finché l’hint non è apparso
        if (!canContinue)
            return;

        if (!continueAction.WasPressedThisFrame())
            return;

        var control = continueAction.activeControl;
        if (control != null)
            useGamepadHint = control.device is Gamepad;

        if (playerAudio != null && continueSfx != null)
            playerAudio.PlayOneShot(continueSfx);

        StartCoroutine(EndIntroRoutine());
    }


    private void UpdateHintText()
    {
        if (introHintText == null)
            return;

        string key = useGamepadHint ? gamepadBinding : keyboardBinding;
        introHintText.text = $"{key} - Continue";
    }


    private IEnumerator EndIntroRoutine()
    {
        introActive = false;
        canContinue = false;

        // 1) Spegni subito pannello + testo + hint
        introPanel?.SetActive(false);

        // 2) Sblocca player
        if (playerController != null)
            playerController.ControlsEnabled = true;

        // 3) Avvia musica
        if (playerAudio != null && musicOnStart != null)
        {
            playerAudio.clip = musicOnStart;
            playerAudio.loop = true;
            playerAudio.Play();
        }

        // 4) Extra message (se esiste)
        bool hasExtra =
            extraMessageText != null &&
            !string.IsNullOrWhiteSpace(extraMessage) &&
            extraMessageDuration > 0f;

        if (!hasExtra)
            yield break;

        // Delay prima della comparsa
        if (extraMessageDelay > 0)
            yield return new WaitForSeconds(extraMessageDelay);

        extraMessageText.gameObject.SetActive(true);
        extraMessageText.text = extraMessage;

        if (playerAudio != null && extraMessageSfx != null)
            playerAudio.PlayOneShot(extraMessageSfx);

        yield return new WaitForSeconds(extraMessageDuration);

        extraMessageText.text = "";
        extraMessageText.gameObject.SetActive(false);
    }
}
