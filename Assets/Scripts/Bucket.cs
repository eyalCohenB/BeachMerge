using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class Bucket : MonoBehaviour
{
    public BucketTierData currentTier;
    public List<BucketTierData> allTiers;
    public GameObject mergeItemPrefab;

    private Collider2D col;
    private SpriteRenderer spriteRenderer;
    private Camera mainCamera;

    void Awake()
    {
        col = GetComponent<Collider2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (Pointer.current == null || !Pointer.current.press.wasPressedThisFrame) return;

        Vector2 screenPos = Pointer.current.position.ReadValue();
        Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, -mainCamera.transform.position.z));
        worldPos.z = 0f;

        if (col.OverlapPoint(worldPos)) TrySpawnItem();
    }

    public bool TrySpawnItem()
    {
        BoardManager board = GameManager.Instance.boardManager;
        BoardCell target = board.FindNearestEmptyCell(board.bucketRow, board.bucketCol);
        if (target == null) return false;

        ItemData picked = PickWeightedRandom();
        if (picked == null) return false;

        board.SpawnItemInCell(target, picked);
        return true;
    }

    ItemData PickWeightedRandom()
    {
        if (currentTier == null || currentTier.possibleDrops == null || currentTier.possibleDrops.Count == 0) return null;

        float totalWeight = 0f;
        foreach (BucketDropEntry entry in currentTier.possibleDrops) totalWeight += entry.weight;

        float roll = Random.Range(0f, totalWeight);
        float cumulative = 0f;
        foreach (BucketDropEntry entry in currentTier.possibleDrops)
        {
            cumulative += entry.weight;
            if (roll <= cumulative) return entry.item;
        }

        return currentTier.possibleDrops[currentTier.possibleDrops.Count - 1].item;
    }

    public void Upgrade(BucketTierData nextTier)
    {
        currentTier = nextTier;
        RefreshSprite();
    }

    public void SetTierLevel(int level)
    {
        BucketTierData found = allTiers.Find(t => t.tierLevel == level);
        if (found != null) currentTier = found;
        RefreshSprite();
    }

    void RefreshSprite()
    {
        if (currentTier != null && currentTier.bucketSprite != null) spriteRenderer.sprite = currentTier.bucketSprite;
    }
}
