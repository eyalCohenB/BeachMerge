using UnityEngine;

public class BoardSlotView : MonoBehaviour
{
    public SpriteRenderer greyedRenderer;
    public int row;
    public int col;

    public void Setup(int r, int c)
    {
        row = r;
        col = c;
    }

    public void ShowLocked(ItemData targetItem)
    {
        greyedRenderer.sprite = targetItem.greyedSprite;
        greyedRenderer.enabled = true;
    }

    public void ShowEmpty()
    {
        greyedRenderer.enabled = false;
    }
}
