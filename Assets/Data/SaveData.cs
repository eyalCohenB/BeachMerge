using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    public int coins;
    public int bucketTier;
    public List<BoardCellSaveData> boardCells;
}

[System.Serializable]
public class BoardCellSaveData
{
    public int row;
    public int col;
    public string targetItemId;
    public bool isCleared;
}
