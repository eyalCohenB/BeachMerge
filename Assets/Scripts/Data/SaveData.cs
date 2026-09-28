[System.Serializable]
public class SaveData
{
    public const int CurrentVersion = 3;

    public int version = CurrentVersion;
    public int coins;
    public int bucketTier = 1;
    public bool musicOn = true;
}
