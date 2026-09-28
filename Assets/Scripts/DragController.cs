using UnityEngine;
using UnityEngine.InputSystem;

public class DragController : MonoBehaviour
{
    public MergeItem heldItem;
    public Camera mainCamera;
    public InputActionAsset inputActions;

    private int heldSortingOrder;

    void Update()
    {
        if (Pointer.current == null) return;

        Vector2 screenPos = Pointer.current.position.ReadValue();
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -mainCamera.transform.position.z));
        worldPos.z = 0f;

        if (Pointer.current.press.wasPressedThisFrame && heldItem == null)
        {
            foreach (Collider2D hit in Physics2D.OverlapPointAll(worldPos))
            {
                MergeItem item = hit.GetComponent<MergeItem>();
                if (item == null || item.boardRow < 0) continue;

                heldItem = item;
                heldSortingOrder = item.spriteRenderer.sortingOrder;
                item.spriteRenderer.sortingOrder = 100;
                break;
            }
        }
        else if (Pointer.current.press.isPressed && heldItem != null)
        {
            heldItem.transform.position = worldPos;
        }
        else if (!Pointer.current.press.isPressed && heldItem != null)
        {
            MergeItem released = heldItem;
            heldItem = null;
            released.spriteRenderer.sortingOrder = heldSortingOrder;
            GameManager.Instance.ResolveDrop(released);
        }
    }
}
