using UnityEngine;
using UnityEngine.InputSystem;

public class PigeonFeedThrower : MonoBehaviour
{
    [Header("Prefab")]
    [SerializeField] private ParticleSystem feedPrefab;   // prefab del particle

    [Header("Spawn")]
    [SerializeField] private Transform spawnPoint;        // es. camera del player
    [SerializeField] private float forwardOffset = 0.5f;  // distanza davanti al player

    [Header("Input")]
    [SerializeField] private InputActionReference throwAction;

    private void OnEnable()
    {
        if (throwAction != null)
            throwAction.action.Enable();
    }

    private void OnDisable()
    {
        if (throwAction != null)
            throwAction.action.Disable();
    }

    private void Update()
    {
        // se il player non ha controllo (cinematic ecc.) non lanciamo
        if (FirstPersonController.Instance != null && !FirstPersonController.Instance.ControlsEnabled)
            return;

        if (throwAction == null || feedPrefab == null)
            return;

        if (throwAction.action.WasPressedThisFrame())
        {
            ThrowFeed();
        }
    }

    private void ThrowFeed()
    {
        // posizione e rotazione di spawn
        Transform refTransform = spawnPoint != null ? spawnPoint : transform;

        Vector3 pos = refTransform.position + refTransform.forward * forwardOffset;
        Quaternion rot = refTransform.rotation;

        // istanzia il prefab
        ParticleSystem instance = Instantiate(feedPrefab, pos, rot);

        // assicuriamoci che parta
        instance.Play();

        // calcoliamo una durata ragionevole per distruggerlo
        var main = instance.main;
        float life = main.duration + main.startLifetime.constantMax + 1f;

        Destroy(instance.gameObject, life);
    }
}
