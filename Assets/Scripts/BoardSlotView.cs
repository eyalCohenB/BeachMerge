using UnityEngine;

public class BoardSlotView : MonoBehaviour
{
    public SpriteRenderer greyedRenderer;
    public int row;
    public int col;

    public void Setup(int r, int c, ItemData targetItem)
    {
        row = r;
        col = c;
        greyedRenderer.sprite = targetItem.greyedSprite;
    }

    public void Fill(ItemData placedItem)
    {
        greyedRenderer.sprite = placedItem.fullSprite;
    }
}
