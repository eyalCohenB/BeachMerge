using UnityEngine;
using System.Collections.Generic;

public class VillagerManager : MonoBehaviour
{
    public ItemData currentRequestItem;
    public int currentReward;
    public List<ItemData> possibleRequestPool;
    public int maxRequestTier = 4;
    public SpriteRenderer requestIcon;

    // Tall screens keep the villager above the board; wide screens have room beside it.
    public Vector3 portraitPosition = new Vector3(-1.6f, 3.9f, 0f);
    public float portraitScale = 0.4f;
    public Vector3 landscapePosition = new Vector3(-4.6f, 0.4f, 0f);
    public float landscapeScale = 0.65f;
    public float landscapeMinAspect = 1.2f;

    Camera mainCamera;
    bool? placedWide;

    public const string SpritePath = "Characters/villager_placeholder";

    void Start()
    {
        mainCamera = Camera.main;
        // Loaded in code so the villager can't vanish if the scene's sprite reference goes stale.
        GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>(SpritePath);
        if (requestIcon == null) CreateRequestIcon();
        FitToScreen();
        RollNewRequest();
    }

    void Update()
    {
        FitToScreen();
    }

    void FitToScreen()
    {
        bool wide = mainCamera.aspect >= landscapeMinAspect;
        if (placedWide == wide) return;

        placedWide = wide;
        transform.position = wide ? landscapePosition : portraitPosition;
        float scale = wide ? landscapeScale : portraitScale;
        transform.localScale = new Vector3(scale, scale, 1f);
        // Move the drop zone with the sprite now, not at the next physics step.
        Physics2D.SyncTransforms();
    }

    void CreateRequestIcon()
    {
        GameObject icon = new GameObject("RequestIcon");
        icon.transform.SetParent(transform, false);
        // Center of the speech bubble in the villager sprite (512px canvas, 100 px/unit, centered pivot).
        icon.transform.localPosition = new Vector3(1.14f, 1.56f, -0.01f);
        icon.transform.localScale = new Vector3(0.34f, 0.34f, 1f);

        requestIcon = icon.AddComponent<SpriteRenderer>();
        requestIcon.sortingOrder = 5;
    }

    public bool TryFulfillRequest(ItemData deliveredItem, CurrencyManager currency)
    {
        if (deliveredItem != currentRequestItem) return false;

        currency.AddCoins(currentReward);
        RollNewRequest();
        return true;
    }

    public void SetRequest(ItemData item)
    {
        currentRequestItem = item;
        currentReward = 10 * item.tier;
        requestIcon.sprite = item.fullSprite;
    }

    public void RollNewRequest()
    {
        List<ItemData> eligible = possibleRequestPool.FindAll(i => i.tier <= maxRequestTier);
        if (eligible.Count == 0) return;

        SetRequest(eligible[Random.Range(0, eligible.Count)]);
    }
}
