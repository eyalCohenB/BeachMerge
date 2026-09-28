using System;
using UnityEngine;

public enum GameState { Menu, Playing, Finished }

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public BoardManager boardManager;
    public CurrencyManager currencyManager;
    public VillagerManager villagerManager;
    public Bucket bucket;
    public SaveManager saveManager;

    public float mergeCheckRadius = 0.4f;

    public GameState State { get; private set; } = GameState.Menu;
    public bool MusicOn { get; private set; } = true;
    public int CoinsEarnedThisRun => currencyManager.coins - coinsAtRunStart;
    public event Action<GameState> OnStateChanged;
    public event Action OnBucketChanged;

    public UIManager UI { get; private set; }
    public MusicPlayer Music { get; private set; }

    int coinsAtRunStart;
    bool loaded;

    void Awake()
    {
        Instance = this;
        Music = gameObject.AddComponent<MusicPlayer>();
        UI = gameObject.AddComponent<UIManager>();
    }

    void Start()
    {
        SaveData profile = saveManager.Load();
        currencyManager.SetCoins(profile.coins);
        bucket.SetTierLevel(profile.bucketTier);

        currencyManager.OnCoinsChanged += _ => SaveProgress();
        boardManager.OnBoardCleared += () => SetState(GameState.Finished);

        Music.Init();
        SetMusic(profile.musicOn);
        UI.Init(this);

        loaded = true;
        SetState(GameState.Menu);
    }

    void OnApplicationQuit()
    {
        SaveProgress();
    }

    void SetState(GameState state)
    {
        State = state;
        OnStateChanged?.Invoke(state);
    }

    public void StartGame()
    {
        coinsAtRunStart = currencyManager.coins;
        boardManager.SpawnBoard();
        villagerManager.RollNewRequest();
        SetState(GameState.Playing);
    }

    public void ReturnToMenu()
    {
        boardManager.ClearBoard();
        SetState(GameState.Menu);
    }

    public BucketTierData NextBucketTier =>
        bucket.currentTier == null ? null : bucket.allTiers.Find(t => t.tierLevel == bucket.currentTier.tierLevel + 1);

    public int UpgradeCost => bucket.currentTier != null ? bucket.currentTier.upgradeCost : 0;

    public bool CanUpgradeBucket => NextBucketTier != null && currencyManager.coins >= UpgradeCost;

    public bool TryUpgradeBucket()
    {
        BucketTierData next = NextBucketTier;
        if (next == null || !currencyManager.SpendCoins(UpgradeCost)) return false;

        bucket.Upgrade(next);
        SaveProgress();
        OnBucketChanged?.Invoke();
        return true;
    }

    public void ResetProgress()
    {
        bucket.SetTierLevel(1);
        currencyManager.SetCoins(0);
        SaveProgress();
        OnBucketChanged?.Invoke();
    }

    public void SetMusic(bool on)
    {
        MusicOn = on;
        Music.SetPlaying(on);
        SaveProgress();
    }

    void SaveProgress()
    {
        if (!loaded) return;

        saveManager.Save(new SaveData
        {
            coins = currencyManager.coins,
            bucketTier = bucket.currentTier != null ? bucket.currentTier.tierLevel : 1,
            musicOn = MusicOn
        });
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

        if (target.state == CellState.Empty)
        {
            boardManager.FreeCell(droppedItem.boardRow, droppedItem.boardCol);
            boardManager.PlaceOccupant(target, droppedItem);
            return true;
        }

        if (target.item != droppedItem.data) return false;

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
