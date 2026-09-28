using UnityEngine;

public class MergeItem : MonoBehaviour
{
    public ItemData data;
    public SpriteRenderer spriteRenderer;
    public Collider2D col;

    public int boardRow = -1;
    public int boardCol = -1;

    public void Setup(ItemData itemData)
    {
        data = itemData;
        spriteRenderer.sprite = data.fullSprite;
    }
}
