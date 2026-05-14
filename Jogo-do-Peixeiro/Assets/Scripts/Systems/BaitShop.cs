using System;
using UnityEngine;

public enum BaitPurchaseResult
{
    Purchased,
    MissingReferences,
    InvalidBait,
    NotEnoughMoney
}

public class BaitShop : MonoBehaviour
{
    [SerializeField] private BaitData[] baitsForSale;
    [SerializeField] private PlayerMoneyManager playerMoneyManager;
    [SerializeField] private BaitInventory baitInventory;

    public event Action<BaitData, int> OnBaitPurchased;

    public BaitData[] BaitsForSale => BaitCatalog.GetBaitsOrDefault(baitsForSale);
    public PlayerMoneyManager PlayerMoneyManager
    {
        get
        {
            ResolveReferences();
            return playerMoneyManager;
        }
    }

    public BaitInventory BaitInventory
    {
        get
        {
            ResolveReferences();
            return baitInventory;
        }
    }

    public bool CanBuyBait(BaitData _bait)
    {
        ResolveReferences();

        if (_bait == null || playerMoneyManager == null || baitInventory == null)
            return false;

        return playerMoneyManager.PlayerMoney >= GetBaitPurchaseCost(_bait);
    }

    public int GetBaitPurchaseCost(BaitData _bait)
    {
        if (_bait == null)
            return 0;

        return Mathf.Max(0, _bait.PurchasePrice * _bait.PurchaseQuantity);
    }

    public bool TryBuyBait(BaitData _bait, out BaitPurchaseResult _result)
    {
        ResolveReferences();

        if (_bait == null)
        {
            _result = BaitPurchaseResult.InvalidBait;
            return false;
        }

        if (playerMoneyManager == null || baitInventory == null)
        {
            _result = BaitPurchaseResult.MissingReferences;
            return false;
        }

        int cost = GetBaitPurchaseCost(_bait);

        if (cost > 0 && !playerMoneyManager.TrySpendMoney(cost))
        {
            _result = BaitPurchaseResult.NotEnoughMoney;
            return false;
        }

        int quantity = Mathf.Max(1, _bait.PurchaseQuantity);
        baitInventory.AddBait(_bait, quantity);
        OnBaitPurchased?.Invoke(_bait, quantity);

        _result = BaitPurchaseResult.Purchased;
        return true;
    }

    private void Awake()
    {
        ResolveReferences();
    }

    private void ResolveReferences()
    {
        if (playerMoneyManager == null)
            playerMoneyManager = FindFirstObjectByType<PlayerMoneyManager>();

        if (baitInventory == null)
            baitInventory = BaitInventory.GetOrCreate();
    }
}
