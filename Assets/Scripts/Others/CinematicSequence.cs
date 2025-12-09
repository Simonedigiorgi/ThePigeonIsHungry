using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Animation))]
public class CinematicSequence : MonoBehaviour
{
    // ---------------- INTERACTION (RAYCAST) ----------------
    [BoxGroup("Interaction")]
    public string interactionLabel = "Cinematic";

    [BoxGroup("Interaction")]
    [Tooltip("Collider colpito dal raycast del PlayerInteraction (NON deve essere trigger).")]
    public BoxCollider interactionCollider;

    [BoxGroup("Interaction")]
    [Tooltip("Se true, questa cinematic può essere avviata dal raycast solo una volta.")]
    public bool playInteractionOnlyOnce = false;


    // ---------------- TRIGGER COLLIDER ----------------
    [BoxGroup("Trigger")]
    [Tooltip("Collider trigger opzionale che avvia automaticamente la cinematic quando il Player entra.")]
    public BoxCollider triggerCollider;

    [BoxGroup("Trigger")]
    [Tooltip("Se true, la cinematic può essere avviata dal trigger solo una volta.")]
    public bool playTriggerOnlyOnce = false;


    // ---------------- CAMERA ----------------
    [BoxGroup("Camera")]
    [SerializeField] private Camera cinematicCamera;


    // ---------------- ANIMATION ----------------
    [BoxGroup("Animation")]
    [Tooltip("Lista di clip da usare. Se vuota, viene usata la clip di default dell'Animation.")]
    [ReadOnly] public AnimationClip[] clips;

    [BoxGroup("Animation"), MinValue(0)]
    [Tooltip("Indice di partenza nella lista Clips.")]
    public int startClipIndex = 0;

    [BoxGroup("Animation")]
    [Tooltip("Se true, ad ogni attivazione verrà riprodotta la clip successiva nella lista Clips.")]
    public bool advanceOnEachPlay = false;


    // ---------------- ACTOR ----------------
    [BoxGroup("Actor")]
    [SerializeField] private Animator actor;


    // ---------------- AUDIO ----------------
    [BoxGroup("Audio")]
    [SerializeField] private AudioSource audioSource;

    [BoxGroup("Audio")]
    [SerializeField] private AudioClip[] sfxClips;


    // ---------------- OPTIONAL ----------------
    [BoxGroup("Optional")]
    [Tooltip("Oggetto da attivare durante la cinematic e disattivare alla fine.")]
    public GameObject activateDuringCinematic;

    [BoxGroup("Optional")]
    public DialogueData dialogueToTrigger;

    [BoxGroup("Events")]
    public UnityEvent onAnimationEventParticle;
    public void TriggerParticleEvent() => onAnimationEventParticle?.Invoke();


    // ---------------- INTERNAL ----------------
    private Animation anim;
    private bool isPlaying;
    private bool allowEvents;

    private FirstPersonController playerController;
    private Camera playerCamera;

    private int currentClipIndex;
    private bool sequenceCompleted;

    // “play una volta” per canale
    private bool interactionUsedOnce;
    private bool triggerUsedOnce;

    public static readonly List<CinematicSequence> AllSequences = new();
    public static bool IsAnyCinematicPlaying = false;


    private void OnEnable() => AllSequences.Add(this);
    private void OnDisable() => AllSequences.Remove(this);


    private void Awake()
    {
        anim = GetComponent<Animation>();
        anim.playAutomatically = false;
        anim.Stop();

        if (cinematicCamera)
            cinematicCamera.enabled = false;

        // interactionCollider: NON lo tocchiamo, può essere normale box
        // triggerCollider: deve essere trigger + forwarder
        if (triggerCollider)
        {
            triggerCollider.isTrigger = true;

            var forwarder = triggerCollider.GetComponent<CinematicTriggerForwarder>();
            if (forwarder == null)
                forwarder = triggerCollider.gameObject.AddComponent<CinematicTriggerForwarder>();

            forwarder.Init(this);
        }

        if (activateDuringCinematic)
            activateDuringCinematic.SetActive(false);

        allowEvents = false;
        sequenceCompleted = false;
        interactionUsedOnce = false;
        triggerUsedOnce = false;

        // Se clips è vuoto, lo riempiamo dalle AnimationState
        if (clips == null || clips.Length == 0)
        {
            var list = new List<AnimationClip>();
            foreach (AnimationState state in anim)
            {
                if (state != null && state.clip != null && !list.Contains(state.clip))
                    list.Add(state.clip);
            }

            clips = list.ToArray();
        }

        if (clips != null && clips.Length > 0)
            currentClipIndex = Mathf.Clamp(startClipIndex, 0, clips.Length - 1);
        else
            currentClipIndex = 0;
    }


    // ======================================================
    //                    PUBLIC API
    // ======================================================

    /// <summary>
    /// Chiamato dal PlayerInteraction (raycast).
    /// </summary>
    public void PlayFromInteraction(FirstPersonController controller, Camera cam)
    {
        PlayInternal(controller, cam, fromTrigger: false);
    }

