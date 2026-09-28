using UnityEngine;
using System.Collections.Generic;

public class VillagerManager : MonoBehaviour
{
    public ItemData currentRequestItem;
    public int currentReward;
    public List<ItemData> possibleRequestPool;

    void Start()
    {
        RollNewRequest();
    }

    public bool TryFulfillRequest(ItemData deliveredItem, CurrencyManager currency)
    {
        if (deliveredItem != currentRequestItem) return false;

        currency.AddCoins(currentReward);
        RollNewRequest();
        return true;
    }

    void RollNewRequest()
    {
        if (possibleRequestPool == null || possibleRequestPool.Count == 0) return;

        currentRequestItem = possibleRequestPool[Random.Range(0, possibleRequestPool.Count)];
        currentReward = 10 * currentRequestItem.tier;
    }
}
