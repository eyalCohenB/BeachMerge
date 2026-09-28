using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    public const int CurrentVersion = 2;

    public int version;
    public int coins;
    public int bucketTier;
    public List<BoardCellSaveData> boardCells;
}

[System.Serializable]
public class BoardCellSaveData
{
    public int row;
    public int col;
    public CellState state;
    public string itemId;
}
