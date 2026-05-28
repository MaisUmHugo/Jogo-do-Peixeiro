using System;
using System.Collections.Generic;
using UnityEngine;

public enum DockUpgradePurchaseResult
{
    Purchased,
    MissingReferences,
    MaxLevel,
    NotEnoughMoney,
    AlreadyOwned
}

public enum DockUpgradeType
{
    Capacity,
    BoatSpeed,
    Rod,
    FireproofBoat
}

public class DockUpgradeSystem : MonoBehaviour
{
    private static readonly List<DockUpgradeSystem> instances = new List<DockUpgradeSystem>();
    private static bool hasSharedUpgradeState;
    private static int sharedCapacityLevel;
    private static int sharedBoatSpeedLevel;
    private static int sharedRodLevel;
    private static bool sharedHasFireproofBoatUpgrade;

    [Header("References")]
    [SerializeField] private ShipInventory shipInventory;
    [SerializeField] private PlayerMoneyManager playerMoneyManager;
    [SerializeField] private BoatMotor boatMotor;
    [SerializeField] private FishSkillCheck fishSkillCheck;

    [Header("Boat Capacity")]
    [SerializeField, Min(0)] private int capacityLevel;
    [SerializeField] private float[] capacityByLevel = { 25f, 40f, 65f, 85f, 100f, 120f };
    [SerializeField] private int[] capacityUpgradeCosts = { 130, 260, 400, 660, 1190 };

    [Header("Boat Speed")]
    [SerializeField, Min(0)] private int boatSpeedLevel;
    [SerializeField] private int[] boatSpeedUpgradeCosts = { 190, 380, 590, 975, 1755 };
    [SerializeField, Range(0f, 1f)] private float boatSpeedIncreasePerLevel = 0.15f;

    [Header("Rod")]
    [SerializeField, Min(0)] private int rodLevel;
    [SerializeField] private int[] rodUpgradeCosts = { 150, 300, 465, 767, 1380 };
    [SerializeField, Range(0f, 1f)] private float rodIndicatorSpeedReductionPerLevel = 0.1f;
    [SerializeField, Range(0f, 1f)] private float rodSuccessZoneIncreasePerLevel = 0.15f;

    [Header("Special")]
    [SerializeField] private bool hasFireproofBoatUpgrade;
    [SerializeField, Min(0)] private int fireproofBoatUpgradeCost = 1000;

    public event Action OnUpgradesChanged;

    public int CapacityLevel => capacityLevel;
    public int MaxCapacityLevel => Mathf.Max(0, capacityByLevel.Length - 1);
    public float CurrentCapacity => GetCapacityForLevel(capacityLevel);
    public float NextCapacity => GetCapacityForLevel(Mathf.Min(capacityLevel + 1, MaxCapacityLevel));
    public bool IsCapacityUpgradeMaxed => capacityLevel >= MaxCapacityLevel;

    public int BoatSpeedLevel => boatSpeedLevel;
    public int MaxBoatSpeedLevel => boatSpeedUpgradeCosts != null ? boatSpeedUpgradeCosts.Length : 0;
    public float BoatSpeedMultiplier => 1f + boatSpeedIncreasePerLevel * boatSpeedLevel;
    public float NextBoatSpeedMultiplier => 1f + boatSpeedIncreasePerLevel * Mathf.Min(boatSpeedLevel + 1, MaxBoatSpeedLevel);
    public bool IsBoatSpeedUpgradeMaxed => boatSpeedLevel >= MaxBoatSpeedLevel;

    public int RodLevel => rodLevel;
    public int MaxRodLevel => rodUpgradeCosts != null ? rodUpgradeCosts.Length : 0;
    public float RodIndicatorSpeedMultiplier => Mathf.Max(0.01f, 1f - rodIndicatorSpeedReductionPerLevel * rodLevel);
    public float NextRodIndicatorSpeedMultiplier => Mathf.Max(0.01f, 1f - rodIndicatorSpeedReductionPerLevel * Mathf.Min(rodLevel + 1, MaxRodLevel));
    public float RodSuccessZoneMultiplier => 1f + rodSuccessZoneIncreasePerLevel * rodLevel;
    public float NextRodSuccessZoneMultiplier => 1f + rodSuccessZoneIncreasePerLevel * Mathf.Min(rodLevel + 1, MaxRodLevel);
    public bool IsRodUpgradeMaxed => rodLevel >= MaxRodLevel;

