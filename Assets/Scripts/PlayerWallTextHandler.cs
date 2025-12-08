using UnityEngine;
using TMPro;

[RequireComponent(typeof(CharacterController))]
public class PlayerWallTextHandler : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI wallText;

    private string currentMessage;
    private bool hasWallTextThisFrame;

    // Viene chiamato da Unity su OGNI componente sullo stesso GameObject
    // che ha il metodo OnControllerColliderHit, se c'è un CharacterController.
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        var wallTrigger = hit.collider.GetComponent<WallTextTrigger>();
        if (wallTrigger == null)
            return;

        hasWallTextThisFrame = true;

        if (wallText != null && currentMessage != wallTrigger.message)
        {
            currentMessage = wallTrigger.message;
            wallText.text = currentMessage;
        }
    }

    private void LateUpdate()
    {
        // Se in questo frame non abbiamo toccato nessun muro con WallTextTrigger,
        // e c'era un messaggio attivo, lo puliamo.
        if (!hasWallTextThisFrame && !string.IsNullOrEmpty(currentMessage))
        {
            if (wallText != null)
                wallText.text = "";

            currentMessage = null;
        }

        hasWallTextThisFrame = false;
    }
}
