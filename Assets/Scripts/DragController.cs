using UnityEngine;
using UnityEngine.InputSystem;

public class DragController : MonoBehaviour
{
    public MergeItem heldItem;
    public Camera mainCamera;
    public InputActionAsset inputActions;

    void Update()
    {
        if (Pointer.current == null) return;

        Vector2 screenPos = Pointer.current.position.ReadValue();
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -mainCamera.transform.position.z));
        worldPos.z = 0f;

        if (Pointer.current.press.wasPressedThisFrame && heldItem == null)
        {
            Collider2D hit = Physics2D.OverlapPoint(worldPos);
            if (hit != null)
            {
                MergeItem item = hit.GetComponent<MergeItem>();
                if (item != null) heldItem = item;
            }
        }
        else if (Pointer.current.press.isPressed && heldItem != null)
        {
            heldItem.transform.position = worldPos;
        }
        else if (Pointer.current.press.wasReleasedThisFrame && heldItem != null)
        {
            MergeItem released = heldItem;
            heldItem = null;
            GameManager.Instance.ResolveDrop(released);
        }
    }
}
