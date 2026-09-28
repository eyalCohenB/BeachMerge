using UnityEngine;
using System.Collections.Generic;

public class BoardManager : MonoBehaviour
{
    public int rows;
    public int columns;
    public GameObject boardSlotPrefab;
    public Transform boardParent;
    public float cellSpacing;
    public BoardCell[,] cells;
    public List<ItemData> allItems;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
