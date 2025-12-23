using UnityEngine;
using Sirenix.OdinInspector;

[RequireComponent(typeof(CharacterController))]
public class PlayerWallTextHandler : MonoBehaviour
{
    // ---------------- EVENTS ----------------
    public event System.Action<string> OnShowWallText;
    public event System.Action OnHideWallText;

    private string currentMessage;
    private bool hasWallTextThisFrame;

    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        var wallTrigger = hit.collider.GetComponent<WallTextTrigger>();
        if (wallTrigger == null)
            return;

        hasWallTextThisFrame = true;

        if (currentMessage != wallTrigger.message)
        {
            currentMessage = wallTrigger.message;
            OnShowWallText?.Invoke(currentMessage);
        }
    }

    private void LateUpdate()
    {
        if (!hasWallTextThisFrame && !string.IsNullOrEmpty(currentMessage))
        {
            currentMessage = null;
            OnHideWallText?.Invoke();
        }

        hasWallTextThisFrame = false;
    }
}
