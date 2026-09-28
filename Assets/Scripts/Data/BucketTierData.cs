using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "BucketTierData", menuName = "BucketTierData")]
public class BucketTierData : ScriptableObject
{
    public int tierLevel;
    public Sprite bucketSprite;
    public int upgradeCost;
    public List<BucketDropEntry> possibleDrops;
}
