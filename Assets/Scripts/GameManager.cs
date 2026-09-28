using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public BoardManager boardManager;
    public CurrencyManager currencyManager;
    public VillagerManager villagerManager;
    public Bucket bucket;
    public SaveManager saveManager;

    public float mergeCheckRadius = 0.5f;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        SaveData loaded;
        if (saveManager != null && saveManager.TryLoad(out loaded))
        {
            currencyManager.coins = loaded.coins;
            bucket.SetTierLevel(loaded.bucketTier);
            boardManager.LoadBoard(loaded.boardCells);
        }
        else
        {
            boardManager.SpawnBoard();
        }
    }

    void OnApplicationQuit()
    {
        saveManager?.Save();
    }

    public void ResolveDrop(MergeItem droppedItem)
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(droppedItem.transform.position, mergeCheckRadius);

        foreach (Collider2D hit in hits)
        {
            if (hit.gameObject == droppedItem.gameObject) continue;

            VillagerManager villager = hit.GetComponent<VillagerManager>();
            if (villager != null)
            {
                if (villager.TryFulfillRequest(droppedItem.data, currencyManager))
                {
                    Destroy(droppedItem.gameObject);
                    return;
                }
                continue;
            }

            BoardSlotView slot = hit.GetComponent<BoardSlotView>();
            if (slot != null)
            {
                if (boardManager.TryClearSlot(slot.row, slot.col, droppedItem.data))
                {
                    Destroy(droppedItem.gameObject);
                    return;
                }
                continue;
            }

            MergeItem otherItem = hit.GetComponent<MergeItem>();
            if (otherItem != null && otherItem != droppedItem && otherItem.data == droppedItem.data && otherItem.data.nextTierItem != null)
            {
                ItemData nextTier = otherItem.data.nextTierItem;
                Vector3 mergePos = otherItem.transform.position;

                Destroy(otherItem.gameObject);
                Destroy(droppedItem.gameObject);

                GameObject spawned = Instantiate(bucket.mergeItemPrefab, mergePos, Quaternion.identity);
                spawned.GetComponent<MergeItem>().Setup(nextTier);
                return;
            }
        }
    }
}
