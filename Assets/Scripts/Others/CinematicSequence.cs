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
    [Tooltip("Collider colpito dal raycast del PlayerInteraction (NON deve essere trigger).")]
    public BoxCollider interactionCollider;

    [BoxGroup("Interaction")]
    [Tooltip("Se true, questa cinematic può essere avviata dal raycast solo una volta.")]
    public bool playInteractionOnlyOnce = false;

    // ---------------- TRIGGER ----------------
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
    public int startClipIndex = 0;

    [BoxGroup("Animation")]
    public bool advanceOnEachPlay = false;

    // ---------------- ACTOR ----------------
    [BoxGroup("Actor")]
    [SerializeField] private Animator actor;

    // ---------------- AUDIO ----------------
    [BoxGroup("Audio")]
    [SerializeField] private AudioSource audioSource;

    // ---------------- EVENTS ----------------
    [BoxGroup("Events")]
    [Tooltip("Evento richiamabile da Animation Event (VFX, SFX, SetActive, ecc.).")]
    public UnityEvent onTimedEvent;

    [BoxGroup("Events")]
    public UnityEvent onCinematicStart;

    [BoxGroup("Events")]
    public UnityEvent onCinematicEnd;

    // Animation Event (no parametri)
    public void TriggerTimedEvent() => onTimedEvent?.Invoke();

    // ---------------- INTERNAL ----------------
    private Animation anim;
    private bool isPlaying;
    private bool allowEvents;

    private FirstPersonController playerController;
    private Camera playerCamera;

    private int currentClipIndex;
    private bool sequenceCompleted;

    private bool interactionUsedOnce;
    private bool triggerUsedOnce;

    public static readonly List<CinematicSequence> AllSequences = new();

    private static int playingCount = 0;
    public static bool IsAnyCinematicPlaying => playingCount > 0;

    // 🔔 evento globale (HUD / crosshair / UI)
    public static event System.Action<bool> OnAnyCinematicPlayingChanged;

    private static void NotifyCinematicStateChanged(bool wasPlaying)
    {
        bool isPlayingNow = IsAnyCinematicPlaying;
        if (wasPlaying != isPlayingNow)
            OnAnyCinematicPlayingChanged?.Invoke(isPlayingNow);
    }

    private void OnEnable() => AllSequences.Add(this);
    private void OnDisable() => AllSequences.Remove(this);

    private void Awake()
    {
        anim = GetComponent<Animation>();
        anim.playAutomatically = false;
        anim.Stop();

        if (cinematicCamera)
            cinematicCamera.enabled = false;

        if (triggerCollider)
        {
            triggerCollider.isTrigger = true;
            var forwarder = triggerCollider.GetComponent<CinematicTriggerForwarder>();
            if (forwarder == null)
                forwarder = triggerCollider.gameObject.AddComponent<CinematicTriggerForwarder>();

            forwarder.Init(this);
        }

        allowEvents = false;
        sequenceCompleted = false;
        interactionUsedOnce = false;
        triggerUsedOnce = false;

        if (clips == null || clips.Length == 0)
        {
            var list = new List<AnimationClip>();
            foreach (AnimationState state in anim)
            {
                if (state?.clip != null && !list.Contains(state.clip))
                    list.Add(state.clip);
            }
            clips = list.ToArray();
        }

        currentClipIndex = (clips != null && clips.Length > 0)
            ? Mathf.Clamp(startClipIndex, 0, clips.Length - 1)
            : 0;
    }

    // ======================================================
    // PLAY
    // ======================================================

    public void PlayFromInteraction(FirstPersonController controller, Camera cam)
    {
        PlayInternal(controller, cam, false);
    }

    public void PlayFromTrigger()
    {
        var controller = FirstPersonController.Instance;
        var cam = controller != null ? controller.GetComponentInChildren<Camera>() : null;

        if (controller != null && cam != null)
            PlayInternal(controller, cam, true);
    }

    private void PlayInternal(FirstPersonController controller, Camera cam, bool fromTrigger)
    {
        if (isPlaying || anim == null || sequenceCompleted)
            return;

        if (fromTrigger)
        {
            if (playTriggerOnlyOnce && triggerUsedOnce) return;
            if (playTriggerOnlyOnce) triggerUsedOnce = true;
        }
        else
        {
            if (playInteractionOnlyOnce && interactionUsedOnce) return;
            if (playInteractionOnlyOnce) interactionUsedOnce = true;
        }

        bool wasPlaying = IsAnyCinematicPlaying;
        playingCount++;
        NotifyCinematicStateChanged(wasPlaying);

        isPlaying = true;
        allowEvents = true;

        playerController = controller;
        playerCamera = cam;

        if (playerController) playerController.ControlsEnabled = false;
        if (playerCamera) playerCamera.enabled = false;
        if (cinematicCamera) cinematicCamera.enabled = true;

        onCinematicStart?.Invoke();

        string clipName = GetCurrentClipName();
        anim.Play(string.IsNullOrEmpty(clipName) ? null : clipName);

        StartCoroutine(WaitForEnd());
    }

    private IEnumerator WaitForEnd()
    {
        while (anim.isPlaying)
            yield return null;

        Stop();
    }

    private void Stop()
    {
        onCinematicEnd?.Invoke();

        if (DialogueSystem.Instance != null)
            DialogueSystem.Instance.ForceCloseDialogue();

        HandleSequenceAdvance();

        if (cinematicCamera) cinematicCamera.enabled = false;
        if (playerCamera) playerCamera.enabled = true;
        if (playerController) playerController.ControlsEnabled = true;

        bool wasPlaying = IsAnyCinematicPlaying;
        isPlaying = false;
        allowEvents = false;
        playingCount = Mathf.Max(0, playingCount - 1);
        NotifyCinematicStateChanged(wasPlaying);
    }

    private string GetCurrentClipName()
    {
        if (clips != null && clips.Length > 0)
            return clips[Mathf.Clamp(currentClipIndex, 0, clips.Length - 1)]?.name;

        return null;
    }

    private void HandleSequenceAdvance()
    {
        if (!advanceOnEachPlay || clips == null || clips.Length == 0)
            return;

        currentClipIndex++;

        if (currentClipIndex >= clips.Length)
        {
            sequenceCompleted = true;
            if (interactionCollider) interactionCollider.enabled = false;
            if (triggerCollider) triggerCollider.enabled = false;
        }
    }

    // ---------------- ANIMATION EVENTS ----------------

    // ✅ Prova principale: Animation Event con parametro AudioClip
    public void PlaySfx(AudioClip clip)
    {
        if (!allowEvents || audioSource == null || clip == null)
            return;

        audioSource.PlayOneShot(clip);
    }

    // ✅ Fallback: se Unity non mostra PlaySfx(AudioClip) nella lista eventi
    public void PlaySfxObject(Object obj)
    {
        if (!allowEvents || audioSource == null)
            return;

        if (obj is AudioClip clip && clip != null)
            audioSource.PlayOneShot(clip);
    }

    public void ActorSetTrigger(string triggerName)
    {
        if (!allowEvents || actor == null || string.IsNullOrEmpty(triggerName)) return;
        actor.SetTrigger(triggerName);
    }

    public void ActorPlayState(string stateName)
    {
        if (!allowEvents || actor == null || string.IsNullOrEmpty(stateName)) return;
        actor.CrossFade(stateName, 0.1f);
    }

    public void TriggerDialogue(DialogueData data)
    {
        if (!allowEvents || data == null || DialogueSystem.Instance == null) return;
        DialogueSystem.Instance.StartDialogue(data, false, null);
    }

    public void OnTriggerEnteredByPlayer()
    {
        PlayFromTrigger();
    }
}

public class CinematicTriggerForwarder : MonoBehaviour
{
    private CinematicSequence sequence;

    public void Init(CinematicSequence seq)
    {
        sequence = seq;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (sequence == null || !other.CompareTag("Player"))
            return;

        sequence.OnTriggerEnteredByPlayer();
    }
}
