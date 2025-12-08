using UnityEngine;
using System.Collections.Generic;

public class ExamineObject : MonoBehaviour
{
    public static readonly List<ExamineObject> AllExaminables = new();

    public DialogueData dialogue;
    public Collider interactionCollider;

    private bool hasInteracted = false;

    private void Reset()
    {
        interactionCollider = GetComponent<Collider>();
    }

    private void OnEnable() => AllExaminables.Add(this);
    private void OnDisable() => AllExaminables.Remove(this);

    public void Examine()
    {
        if (hasInteracted) return;
        if (dialogue == null || DialogueSystem.Instance == null) return;

        hasInteracted = true;

        // Qui vogliamo bloccare il player finché il dialogo non è finito
        DialogueSystem.Instance.StartDialogue(dialogue, true, () =>
        {
            // callback quando il dialogo finisce
            hasInteracted = false;
        });
    }
}
