using UnityEngine;
using TMPro;
using Sirenix.OdinInspector;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance { get; private set; }

    // ---------------- UI ----------------
    [BoxGroup("UI")]
    [SerializeField] private TextMeshProUGUI questText;

    // ---------------- START SETTINGS ----------------
    [BoxGroup("Start Settings")]
    [Tooltip("Quest da usare all'avvio della scena.")]
    [SerializeField] private QuestData startingQuest;

    [BoxGroup("Start Settings")]
    [Tooltip("Indice dello step da cui partire (di solito 0).")]
    [SerializeField, MinValue(0)]
    private int startingStepIndex = 0;

    // ---------------- AUDIO ----------------
    [BoxGroup("Audio")]
    [Tooltip("Effetto sonoro riprodotto quando la quest avanza di uno step.")]
    [SerializeField] private AudioClip questAdvanceSfx;

    private AudioSource playerAudio;

    // ---------------- RUNTIME ----------------
    private QuestData currentQuest;
    private int currentStepIndex = -1;


    private void Awake()
    {
        // Singleton soft
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Trova audio del player
        var playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            playerAudio = playerObj.GetComponent<AudioSource>();

        // Se hai impostato una quest di partenza, la avviamo subito
        if (startingQuest != null)
        {
            StartQuest(startingQuest, startingStepIndex);
        }
        else
        {
            UpdateUI();
        }
    }


    // ================== API PUBBLICA ==================

    public void StartQuest(QuestData quest, int startIndex = 0)
    {
        if (quest == null || quest.steps == null || quest.steps.Length == 0)
        {
            currentQuest = null;
            currentStepIndex = -1;
            UpdateUI();
            return;
        }

        currentQuest = quest;
        currentStepIndex = Mathf.Clamp(startIndex, 0, quest.steps.Length - 1);

        UpdateUI();
    }

    public void AdvanceStep()
    {
        if (currentQuest == null || currentQuest.steps == null || currentQuest.steps.Length == 0)
            return;

        int previousIndex = currentStepIndex;
        currentStepIndex++;

        if (currentStepIndex >= currentQuest.steps.Length)
        {
            currentStepIndex = currentQuest.steps.Length - 1;
        }

        // Se è davvero avanzata di step, riproduci il suono
        if (currentStepIndex != previousIndex)
            PlayQuestAdvanceSfx();

        UpdateUI();
    }

    public void SetStep(int index)
    {
        if (currentQuest == null || currentQuest.steps == null || currentQuest.steps.Length == 0)
            return;

        int previousIndex = currentStepIndex;

        currentStepIndex = Mathf.Clamp(index, 0, currentQuest.steps.Length - 1);

        if (currentStepIndex != previousIndex)
            PlayQuestAdvanceSfx();

        UpdateUI();
    }


    // ================== UI ==================

    private void UpdateUI()
    {
        if (questText == null)
            return;

        if (currentQuest == null || currentStepIndex < 0 ||
            currentQuest.steps == null || currentQuest.steps.Length == 0)
        {
            questText.text = "";
            return;
        }

        questText.text = currentQuest.steps[currentStepIndex];
    }


    // ================== AUDIO ==================

    private void PlayQuestAdvanceSfx()
    {
        if (playerAudio != null && questAdvanceSfx != null)
            playerAudio.PlayOneShot(questAdvanceSfx);
    }
}
