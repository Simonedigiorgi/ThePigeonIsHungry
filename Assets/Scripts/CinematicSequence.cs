using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Animation))]
public class CinematicSequence : MonoBehaviour
{
    // ---------------- INTERACTION ----------------
    [BoxGroup("Interaction")]
    public string interactionLabel = "Cinematic";

    [BoxGroup("Interaction")]
    [Tooltip("Collider usato per l'interazione (raycast + trigger). Può stare su un GameObject separato.")]
    public BoxCollider interactionCollider;

    [BoxGroup("Interaction")]
    [Tooltip("Se true, entrare nel collider (OnTriggerEnter) fa partire la cinematic.")]
    public bool autoPlayOnTrigger = false;

    [BoxGroup("Interaction")]
    [Tooltip("Se true, dopo aver finito la sequence questa cinematic non sarà più interagibile.")]
    public bool playOnlyOnce = false;


    // ---------------- CAMERA ----------------
    [BoxGroup("Camera")]
    [SerializeField] private Camera cinematicCamera;


    // ---------------- ANIMATION ----------------
    [BoxGroup("Animation")]
    [Tooltip("Lista di clip da usare. Se vuota, viene usata la clip di default dell'Animation.")]
    [ReadOnly] public AnimationClip[] clips;

    [BoxGroup("Animation")]
    [MinValue(0)]
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
    public void TriggerParticleEvent()
    {
        onAnimationEventParticle?.Invoke();
    }

    // ---------------- INTERNAL ----------------
    private Animation anim;
    private bool isPlaying;
    private bool allowEvents;

    private FirstPersonController playerController;
    private Camera playerCamera;

    private int currentClipIndex;
    private bool sequenceCompleted;

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

        if (interactionCollider)
        {
            interactionCollider.isTrigger = true;

            var forwarder = interactionCollider.GetComponent<CinematicTriggerForwarder>();
            if (forwarder == null)
                forwarder = interactionCollider.gameObject.AddComponent<CinematicTriggerForwarder>();

            forwarder.Init(this);
        }

        if (activateDuringCinematic)
            activateDuringCinematic.SetActive(false);

        allowEvents = false;
        sequenceCompleted = false;

        // ➜ SE Clips è vuoto, lo auto–riempiamo dalle animazioni del componente Animation
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


    // ---------------- PLAY ----------------
    public void Play(FirstPersonController controller, Camera pCamera)
    {
        // se è già in corso o abbiamo finito e va giocata solo una volta → esci
        if (isPlaying || anim == null)
            return;

        if (playOnlyOnce && sequenceCompleted)
            return;

        IsAnyCinematicPlaying = true;
        isPlaying = true;
        allowEvents = true;

        playerController = controller;
        playerCamera = pCamera;

        if (playerController) playerController.ControlsEnabled = false;
        if (playerCamera) playerCamera.enabled = false;
        if (cinematicCamera) cinematicCamera.enabled = true;
        if (activateDuringCinematic) activateDuringCinematic.SetActive(true);

        // scegli la clip da riprodurre
        string clipName = GetCurrentClipName();
        if (!string.IsNullOrEmpty(clipName))
            anim.Play(clipName);
        else
            anim.Play(); // clip di default

        StartCoroutine(WaitForEnd());
    }

    private string GetCurrentClipName()
    {
        if (clips != null && clips.Length > 0)
        {
            int idx = Mathf.Clamp(currentClipIndex, 0, clips.Length - 1);
            return clips[idx] != null ? clips[idx].name : null;
        }

        return null; // usa default
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

        // chiudi eventuale dialogo aperto ma senza riabilitare i controlli (già fatto sopra)
        if (DialogueSystem.Instance != null)
            DialogueSystem.Instance.ForceCloseDialogue();

        // gestisci avanzamento sequence
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

                if (playOnlyOnce)
                    DisableInteraction();
            }
        }
        else if (playOnlyOnce)
        {
            // single clip (o niente clips[] ma default) e deve essere giocata una sola volta
            sequenceCompleted = true;
            DisableInteraction();
        }
    }

    private void DisableInteraction()
    {
        if (interactionCollider)
            interactionCollider.enabled = false;
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

    // Animation Event: fai partire un dialogo DURANTE la cinematic
    public void TriggerDialogue(DialogueData data)
    {
        if (!allowEvents || data == null || DialogueSystem.Instance == null) return;

        // lockPlayerControls = false → i controlli restano gestiti dalla cinematic
        DialogueSystem.Instance.StartDialogue(data, false, null);
    }

    // --------------------------------------------------
    // FORWARDER PER IL TRIGGER (Collider separato)
    // --------------------------------------------------
    public void OnTriggerEnteredByPlayer()
    {
        if (!autoPlayOnTrigger)
            return;

        var controller = FirstPersonController.Instance;
        var cam = Camera.main;

        if (controller != null && cam != null)
            Play(controller, cam);
    }
}


/// <summary>
/// Piccolo helper che gira su GameObject del collider e notifica la CinematicSequence.
/// Viene aggiunto automaticamente in Awake se manca.
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