    /// <summary>
    /// Chiamato dal trigger (forwarder).
    /// </summary>
    public void PlayFromTrigger()
    {
        var controller = FirstPersonController.Instance;
        var cam = Camera.main;

        if (controller != null && cam != null)
            PlayInternal(controller, cam, fromTrigger: true);
    }


    // ======================================================
    //                   CORE PLAY LOGIC
    // ======================================================

    private void PlayInternal(FirstPersonController controller, Camera cam, bool fromTrigger)
    {
        if (isPlaying || anim == null || sequenceCompleted)
            return;

        // gestisci “play una sola volta” per canale
        if (fromTrigger)
        {
            if (playTriggerOnlyOnce && triggerUsedOnce)
                return;

            if (playTriggerOnlyOnce)
                triggerUsedOnce = true;
        }
        else
        {
            if (playInteractionOnlyOnce && interactionUsedOnce)
                return;

            if (playInteractionOnlyOnce)
                interactionUsedOnce = true;
        }

        IsAnyCinematicPlaying = true;
        isPlaying = true;
        allowEvents = true;

        playerController = controller;
        playerCamera = cam;

        if (playerController) playerController.ControlsEnabled = false;
        if (playerCamera) playerCamera.enabled = false;
        if (cinematicCamera) cinematicCamera.enabled = true;
        if (activateDuringCinematic) activateDuringCinematic.SetActive(true);

        string clipName = GetCurrentClipName();
        if (!string.IsNullOrEmpty(clipName))
            anim.Play(clipName);
        else
            anim.Play();

        StartCoroutine(WaitForEnd());
    }

    private string GetCurrentClipName()
    {
        if (clips != null && clips.Length > 0)
        {
            int idx = Mathf.Clamp(currentClipIndex, 0, clips.Length - 1);
            return clips[idx] != null ? clips[idx].name : null;
        }

        return null;
    }

    private IEnumerator WaitForEnd()
    {
        while (anim.isPlaying)
            yield return null;

        Stop();
    }

    private void Stop()
    {
        IsAnyCinematicPlaying = false;

        if (cinematicCamera)
            cinematicCamera.enabled = false;

        if (playerCamera)
            playerCamera.enabled = true;

        if (playerController)
            playerController.ControlsEnabled = true;

        if (activateDuringCinematic)
            activateDuringCinematic.SetActive(false);

        // chiudi eventuale dialogo aperto
        if (DialogueSystem.Instance != null)
            DialogueSystem.Instance.ForceCloseDialogue();

        HandleSequenceAdvance();

        isPlaying = false;
        allowEvents = false;
    }

    private void HandleSequenceAdvance()
    {
        if (clips != null && clips.Length > 0 && advanceOnEachPlay)
        {
            currentClipIndex++;

            if (currentClipIndex >= clips.Length)
            {
                sequenceCompleted = true;
                DisableAllColliders();
            }
        }
        else if (!advanceOnEachPlay)
        {
            // niente advance, ma se abbiamo consumato il canale “solo una volta”
            // lasciamo comunque attivi i collider (potranno usarli altri canali se non flaggati)
            if (!playInteractionOnlyOnce && !playTriggerOnlyOnce)
                return;
        }
    }

    private void DisableAllColliders()
    {
        if (interactionCollider)
            interactionCollider.enabled = false;

        if (triggerCollider)
            triggerCollider.enabled = false;
    }


    // ---------------- ANIMATION EVENTS ----------------
    public void PlaySfx(int index)
    {
        if (!allowEvents || !audioSource) return;
        if (index < 0 || index >= sfxClips.Length) return;

        audioSource.PlayOneShot(sfxClips[index]);
    }

    public void ActorSetTrigger(string triggerName)
    {
        if (!allowEvents || !actor || string.IsNullOrEmpty(triggerName)) return;
        actor.SetTrigger(triggerName);
    }

    public void ActorPlayState(string stateName)
    {
        if (!allowEvents || !actor || string.IsNullOrEmpty(stateName)) return;
        actor.CrossFade(stateName, 0.1f);
    }

    public void TriggerDialogue(DialogueData data)
    {
        if (!allowEvents || data == null || DialogueSystem.Instance == null) return;
        DialogueSystem.Instance.StartDialogue(data, false, null);
    }


    // ======================================================
    //          CHIAMATO DAL FORWARDER DEL TRIGGER
    // ======================================================
    public void OnTriggerEnteredByPlayer()
    {
        PlayFromTrigger();
    }
}


/// <summary>
/// Helper su GameObject del triggerCollider che notifica la CinematicSequence.
/// </summary>
public class CinematicTriggerForwarder : MonoBehaviour
{
    private CinematicSequence sequence;

    public void Init(CinematicSequence seq)
    {
        sequence = seq;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (sequence == null)
            return;

        if (!other.CompareTag("Player"))
            return;

        sequence.OnTriggerEnteredByPlayer();
    }
}
