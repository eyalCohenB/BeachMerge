using UnityEngine;
using System;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
    public int rows = 5;
    public int columns = 5;
    public GameObject boardSlotPrefab;
    public Transform boardParent;
    public float cellSpacing = 1.2f;
    public List<ItemData> allItems;

    public BoardCell[,] cells;
    private BoardSlotView[,] slotViews;
    private int remainingSlots;

    public event Action OnBoardCleared;

    public void SpawnBoard()
    {
        cells = new BoardCell[rows, columns];
        slotViews = new BoardSlotView[rows, columns];
        remainingSlots = rows * columns;

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                ItemData target = allItems[UnityEngine.Random.Range(0, allItems.Count)];
                SpawnSlot(row, col, target, false);
            }
        }
    }

    public void LoadBoard(List<BoardCellSaveData> savedCells)
    {
        cells = new BoardCell[rows, columns];
        slotViews = new BoardSlotView[rows, columns];
        remainingSlots = 0;

        foreach (BoardCellSaveData saved in savedCells)
        {
            ItemData target = allItems.Find(item => item.id == saved.targetItemId);
            SpawnSlot(saved.row, saved.col, target, saved.isCleared);
        }
    }

    void SpawnSlot(int row, int col, ItemData target, bool isCleared)
    {
        GameObject instance = Instantiate(boardSlotPrefab, boardParent);
        instance.transform.localPosition = GetLocalPosition(row, col);

        BoardSlotView view = instance.GetComponent<BoardSlotView>();
        view.Setup(row, col, target);
        if (isCleared) view.Fill(target);

        cells[row, col] = new BoardCell { row = row, col = col, targetItem = target, isCleared = isCleared };
        slotViews[row, col] = view;

        if (!isCleared) remainingSlots++;
    }

    Vector3 GetLocalPosition(int row, int col)
    {
        float startX = -(columns - 1) * cellSpacing * 0.5f;
        float startY = (rows - 1) * cellSpacing * 0.5f;
        return new Vector3(startX + col * cellSpacing, startY - row * cellSpacing, 0f);
    }

    public bool TryClearSlot(int row, int col, ItemData placedItem)
    {
        BoardCell cell = cells[row, col];
        if (cell == null || cell.isCleared || cell.targetItem != placedItem) return false;

        cell.isCleared = true;
        slotViews[row, col].Fill(placedItem);
        remainingSlots--;

        if (remainingSlots <= 0) OnBoardCleared?.Invoke();

        return true;
    }
}
