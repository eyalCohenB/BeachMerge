using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public BoardManager boardManager;
    public CurrencyManager currencyManager;
    public VillagerManager villagerManager;
    public Bucket bucket;
    public SaveManager saveManager;

    public float mergeCheckRadius = 0.4f;

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
            bucket.SetTierLevel(1);
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
            VillagerManager villager = hit.GetComponent<VillagerManager>();
            if (villager != null && villager.TryFulfillRequest(droppedItem.data, currencyManager))
            {
                boardManager.FreeCell(droppedItem.boardRow, droppedItem.boardCol);
                Destroy(droppedItem.gameObject);
                return;
            }
        }

        BoardSlotView slot = FindClosestSlot(hits, droppedItem.transform.position);
        if (slot != null && TryResolveCell(slot.row, slot.col, droppedItem)) return;

        droppedItem.transform.position = boardManager.GetWorldPosition(droppedItem.boardRow, droppedItem.boardCol);
    }

    BoardSlotView FindClosestSlot(Collider2D[] hits, Vector3 position)
    {
        BoardSlotView closest = null;
        float closestDistance = float.MaxValue;

        foreach (Collider2D hit in hits)
        {
            BoardSlotView slot = hit.GetComponent<BoardSlotView>();
            if (slot == null) continue;

            float distance = (hit.transform.position - position).sqrMagnitude;
            if (distance < closestDistance)
            {
                closest = slot;
                closestDistance = distance;
            }
        }
        return closest;
    }

    bool TryResolveCell(int row, int col, MergeItem droppedItem)
    {
        BoardCell target = boardManager.GetCell(row, col);
        if (target == null) return false;
        if (row == droppedItem.boardRow && col == droppedItem.boardCol) return false;

        bool sameItem = target.item == droppedItem.data;

        if (target.state == CellState.Empty)
        {
            boardManager.FreeCell(droppedItem.boardRow, droppedItem.boardCol);
            boardManager.PlaceOccupant(target, droppedItem);
            return true;
        }

        if (!sameItem) return false;

        ItemData result = droppedItem.data.nextTierItem;
        if (result == null && target.state == CellState.Filled) return false;
        if (result == null) result = droppedItem.data;

        boardManager.FreeCell(droppedItem.boardRow, droppedItem.boardCol);
        Destroy(droppedItem.gameObject);
        if (target.occupant != null) Destroy(target.occupant.gameObject);

        boardManager.SpawnItemInCell(target, result);
        return true;
    }
}
