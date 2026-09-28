using UnityEngine;
using System;
using System.Collections.Generic;

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
    public int openRadiusAroundBucket = 1;
    public int maxLockedTier = 4;

    public BoardCell[,] cells;
    private BoardSlotView[,] slotViews;
    private int remainingLocked;

    public event Action OnBoardCleared;

    public int RemainingLocked => remainingLocked;

    public void SpawnBoard()
    {
        ResetBoard();

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if (IsBucketCell(row, col)) continue;

                SpawnSlot(row, col);
                bool nearBucket = Mathf.Max(Mathf.Abs(row - bucketRow), Mathf.Abs(col - bucketCol)) <= openRadiusAroundBucket;
                if (!nearBucket) LockCell(row, col, PickLockedTarget());
            }
        }
    }

    public bool IsValidSave(List<BoardCellSaveData> savedCells)
    {
        if (savedCells == null || savedCells.Count != rows * columns - 1) return false;

        foreach (BoardCellSaveData saved in savedCells)
        {
            if (saved.row < 0 || saved.row >= rows || saved.col < 0 || saved.col >= columns) return false;
            if (IsBucketCell(saved.row, saved.col)) return false;
            if (saved.state == CellState.Empty) continue;
            if (allItems.Find(i => i.id == saved.itemId) == null) return false;
        }
        return true;
    }

    public void LoadBoard(List<BoardCellSaveData> savedCells)
    {
        ResetBoard();

        foreach (BoardCellSaveData saved in savedCells)
        {
            SpawnSlot(saved.row, saved.col);
            ItemData item = allItems.Find(i => i.id == saved.itemId);

            if (saved.state == CellState.Locked)
            {
                LockCell(saved.row, saved.col, item);
            }
            else if (saved.state == CellState.Filled)
            {
                SpawnItemInCell(cells[saved.row, saved.col], item);
            }
        }
    }

    void ResetBoard()
    {
        cells = new BoardCell[rows, columns];
        slotViews = new BoardSlotView[rows, columns];
        remainingLocked = 0;

        GameObject bucketTile = Instantiate(boardSlotPrefab, boardParent);
        bucketTile.name = "BucketTile";
        bucketTile.transform.localPosition = GetLocalPosition(bucketRow, bucketCol);
        BoardSlotView bucketTileView = bucketTile.GetComponent<BoardSlotView>();
        bucketTileView.Setup(bucketRow, bucketCol);
        bucketTileView.ShowEmpty();
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

    void SpawnSlot(int row, int col)
    {
        GameObject instance = Instantiate(boardSlotPrefab, boardParent);
        instance.transform.localPosition = GetLocalPosition(row, col);

        BoardSlotView view = instance.GetComponent<BoardSlotView>();
        view.Setup(row, col);
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

    bool IsBucketCell(int row, int col) => row == bucketRow && col == bucketCol;

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
