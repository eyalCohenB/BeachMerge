using UnityEngine;
using System.Collections.Generic;

public class VillagerManager : MonoBehaviour
{
    public ItemData currentRequestItem;
    public int currentReward;
    public List<ItemData> possibleRequestPool;
    public int maxRequestTier = 4;
    public SpriteRenderer requestIcon;

    void Start()
    {
        if (requestIcon == null) CreateRequestIcon();
        RollNewRequest();
    }

    void CreateRequestIcon()
    {
        GameObject icon = new GameObject("RequestIcon");
        icon.transform.SetParent(transform, false);
        // Center of the speech bubble in the villager sprite (512px canvas, 100 px/unit, centered pivot).
        icon.transform.localPosition = new Vector3(1.14f, 1.56f, -0.01f);
        icon.transform.localScale = new Vector3(0.34f, 0.34f, 1f);

        requestIcon = icon.AddComponent<SpriteRenderer>();
        requestIcon.sortingOrder = 5;
    }

    public bool TryFulfillRequest(ItemData deliveredItem, CurrencyManager currency)
    {
        if (deliveredItem != currentRequestItem) return false;

        currency.AddCoins(currentReward);
        RollNewRequest();
        return true;
    }

    public void SetRequest(ItemData item)
    {
        currentRequestItem = item;
        currentReward = 10 * item.tier;
        requestIcon.sprite = item.fullSprite;
    }

    public void RollNewRequest()
    {
        List<ItemData> eligible = possibleRequestPool.FindAll(i => i.tier <= maxRequestTier);
        if (eligible.Count == 0) return;

        SetRequest(eligible[Random.Range(0, eligible.Count)]);
    }
}
