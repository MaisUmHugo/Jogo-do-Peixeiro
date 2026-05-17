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
        return CanBuyBait(_bait, Mathf.Max(1, _bait != null ? _bait.PurchaseQuantity : 1));
    }

    public bool CanBuyBait(BaitData _bait, int _quantity)
    {
        ResolveReferences();

        if (_bait == null || playerMoneyManager == null || baitInventory == null)
            return false;

        int quantity = Mathf.Max(1, _quantity);
        return playerMoneyManager.PlayerMoney >= GetBaitPurchaseCost(_bait, quantity);
    }

    public int GetBaitPurchaseCost(BaitData _bait)
    {
        if (_bait == null)
            return 0;

        return Mathf.Max(0, _bait.PurchasePrice * _bait.PurchaseQuantity);
    }

    public int GetBaitPurchaseCost(BaitData _bait, int _quantity)
    {
        if (_bait == null)
            return 0;

        int quantity = Mathf.Max(1, _quantity);
        return Mathf.Max(0, _bait.PurchasePrice * quantity);
    }

    public int GetMaxAffordableQuantity(BaitData _bait, int _quantityLimit)
    {
        ResolveReferences();

        if (_bait == null || playerMoneyManager == null)
            return 0;

        int unitCost = Mathf.Max(0, _bait.PurchasePrice);
        int safeLimit = Mathf.Max(0, _quantityLimit);

        if (safeLimit == 0)
            return 0;

        if (unitCost == 0)
            return safeLimit;

        int affordable = Mathf.FloorToInt(playerMoneyManager.PlayerMoney / unitCost);
        return Mathf.Clamp(affordable, 0, safeLimit);
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

    public bool TryBuyBait(BaitData _bait, int _quantity, out BaitPurchaseResult _result)
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

        int quantity = Mathf.Max(1, _quantity);
        int cost = GetBaitPurchaseCost(_bait, quantity);

        if (cost > 0 && !playerMoneyManager.TrySpendMoney(cost))
        {
            _result = BaitPurchaseResult.NotEnoughMoney;
            return false;
        }

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
