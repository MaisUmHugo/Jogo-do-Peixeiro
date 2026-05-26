using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class InventoryDebugCheats : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ShipInventory shipInventory;
    [SerializeField] private BaitInventory baitInventory;

    [Header("Fish")]
    [SerializeField] private FishScriptableObject[] fishPool;
    [SerializeField, Min(1)] private int randomFishAmount = 1;
    [SerializeField] private bool ignoreShipCapacity = true;
    [SerializeField] private bool autoFindFishAssetsInEditor = true;

    [Header("Baits")]
    [SerializeField] private BaitData[] baitPool;
    [SerializeField, Min(1)] private int baitQuantityToAdd = 5;
    [SerializeField] private bool useDefaultBaitsWhenEmpty = true;

    [Header("Keyboard Shortcuts")]
    [SerializeField] private bool enableKeyboardShortcuts = true;
    [SerializeField] private bool requireShiftModifier = true;
    [SerializeField] private Key addRandomFishKey = Key.F6;
    [SerializeField] private Key addRandomFishBatchKey = Key.F7;
    [SerializeField] private Key addBaitsKey = Key.F8;

    private void Awake()
    {
        EnsureValidShortcutKeys();
        ResolveReferences();
    }

    private void OnValidate()
    {
        randomFishAmount = Mathf.Max(1, randomFishAmount);
        EnsureValidShortcutKeys();
    }

    private void Update()
    {
        if (!enableKeyboardShortcuts || Keyboard.current == null)
            return;

        if (requireShiftModifier && !IsShiftPressed())
            return;

        if (WasKeyPressed(addRandomFishKey))
            AddRandomFish();

        if (WasKeyPressed(addRandomFishBatchKey))
            AddRandomFishBatch();

        if (WasKeyPressed(addBaitsKey))
            AddTestBaits();
    }

    [ContextMenu("Cheats/Add Random Fish")]
    public void AddRandomFish()
    {
        AddRandomFishInternal(1);
    }

    [ContextMenu("Cheats/Add Random Fish Batch")]
    public void AddRandomFishBatch()
    {
        AddAllFishTypes();
    }

    [ContextMenu("Cheats/Add All Fish Types")]
    public void AddAllFishTypes()
    {
        ResolveReferences();

        if (shipInventory == null)
        {
            Debug.LogWarning("[InventoryDebugCheats] ShipInventory nao encontrado.", this);
            return;
        }

        FishScriptableObject[] fishTypes = GetValidUniqueFishTypes(GetFishPool());

        if (fishTypes == null || fishTypes.Length == 0)
        {
            Debug.LogWarning("[InventoryDebugCheats] Nenhum peixe configurado para debug.", this);
            return;
        }

        int addedCount = 0;

        for (int i = 0; i < fishTypes.Length; i++)
        {
            FishScriptableObject fishType = fishTypes[i];

            if (fishType == null)
                continue;

            FishData fish = new FishData(fishType);

            if (AddFishToInventory(fish))
            {
                FishCaptureHistory.RegisterCatch(fishType);
                addedCount++;
            }
        }

        Debug.Log($"[InventoryDebugCheats] Adicionou {addedCount} {GetFishCountLabel(addedCount)} diferentes ao inventario.", this);
    }

    [ContextMenu("Cheats/Add Test Baits")]
    public void AddTestBaits()
    {
        ResolveReferences();

        if (baitInventory == null)
        {
            Debug.LogWarning("[InventoryDebugCheats] BaitInventory não encontrado.", this);
            return;
        }

        BaitData[] baits = GetBaitPool();

        if (baits == null || baits.Length == 0)
        {
            Debug.LogWarning("[InventoryDebugCheats] Nenhuma isca configurada para debug.", this);
            return;
        }

        int addedTypes = 0;

        for (int i = 0; i < baits.Length; i++)
        {
            if (baits[i] == null)
                continue;

            baitInventory.AddBait(baits[i], baitQuantityToAdd);
            addedTypes++;
        }

        Debug.Log($"[InventoryDebugCheats] Adicionou {baitQuantityToAdd} unidade(s) de {addedTypes} tipo(s) de isca.", this);
    }

    private void AddRandomFishInternal(int _amount)
    {
        ResolveReferences();

        if (shipInventory == null)
        {
            Debug.LogWarning("[InventoryDebugCheats] ShipInventory não encontrado.", this);
            return;
        }

        FishScriptableObject[] fishTypes = GetFishPool();

        if (fishTypes == null || fishTypes.Length == 0)
        {
            Debug.LogWarning("[InventoryDebugCheats] Nenhum peixe configurado para debug.", this);
            return;
        }

        int amount = Mathf.Max(1, _amount);
        int addedCount = 0;

        for (int i = 0; i < amount; i++)
        {
            FishScriptableObject fishType = GetRandomFishType(fishTypes);

            if (fishType == null)
                continue;

            FishData fish = new FishData(fishType);

            if (AddFishToInventory(fish))
                addedCount++;
        }

        Debug.Log($"[InventoryDebugCheats] Adicionou {addedCount} {GetFishCountLabel(addedCount)} {GetRandomLabel(addedCount)} ao inventário.", this);
    }

    private bool AddFishToInventory(FishData _fish)
    {
        if (_fish == null || shipInventory == null)
            return false;

        if (!ignoreShipCapacity)
            return shipInventory.TryAddFish(_fish);

        List<FishData> fishList = new List<FishData>(shipInventory.OwnedFish)
        {
            _fish
        };

        shipInventory.ReplaceFish(fishList);
        return true;
    }

    private FishScriptableObject GetRandomFishType(FishScriptableObject[] _fishTypes)
    {
        FishScriptableObject[] validFish = GetValidUniqueFishTypes(_fishTypes);

        if (validFish == null || validFish.Length == 0)
            return null;

        return validFish[Random.Range(0, validFish.Length)];
    }

    private FishScriptableObject[] GetValidUniqueFishTypes(FishScriptableObject[] _fishTypes)
    {
        List<FishScriptableObject> validFish = new List<FishScriptableObject>();

        if (_fishTypes == null)
            return validFish.ToArray();

        for (int i = 0; i < _fishTypes.Length; i++)
        {
            FishScriptableObject fish = _fishTypes[i];

            if (fish != null && !validFish.Contains(fish))
                validFish.Add(fish);
        }

        return validFish.ToArray();
    }

    private FishScriptableObject[] GetFishPool()
    {
        if (HasAnyFish(fishPool))
            return fishPool;

#if UNITY_EDITOR
        if (autoFindFishAssetsInEditor)
        {
            fishPool = FindFishAssetsInEditor();

            if (HasAnyFish(fishPool))
                return fishPool;
        }
#endif

        return fishPool;
    }

    private BaitData[] GetBaitPool()
    {
        if (HasAnyBait(baitPool))
            return baitPool;

        return useDefaultBaitsWhenEmpty ? BaitCatalog.GetDefaultBaits() : baitPool;
    }

    private bool HasAnyFish(FishScriptableObject[] _fishTypes)
    {
        if (_fishTypes == null)
            return false;

        for (int i = 0; i < _fishTypes.Length; i++)
        {
            if (_fishTypes[i] != null)
                return true;
        }

        return false;
    }

    private bool HasAnyBait(BaitData[] _baits)
    {
        if (_baits == null)
            return false;

        for (int i = 0; i < _baits.Length; i++)
        {
            if (_baits[i] != null)
                return true;
        }

        return false;
    }

    private void ResolveReferences()
    {
        if (shipInventory == null)
            shipInventory = FindFirstObjectByType<ShipInventory>(FindObjectsInactive.Include);

        if (baitInventory == null)
            baitInventory = BaitInventory.GetOrCreate();
    }

    private bool WasKeyPressed(Key _key)
    {
        if (!IsValidKey(_key) || Keyboard.current == null)
            return false;

        return Keyboard.current[_key].wasPressedThisFrame;
    }

    private bool IsShiftPressed()
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && (keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed);
    }

    private void EnsureValidShortcutKeys()
    {
        if (!IsValidKey(addRandomFishKey))
            addRandomFishKey = Key.F6;

        if (!IsValidKey(addRandomFishBatchKey))
            addRandomFishBatchKey = Key.F7;

        if (!IsValidKey(addBaitsKey))
            addBaitsKey = Key.F8;
    }

    private bool IsValidKey(Key _key)
    {
        return _key != Key.None && System.Enum.IsDefined(typeof(Key), _key);
    }

    private static string GetFishCountLabel(int _count)
    {
        return _count == 1 ? "peixe" : "peixes";
    }

    private static string GetRandomLabel(int _count)
    {
        return _count == 1 ? "aleatório" : "aleatórios";
    }

#if UNITY_EDITOR
    private FishScriptableObject[] FindFishAssetsInEditor()
    {
        string[] guids = AssetDatabase.FindAssets("t:FishScriptableObject");
        List<FishScriptableObject> foundFish = new List<FishScriptableObject>();

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            FishScriptableObject fish = AssetDatabase.LoadAssetAtPath<FishScriptableObject>(path);

            if (fish != null && !foundFish.Contains(fish))
                foundFish.Add(fish);
        }

        return foundFish.ToArray();
    }
#endif
}