    public bool HasFireproofBoatUpgrade => hasFireproofBoatUpgrade;
    public int FireproofBoatUpgradeCost => fireproofBoatUpgradeCost;

    public int CurrentCapacityUpgradeCost
    {
        get
        {
            if (IsCapacityUpgradeMaxed)
                return 0;

            return GetCostAtLevel(capacityUpgradeCosts, capacityLevel);
        }
    }

    public int CurrentBoatSpeedUpgradeCost => IsBoatSpeedUpgradeMaxed ? 0 : GetCostAtLevel(boatSpeedUpgradeCosts, boatSpeedLevel);
    public int CurrentRodUpgradeCost => IsRodUpgradeMaxed ? 0 : GetCostAtLevel(rodUpgradeCosts, rodLevel);

    public bool CanBuyCapacityUpgrade
    {
        get
        {
            ResolveReferences();
            return CanBuyUpgrade(CurrentCapacityUpgradeCost, !IsCapacityUpgradeMaxed, shipInventory != null);
        }
    }

    public bool CanBuyBoatSpeedUpgrade
    {
        get
        {
            ResolveReferences();
            return CanBuyUpgrade(CurrentBoatSpeedUpgradeCost, !IsBoatSpeedUpgradeMaxed, boatMotor != null);
        }
    }

    public bool CanBuyRodUpgrade
    {
        get
        {
            ResolveReferences();
            return CanBuyUpgrade(CurrentRodUpgradeCost, !IsRodUpgradeMaxed, fishSkillCheck != null);
        }
    }

    public bool CanBuyFireproofBoatUpgrade
    {
        get
        {
            ResolveReferences();
            return CanBuyUpgrade(fireproofBoatUpgradeCost, !hasFireproofBoatUpgrade, true);
        }
    }

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
        RegisterInstance();

        if (hasSharedUpgradeState)
            ApplySharedUpgradeState(false);
        else
            StoreSharedUpgradeState(capacityLevel, boatSpeedLevel, rodLevel, hasFireproofBoatUpgrade);

