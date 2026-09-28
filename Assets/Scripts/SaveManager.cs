using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class SaveManager : MonoBehaviour
{
    public CurrencyManager currencyManager;
    public Bucket bucket;
    public BoardManager boardManager;

    public static string SavePathOverride;

    private string SavePath => SavePathOverride ?? Path.Combine(Application.persistentDataPath, "save.json");

    public void Save()
    {
        SaveData data = new SaveData
        {
            version = SaveData.CurrentVersion,
            coins = currencyManager.coins,
            bucketTier = bucket.currentTier != null ? bucket.currentTier.tierLevel : 1,
            boardCells = new List<BoardCellSaveData>()
        };

        for (int row = 0; row < boardManager.rows; row++)
        {
            for (int col = 0; col < boardManager.columns; col++)
            {
                BoardCell cell = boardManager.GetCell(row, col);
                if (cell == null) continue;

                data.boardCells.Add(new BoardCellSaveData
                {
                    row = cell.row,
                    col = cell.col,
                    state = cell.state,
                    itemId = cell.item != null ? cell.item.id : null
                });
            }
        }

        File.WriteAllText(SavePath, JsonUtility.ToJson(data));
    }

    public bool TryLoad(out SaveData data)
    {
        if (!File.Exists(SavePath))
        {
            data = null;
            return false;
        }

        data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
        if (data == null || data.version != SaveData.CurrentVersion || !boardManager.IsValidSave(data.boardCells))
        {
            Debug.LogWarning("Save file is outdated or invalid, starting a fresh board.");
            data = null;
            return false;
        }
        return true;
    }

    public void DeleteSave()
    {
        if (File.Exists(SavePath)) File.Delete(SavePath);
    }
}
