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
        icon.transform.localPosition = new Vector3(1.2f, 1.75f, -0.01f);
        icon.transform.localScale = new Vector3(0.3f, 0.3f, 1f);

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

    void RollNewRequest()
    {
        List<ItemData> eligible = possibleRequestPool.FindAll(i => i.tier <= maxRequestTier);
        if (eligible.Count == 0) return;

        SetRequest(eligible[Random.Range(0, eligible.Count)]);
    }
}
