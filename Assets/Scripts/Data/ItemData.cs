using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "ItemData")]
public class ItemData : ScriptableObject
{
    public string id;
    public string displayName;
    public int tier;
    public Sprite fullSprite;
    public Sprite greyedSprite;
    public ItemData nextTierItem;
}
