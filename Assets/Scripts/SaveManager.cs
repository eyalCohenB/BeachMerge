using UnityEngine;
using System.IO;

public class SaveManager : MonoBehaviour
{
    public static string SavePathOverride;

    public string SavePath => SavePathOverride ?? Path.Combine(Application.persistentDataPath, "save.json");

    public void Save(SaveData data)
    {
        File.WriteAllText(SavePath, JsonUtility.ToJson(data, true));
    }

    public SaveData Load()
    {
        if (!File.Exists(SavePath)) return new SaveData();

        SaveData data = JsonUtility.FromJson<SaveData>(File.ReadAllText(SavePath));
        if (data == null || data.version != SaveData.CurrentVersion)
        {
            Debug.LogWarning("Save file is from an older version, starting a fresh profile.");
            return new SaveData();
        }
        return data;
    }
}
