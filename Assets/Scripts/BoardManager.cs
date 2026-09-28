using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class BoardManager : MonoBehaviour
{
    public int rows = 5;
    public int columns = 5;
    public GameObject boardSlotPrefab;
    public GameObject mergeItemPrefab;
    public Transform boardParent;
    public float cellSpacing = 1.2f;
    public List<ItemData> allItems;
    public int bucketRow = 2;
    public int bucketCol = 2;
    public int startingEmptyCells = 2;
    public int maxLockedTier = 7;
    // Tiers every board is guaranteed to contain as greyed items: a Pearl and a Starfish to aim for,
    // and a few Pebbles so the opening moves are never stuck.
    public int[] guaranteedLockedTiers = { 7, 5, 1, 1, 1 };

    public BoardCell[,] cells;
    private BoardSlotView[,] slotViews;
    private readonly List<GameObject> spawnedTiles = new List<GameObject>();
    private int remainingLocked;

    public event Action OnBoardCleared;
    public event Action OnLockedCountChanged;

    public int RemainingLocked => remainingLocked;

    public void SpawnBoard()
    {
        ClearBoard();
        cells = new BoardCell[rows, columns];
        slotViews = new BoardSlotView[rows, columns];

        SpawnTile(bucketRow, bucketCol).ShowEmpty();

        var cellsToLock = new List<BoardCell>();
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if (row == bucketRow && col == bucketCol) continue;
                SpawnSlot(row, col);
                cellsToLock.Add(cells[row, col]);
            }
        }

        // Leave a few cells right next to the bucket open; everything else starts greyed out.
        var nextToBucket = cellsToLock
            .Where(c => Mathf.Abs(c.row - bucketRow) + Mathf.Abs(c.col - bucketCol) == 1)
            .OrderBy(_ => UnityEngine.Random.value)
            .Take(startingEmptyCells);
        foreach (BoardCell open in nextToBucket.ToList()) cellsToLock.Remove(open);

        cellsToLock = cellsToLock.OrderBy(_ => UnityEngine.Random.value).ToList();
        for (int i = 0; i < cellsToLock.Count; i++)
        {
            ItemData guaranteed = i < guaranteedLockedTiers.Length ? allItems.Find(it => it.tier == guaranteedLockedTiers[i]) : null;
            LockCell(cellsToLock[i].row, cellsToLock[i].col, guaranteed ?? PickLockedTarget());
        }
        OnLockedCountChanged?.Invoke();
    }

    public void ClearBoard()
    {
        if (cells != null)
        {
            foreach (BoardCell cell in cells)
            {
                if (cell?.occupant != null) Destroy(cell.occupant.gameObject);
            }
        }
        foreach (GameObject tile in spawnedTiles)
        {
            if (tile != null) Destroy(tile);
        }
        spawnedTiles.Clear();
        cells = null;
        slotViews = null;
        remainingLocked = 0;
    }

    ItemData PickLockedTarget()
    {
        float total = 0f;
        foreach (ItemData item in allItems)
        {
            if (item.tier <= maxLockedTier) total += maxLockedTier + 1 - item.tier;
        }

        float roll = UnityEngine.Random.Range(0f, total);
        foreach (ItemData item in allItems)
        {
            if (item.tier > maxLockedTier) continue;
            roll -= maxLockedTier + 1 - item.tier;
            if (roll <= 0f) return item;
        }
        return allItems[0];
    }

    BoardSlotView SpawnTile(int row, int col)
    {
        GameObject instance = Instantiate(boardSlotPrefab, boardParent);
        instance.transform.localPosition = GetLocalPosition(row, col);
        spawnedTiles.Add(instance);

        BoardSlotView view = instance.GetComponent<BoardSlotView>();
        view.Setup(row, col);
        return view;
    }

    void SpawnSlot(int row, int col)
    {
        BoardSlotView view = SpawnTile(row, col);
        view.ShowEmpty();

        cells[row, col] = new BoardCell { row = row, col = col, state = CellState.Empty };
        slotViews[row, col] = view;
    }

    void LockCell(int row, int col, ItemData target)
    {
        BoardCell cell = cells[row, col];
        cell.state = CellState.Locked;
        cell.item = target;
        slotViews[row, col].ShowLocked(target);
        remainingLocked++;
    }

    Vector3 GetLocalPosition(int row, int col)
    {
        float startX = -(columns - 1) * cellSpacing * 0.5f;
        float startY = (rows - 1) * cellSpacing * 0.5f;
        return new Vector3(startX + col * cellSpacing, startY - row * cellSpacing, 0f);
    }

    public Vector3 GetWorldPosition(int row, int col)
    {
        return boardParent.TransformPoint(GetLocalPosition(row, col));
    }

    public BoardCell GetCell(int row, int col)
    {
        if (cells == null || row < 0 || row >= rows || col < 0 || col >= columns) return null;
        return cells[row, col];
    }

    public BoardCell FindNearestEmptyCell(int fromRow, int fromCol)
    {
        if (cells == null) return null;

        BoardCell best = null;
        int bestDistance = int.MaxValue;

        foreach (BoardCell cell in cells)
        {
            if (cell == null || cell.state != CellState.Empty) continue;

            int distance = Mathf.Max(Mathf.Abs(cell.row - fromRow), Mathf.Abs(cell.col - fromCol));
            if (distance < bestDistance)
            {
                best = cell;
                bestDistance = distance;
            }
        }
        return best;
    }

    public MergeItem SpawnItemInCell(BoardCell cell, ItemData item)
    {
        GameObject instance = Instantiate(mergeItemPrefab, GetWorldPosition(cell.row, cell.col), Quaternion.identity);
        MergeItem mergeItem = instance.GetComponent<MergeItem>();
        mergeItem.Setup(item);
        PlaceOccupant(cell, mergeItem);
        return mergeItem;
    }

    public void PlaceOccupant(BoardCell cell, MergeItem item)
    {
        bool wasLocked = cell.state == CellState.Locked;

        cell.state = CellState.Filled;
        cell.item = item.data;
        cell.occupant = item;

        item.boardRow = cell.row;
        item.boardCol = cell.col;
        item.transform.position = GetWorldPosition(cell.row, cell.col);

        slotViews[cell.row, cell.col].ShowEmpty();

        if (wasLocked)
        {
            remainingLocked--;
            OnLockedCountChanged?.Invoke();
            if (remainingLocked <= 0) OnBoardCleared?.Invoke();
        }
    }

    public void FreeCell(int row, int col)
    {
        BoardCell cell = GetCell(row, col);
        if (cell == null) return;

        cell.state = CellState.Empty;
        cell.item = null;
        cell.occupant = null;
        slotViews[row, col].ShowEmpty();
    }
}
