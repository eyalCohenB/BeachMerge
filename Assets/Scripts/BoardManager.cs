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

    public BoardCell[,] cells;
    private BoardSlotView[,] slotViews;
    private int remainingLocked;

    public event Action OnBoardCleared;

    public void SpawnBoard()
    {
        cells = new BoardCell[rows, columns];
        slotViews = new BoardSlotView[rows, columns];
        remainingLocked = 0;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if (row == bucketRow && col == bucketCol) continue;

                SpawnSlot(row, col);
                ItemData target = allItems[UnityEngine.Random.Range(0, allItems.Count)];
                LockCell(row, col, target);
            }
        }
    }

    public void LoadBoard(List<BoardCellSaveData> savedCells)
    {
        cells = new BoardCell[rows, columns];
        slotViews = new BoardSlotView[rows, columns];
        remainingLocked = 0;

        foreach (BoardCellSaveData saved in savedCells)
        {
            SpawnSlot(saved.row, saved.col);
            ItemData item = string.IsNullOrEmpty(saved.itemId) ? null : allItems.Find(i => i.id == saved.itemId);

            if (saved.state == CellState.Locked)
            {
                LockCell(saved.row, saved.col, item);
            }
            else if (saved.state == CellState.Filled)
            {
                GameObject instance = Instantiate(mergeItemPrefab, GetWorldPosition(saved.row, saved.col), Quaternion.identity);
                MergeItem restored = instance.GetComponent<MergeItem>();
                restored.Setup(item);
                PlaceOccupant(cells[saved.row, saved.col], restored);
            }
        }
    }

    void SpawnSlot(int row, int col)
    {
        GameObject instance = Instantiate(boardSlotPrefab, boardParent);
        instance.transform.localPosition = GetLocalPosition(row, col);

        BoardSlotView view = instance.GetComponent<BoardSlotView>();
        view.Setup(row, col);

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
        if (row < 0 || row >= rows || col < 0 || col >= columns) return null;
        return cells[row, col];
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
