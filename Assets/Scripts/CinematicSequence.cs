using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animation))]
public class CinematicSequence : MonoBehaviour
{
    [BoxGroup("Interaction")]
    public string interactionLabel = "Cinematic";

    [BoxGroup("Interaction")]
    public BoxCollider interactionCollider;

    [BoxGroup("Camera")]
    [SerializeField] private Camera cinematicCamera;

    [BoxGroup("Actor")]
    [SerializeField] private Animator actor;

    [BoxGroup("Audio")]
    [SerializeField] private AudioSource audioSource;

    [BoxGroup("Audio")]
    [SerializeField] private AudioClip[] sfxClips;

    [BoxGroup("Optional")]
    public GameObject activateDuringCinematic;

    [BoxGroup("Optional")]
    public DialogueData dialogueToTrigger;

    private Animation anim;
    private bool isPlaying;
    private bool allowEvents;

    private FirstPersonController playerController;
    private Camera playerCamera;

    public static readonly List<CinematicSequence> AllSequences = new();


    private void OnEnable() => AllSequences.Add(this);
    private void OnDisable() => AllSequences.Remove(this);

    public static bool IsAnyCinematicPlaying = false;


    private void Awake()
    {
        anim = GetComponent<Animation>();
        anim.playAutomatically = false;
        anim.Stop();

        if (cinematicCamera)
            cinematicCamera.enabled = false;

        if (interactionCollider)
            interactionCollider.isTrigger = true;

        if (activateDuringCinematic)
            activateDuringCinematic.SetActive(false);

        allowEvents = false;
    }


    public void Play(FirstPersonController controller, Camera pCamera)
    {
        if (isPlaying || anim == null)
            return;

        IsAnyCinematicPlaying = true;   // <--- AGGIUNTO
        isPlaying = true;
        allowEvents = true;

        playerController = controller;
        playerCamera = pCamera;

        if (playerController) playerController.ControlsEnabled = false;
        if (playerCamera) playerCamera.enabled = false;
        if (cinematicCamera) cinematicCamera.enabled = true;
        if (activateDuringCinematic) activateDuringCinematic.SetActive(true);

        anim.Play();
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
        IsAnyCinematicPlaying = false;  // <--- AGGIUNTO

        if (cinematicCamera)
            cinematicCamera.enabled = false;

        if (playerCamera)
            playerCamera.enabled = true;

        if (playerController)
            playerController.ControlsEnabled = true;

        if (activateDuringCinematic)
            activateDuringCinematic.SetActive(false);

        // Chiudi eventuale dialogo aperto ma senza riabilitare controlli
        if (DialogueSystem.Instance != null)
            DialogueSystem.Instance.ForceCloseDialogue();

        isPlaying = false;
        allowEvents = false;
    }


    // ------- Animation Events -------

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
}
