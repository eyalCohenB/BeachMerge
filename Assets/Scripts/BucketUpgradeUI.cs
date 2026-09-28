using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BucketUpgradeUI : MonoBehaviour
{
    public Bucket bucket;
    public CurrencyManager currencyManager;
    public Button upgradeButton;
    public TextMeshProUGUI costText;

    void Start()
    {
        upgradeButton.onClick.AddListener(OnUpgradeClicked);
    }

    void Update()
    {
        BucketTierData next = GetNextTier();
        gameObject.SetActive(next != null);
        if (next == null) return;

        costText.text = next.upgradeCost.ToString();
        upgradeButton.interactable = currencyManager.coins >= next.upgradeCost;
    }

    BucketTierData GetNextTier()
    {
        if (bucket.currentTier == null) return null;
        return bucket.allTiers.Find(t => t.tierLevel == bucket.currentTier.tierLevel + 1);
    }

    void OnUpgradeClicked()
    {
        BucketTierData next = GetNextTier();
        if (next == null) return;

        if (currencyManager.SpendCoins(next.upgradeCost))
        {
            bucket.Upgrade(next);
        }
    }
}
