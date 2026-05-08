using System;
using System.Collections.Generic;
using UnityEngine;

public class FishMarket : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ShipInventory shipInventory;
    [SerializeField] private PlayerMoneyManager playerMoneyManager;

    public event Action<int> OnSaleCompleted;

    public bool HasFishToSell => shipInventory != null && shipInventory.OwnedFish.Count > 0;
    public ShipInventory ShipInventory
    {
        get
        {
            ResolveReferences();
            return shipInventory;
        }
    }

    public PlayerMoneyManager PlayerMoneyManager
    {
        get
        {
            ResolveReferences();
            return playerMoneyManager;
        }
    }

    private void Awake()
    {
        ResolveReferences();
    }

    public bool TrySellFish(FishData fish, out int earnedMoney)
    {
        earnedMoney = 0;
        ResolveReferences();

        if (fish == null || shipInventory == null || playerMoneyManager == null)
            return false;

        earnedMoney = FishPriceCalculator.CalculatePrice(fish);

        if (!shipInventory.TryRemoveFish(fish))
        {
            earnedMoney = 0;
            return false;
        }

        playerMoneyManager.ReceiveMoney(earnedMoney);
        OnSaleCompleted?.Invoke(earnedMoney);
        return true;
    }

    public bool TrySellAllFish(out int earnedMoney)
    {
        earnedMoney = 0;
        ResolveReferences();

        if (shipInventory == null || playerMoneyManager == null || shipInventory.OwnedFish.Count == 0)
            return false;

        List<FishData> fishToSell = new List<FishData>(shipInventory.OwnedFish);

        foreach (FishData fish in fishToSell)
        {
            earnedMoney += FishPriceCalculator.CalculatePrice(fish);
        }

        if (earnedMoney <= 0)
            return false;

        shipInventory.ClearFish();
        playerMoneyManager.ReceiveMoney(earnedMoney);
        OnSaleCompleted?.Invoke(earnedMoney);
        return true;
    }

    public int GetInventorySaleValue()
    {
        ResolveReferences();
        return shipInventory != null ? shipInventory.GetTotalFishValue() : 0;
    }

    private void ResolveReferences()
    {
        if (shipInventory == null)
            shipInventory = FindFirstObjectByType<ShipInventory>();

        if (playerMoneyManager == null)
            playerMoneyManager = FindFirstObjectByType<PlayerMoneyManager>();
    }
}
