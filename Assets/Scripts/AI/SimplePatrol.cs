using UnityEngine;

public class SimplePatrol : MonoBehaviour
{
    [Header("Path")]
    [Tooltip("Punti di patrol che il personaggio seguirà in loop.")]
    [SerializeField] private Transform[] waypoints;

    [Tooltip("Distanza considerata come 'arrivato' al waypoint.")]
    [SerializeField] private float arriveDistance = 0.2f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float rotationSpeed = 5f;

    [Header("Animation (opzionale)")]
    [SerializeField] private Animator animator;
    [SerializeField] private string walkParameterName = "IsWalking";

    private int currentIndex = 0;

    private void Update()
    {
        if (waypoints == null || waypoints.Length == 0)
            return;

        Transform target = waypoints[currentIndex];

        // Direzione verso il prossimo waypoint (solo piano XZ)
        Vector3 toTarget = target.position - transform.position;
        toTarget.y = 0f; // non guardare verso l'alto/basso

        float distance = toTarget.magnitude;

        // Se siamo arrivati vicino al punto, passa al successivo
        if (distance <= arriveDistance)
        {
            currentIndex = (currentIndex + 1) % waypoints.Length;
            target = waypoints[currentIndex];
            toTarget = target.position - transform.position;
            toTarget.y = 0f;
        }

        // Se non c'è direzione (siamo esattamente sopra il punto), esci
        if (toTarget.sqrMagnitude < 0.0001f)
        {
            SetWalking(false);
            return;
        }

        // ROTAZIONE verso il prossimo waypoint
        Quaternion targetRotation = Quaternion.LookRotation(toTarget.normalized, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );

        // MOVIMENTO in avanti nella direzione in cui stai guardando
        Vector3 move = transform.forward * moveSpeed * Time.deltaTime;
        transform.position += move;

        SetWalking(true);
    }

    private void SetWalking(bool value)
    {
        if (animator == null || string.IsNullOrEmpty(walkParameterName))
            return;

        animator.SetBool(walkParameterName, value);
    }

    // Gizmos per vedere i waypoint in scena
    private void OnDrawGizmosSelected()
    {
        if (waypoints == null || waypoints.Length == 0)
            return;

        Gizmos.color = Color.yellow;
        for (int i = 0; i < waypoints.Length; i++)
        {
            if (waypoints[i] == null) continue;

            Gizmos.DrawSphere(waypoints[i].position, 0.1f);

            int next = (i + 1) % waypoints.Length;
            if (waypoints[next] != null)
                Gizmos.DrawLine(waypoints[i].position, waypoints[next].position);
        }
    }
}
