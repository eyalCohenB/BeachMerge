using UnityEngine;
using System;

public class CurrencyManager : MonoBehaviour
{
    public int coins;
    public event Action<int> OnCoinsChanged;

    public void SetCoins(int amount)
    {
        coins = amount;
        OnCoinsChanged?.Invoke(coins);
    }

    public void AddCoins(int amount)
    {
        coins += amount;
        OnCoinsChanged?.Invoke(coins);
    }

    public bool SpendCoins(int amount)
    {
        if (coins < amount) return false;
        coins -= amount;
        OnCoinsChanged?.Invoke(coins);
        return true;
    }
}