        ApplyUpgrades();
    }

    private void OnDestroy()
    {
        instances.Remove(this);
    }

    private void OnValidate()
    {
        EnsureCapacityTable();

        capacityLevel = Mathf.Clamp(capacityLevel, 0, MaxCapacityLevel);
        boatSpeedLevel = Mathf.Clamp(boatSpeedLevel, 0, MaxBoatSpeedLevel);
        rodLevel = Mathf.Clamp(rodLevel, 0, MaxRodLevel);

        ClampCosts(capacityUpgradeCosts);
        ClampCosts(boatSpeedUpgradeCosts);
        ClampCosts(rodUpgradeCosts);

        fireproofBoatUpgradeCost = Mathf.Max(0, fireproofBoatUpgradeCost);
    }

    public bool TryBuyCapacityUpgrade(out DockUpgradePurchaseResult _result)
    {
        ResolveReferences();

        if (!TryPurchase(CurrentCapacityUpgradeCost, !IsCapacityUpgradeMaxed, shipInventory != null, out _result))
            return false;

        capacityLevel++;
        ApplyCapacityUpgrade();
        PublishUpgradeState();

        _result = DockUpgradePurchaseResult.Purchased;
        return true;
    }

    public bool TryBuyBoatSpeedUpgrade(out DockUpgradePurchaseResult _result)
    {
        ResolveReferences();

        if (!TryPurchase(CurrentBoatSpeedUpgradeCost, !IsBoatSpeedUpgradeMaxed, boatMotor != null, out _result))
            return false;

        boatSpeedLevel++;
        ApplyBoatSpeedUpgrade();
        PublishUpgradeState();

        _result = DockUpgradePurchaseResult.Purchased;
        return true;
    }

    public bool TryBuyRodUpgrade(out DockUpgradePurchaseResult _result)
    {
        ResolveReferences();

        if (!TryPurchase(CurrentRodUpgradeCost, !IsRodUpgradeMaxed, fishSkillCheck != null, out _result))
            return false;

        rodLevel++;
        ApplyRodUpgrade();
        PublishUpgradeState();

        _result = DockUpgradePurchaseResult.Purchased;
        return true;
    }

    public bool TryBuyFireproofBoatUpgrade(out DockUpgradePurchaseResult _result)
    {
        ResolveReferences();

        if (hasFireproofBoatUpgrade)
        {
            _result = DockUpgradePurchaseResult.AlreadyOwned;
            return false;
        }

        if (!TryPurchase(fireproofBoatUpgradeCost, true, true, out _result))
            return false;

        hasFireproofBoatUpgrade = true;
        PublishUpgradeState();

        _result = DockUpgradePurchaseResult.Purchased;
        return true;
    }

    public void ApplyUpgrades()
    {
        ResolveReferences();
        ApplyCapacityUpgrade();
        ApplyBoatSpeedUpgrade();
        ApplyRodUpgrade();
    }

    public void SetUpgradeState(int _capacityLevel, int _boatSpeedLevel, int _rodLevel, bool _hasFireproofBoatUpgrade)
    {
        SetSharedUpgradeState(_capacityLevel, _boatSpeedLevel, _rodLevel, _hasFireproofBoatUpgrade);
    }

    public static bool TryGetSharedUpgradeState(
        out int _capacityLevel,
        out int _boatSpeedLevel,
        out int _rodLevel,
        out bool _hasFireproofBoatUpgrade)
    {
        _capacityLevel = sharedCapacityLevel;
        _boatSpeedLevel = sharedBoatSpeedLevel;
        _rodLevel = sharedRodLevel;
        _hasFireproofBoatUpgrade = sharedHasFireproofBoatUpgrade;
        return hasSharedUpgradeState;
    }

    public static void SetSharedUpgradeState(int _capacityLevel, int _boatSpeedLevel, int _rodLevel, bool _hasFireproofBoatUpgrade)
    {
        StoreSharedUpgradeState(_capacityLevel, _boatSpeedLevel, _rodLevel, _hasFireproofBoatUpgrade);
        ApplySharedUpgradeStateToAll(true);
    }

    public static void ResetSharedUpgradeState()
    {
        SetSharedUpgradeState(0, 0, 0, false);
    }

    private static void StoreSharedUpgradeState(int _capacityLevel, int _boatSpeedLevel, int _rodLevel, bool _hasFireproofBoatUpgrade)
    {
        hasSharedUpgradeState = true;
        sharedCapacityLevel = Mathf.Max(0, _capacityLevel);
        sharedBoatSpeedLevel = Mathf.Max(0, _boatSpeedLevel);
        sharedRodLevel = Mathf.Max(0, _rodLevel);
        sharedHasFireproofBoatUpgrade = _hasFireproofBoatUpgrade;
    }

    private static void ApplySharedUpgradeStateToAll(bool _notify)
    {
        DockUpgradeSystem[] snapshot = instances.ToArray();

        for (int i = 0; i < snapshot.Length; i++)
        {
            DockUpgradeSystem system = snapshot[i];

            if (system != null)
                system.ApplySharedUpgradeState(_notify);
        }
    }

    private void RegisterInstance()
    {
        if (!instances.Contains(this))
            instances.Add(this);
    }

    private void PublishUpgradeState()
    {
        SetSharedUpgradeState(capacityLevel, boatSpeedLevel, rodLevel, hasFireproofBoatUpgrade);
    }

    private void ApplySharedUpgradeState(bool _notify)
    {
        if (!hasSharedUpgradeState)
            return;

        SetUpgradeStateLocal(sharedCapacityLevel, sharedBoatSpeedLevel, sharedRodLevel, sharedHasFireproofBoatUpgrade, _notify);
    }

    private void SetUpgradeStateLocal(int _capacityLevel, int _boatSpeedLevel, int _rodLevel, bool _hasFireproofBoatUpgrade, bool _notify)
    {
        EnsureCapacityTable();

        capacityLevel = Mathf.Clamp(_capacityLevel, 0, MaxCapacityLevel);
        boatSpeedLevel = Mathf.Clamp(_boatSpeedLevel, 0, MaxBoatSpeedLevel);
        rodLevel = Mathf.Clamp(_rodLevel, 0, MaxRodLevel);
        hasFireproofBoatUpgrade = _hasFireproofBoatUpgrade;

        ApplyUpgrades();

        if (_notify)
            OnUpgradesChanged?.Invoke();
    }

    private void ResolveReferences()
    {
        if (shipInventory == null)
            shipInventory = FindFirstObjectByType<ShipInventory>();

        if (playerMoneyManager == null)
            playerMoneyManager = FindFirstObjectByType<PlayerMoneyManager>();

        if (boatMotor == null)
            boatMotor = FindFirstObjectByType<BoatMotor>();

        if (fishSkillCheck == null)
            fishSkillCheck = FindFirstObjectByType<FishSkillCheck>(FindObjectsInactive.Include);
    }

    private void ApplyCapacityUpgrade()
    {
        if (shipInventory != null)
            shipInventory.SetMaxCapacity(CurrentCapacity);
    }

    private void ApplyBoatSpeedUpgrade()
    {
        if (boatMotor != null)
            boatMotor.SetSpeedUpgradeMultiplier(BoatSpeedMultiplier);
    }

    private void ApplyRodUpgrade()
    {
        if (fishSkillCheck != null)
            fishSkillCheck.SetUpgradeModifiers(RodIndicatorSpeedMultiplier, RodSuccessZoneMultiplier);
    }

    private bool TryPurchase(int _cost, bool _canUpgrade, bool _hasRequiredReference, out DockUpgradePurchaseResult _result)
    {
        ResolveReferences();

        if (playerMoneyManager == null || !_hasRequiredReference)
        {
            _result = DockUpgradePurchaseResult.MissingReferences;
            return false;
        }

        if (!_canUpgrade)
        {
            _result = DockUpgradePurchaseResult.MaxLevel;
            return false;
        }

        if (_cost > 0 && !playerMoneyManager.TrySpendMoney(_cost))
        {
            _result = DockUpgradePurchaseResult.NotEnoughMoney;
            return false;
        }

        _result = DockUpgradePurchaseResult.Purchased;
        return true;
    }

    private bool CanBuyUpgrade(int _cost, bool _canUpgrade, bool _hasRequiredReference)
    {
        return _canUpgrade &&
               _hasRequiredReference &&
               playerMoneyManager != null &&
               playerMoneyManager.PlayerMoney >= _cost;
    }

    private float GetCapacityForLevel(int _level)
    {
        EnsureCapacityTable();
        int level = Mathf.Clamp(_level, 0, capacityByLevel.Length - 1);
        return capacityByLevel[level];
    }

    private int GetCostAtLevel(int[] _costs, int _level)
    {
        if (_costs == null || _costs.Length == 0)
            return 0;

        int level = Mathf.Clamp(_level, 0, _costs.Length - 1);
        return Mathf.Max(0, _costs[level]);
    }

    private void EnsureCapacityTable()
    {
        if (capacityByLevel == null || capacityByLevel.Length < 2)
            capacityByLevel = new[] { 25f, 40f, 65f, 85f, 100f, 120f };

        for (int i = 0; i < capacityByLevel.Length; i++)
            capacityByLevel[i] = Mathf.Max(0f, capacityByLevel[i]);
    }

    private void ClampCosts(int[] _costs)
    {
        if (_costs == null)
            return;

        for (int i = 0; i < _costs.Length; i++)
            _costs[i] = Mathf.Max(0, _costs[i]);
    }
}
