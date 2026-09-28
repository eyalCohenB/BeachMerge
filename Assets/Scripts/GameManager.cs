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

    public void ResolveDrop(MergeItem droppedItem, Vector3 fallbackPosition)
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
                    ClearOrigin(droppedItem);
                    Destroy(droppedItem.gameObject);
                    return;
                }
                continue;
            }

            BoardSlotView slot = hit.GetComponent<BoardSlotView>();
            if (slot != null)
            {
                if (TryResolveCell(slot.row, slot.col, droppedItem)) return;
                continue;
            }
        }

        droppedItem.transform.position = fallbackPosition;
    }

    bool TryResolveCell(int row, int col, MergeItem droppedItem)
    {
        BoardCell cell = boardManager.GetCell(row, col);
        if (cell == null) return false;

        if (row == droppedItem.boardRow && col == droppedItem.boardCol)
        {
            droppedItem.transform.position = boardManager.GetWorldPosition(row, col);
            return true;
        }

        if (cell.state == CellState.Locked && cell.item == droppedItem.data)
        {
            ClearOrigin(droppedItem);
            boardManager.PlaceOccupant(cell, droppedItem);
            return true;
        }

        if (cell.state == CellState.Filled && cell.item == droppedItem.data && cell.item.nextTierItem != null)
        {
            ItemData nextTier = cell.item.nextTierItem;
            MergeItem existing = cell.occupant;

            ClearOrigin(droppedItem);
            Destroy(existing.gameObject);
            Destroy(droppedItem.gameObject);

            GameObject spawned = Instantiate(bucket.mergeItemPrefab, boardManager.GetWorldPosition(row, col), Quaternion.identity);
            MergeItem merged = spawned.GetComponent<MergeItem>();
            merged.Setup(nextTier);
            boardManager.PlaceOccupant(cell, merged);
            return true;
        }

        if (cell.state == CellState.Empty)
        {
            ClearOrigin(droppedItem);
            boardManager.PlaceOccupant(cell, droppedItem);
            return true;
        }

        return false;
    }

    void ClearOrigin(MergeItem item)
    {
        if (item.boardRow >= 0 && item.boardCol >= 0)
        {
            boardManager.FreeCell(item.boardRow, item.boardCol);
        }
    }
}
