using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BaitStack
{
    public BaitData bait;
    public int quantity;

    public BaitStack(BaitData _bait, int _quantity)
    {
        bait = _bait;
        quantity = Mathf.Max(0, _quantity);
    }
}

public class BaitInventory : MonoBehaviour
{
    public static BaitInventory Instance { get; private set; }

    [SerializeField] private bool persistAcrossScenes;
    [SerializeField] private List<BaitStack> startingBaits = new List<BaitStack>();
    [SerializeField] private BaitData startingEquippedBait;

    private readonly List<BaitStack> baitStacks = new List<BaitStack>();

    public event Action OnBaitInventoryChanged;

    public IReadOnlyList<BaitStack> BaitStacks => baitStacks;
    public BaitData EquippedBait { get; private set; }

    public static BaitInventory GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        Instance = FindFirstObjectByType<BaitInventory>(FindObjectsInactive.Include);

        if (Instance != null)
            return Instance;

        GameObject baitInventoryObject = new GameObject("BaitInventory");
        Instance = baitInventoryObject.AddComponent<BaitInventory>();
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (persistAcrossScenes)
            DontDestroyOnLoad(gameObject);

        InitializeStartingBaits();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public int GetQuantity(BaitData _bait)
    {
        BaitStack stack = FindStack(_bait);
        return stack != null ? stack.quantity : 0;
    }

    public bool HasBait(BaitData _bait)
    {
        return GetQuantity(_bait) > 0;
    }

    public void AddBait(BaitData _bait, int _quantity)
    {
        if (_bait == null || _quantity <= 0)
            return;

        BaitStack stack = FindStack(_bait);

        if (stack == null)
        {
            baitStacks.Add(new BaitStack(_bait, _quantity));
        }
        else
        {
            stack.quantity += _quantity;
        }

        NotifyChanged();
    }

    public bool EquipBait(BaitData _bait)
    {
        if (_bait == null || !HasBait(_bait))
            return false;

        EquippedBait = _bait;
        NotifyChanged();
        return true;
    }

    public void ClearEquippedBait()
    {
        if (EquippedBait == null)
            return;

        EquippedBait = null;
        NotifyChanged();
    }

    public bool TryConsumeEquippedBait()
    {
        if (EquippedBait == null)
            return false;

        return TryConsumeBait(EquippedBait);
    }

    public bool TryConsumeBait(BaitData _bait)
    {
        if (_bait == null)
            return false;

        BaitStack stack = FindStack(_bait);

        if (stack == null || stack.quantity <= 0)
        {
            if (IsEquippedBait(_bait))
                EquippedBait = null;

            NotifyChanged();
            return false;
        }

        stack.quantity--;

        if (stack.quantity <= 0)
        {
            baitStacks.Remove(stack);

            if (IsEquippedBait(_bait))
                EquippedBait = null;
        }

        NotifyChanged();
        return true;
    }

    public void ReplaceBaits(IEnumerable<BaitStack> _baitStacks, BaitData _equippedBait)
    {
        baitStacks.Clear();

        if (_baitStacks != null)
        {
            foreach (BaitStack stack in _baitStacks)
            {
                if (stack == null || stack.bait == null || stack.quantity <= 0)
                    continue;

                AddOrReplaceStack(stack.bait, stack.quantity);
            }
        }

        EquippedBait = _equippedBait != null && GetQuantity(_equippedBait) > 0
            ? _equippedBait
            : null;

        NotifyChanged();
    }

    private void InitializeStartingBaits()
    {
        if (baitStacks.Count > 0)
            return;

        if (startingBaits != null)
        {
            for (int i = 0; i < startingBaits.Count; i++)
            {
                BaitStack stack = startingBaits[i];

                if (stack != null && stack.bait != null && stack.quantity > 0)
                    AddOrReplaceStack(stack.bait, stack.quantity);
            }
        }

        if (startingEquippedBait != null && GetQuantity(startingEquippedBait) > 0)
            EquippedBait = startingEquippedBait;
    }

    private void AddOrReplaceStack(BaitData _bait, int _quantity)
    {
        BaitStack stack = FindStack(_bait);

        if (stack == null)
            baitStacks.Add(new BaitStack(_bait, _quantity));
        else
            stack.quantity = Mathf.Max(0, _quantity);
    }

    private BaitStack FindStack(BaitData _bait)
    {
        if (_bait == null)
            return null;

        for (int i = 0; i < baitStacks.Count; i++)
        {
            BaitStack stack = baitStacks[i];

            if (stack != null && BaitCatalog.BaitIdMatches(stack.bait, _bait.SaveId))
                return stack;
        }

        return null;
    }

    private bool IsEquippedBait(BaitData _bait)
    {
        return EquippedBait != null &&
               _bait != null &&
               BaitCatalog.BaitIdMatches(EquippedBait, _bait.SaveId);
    }

    private void NotifyChanged()
    {
        OnBaitInventoryChanged?.Invoke();
    }
}
