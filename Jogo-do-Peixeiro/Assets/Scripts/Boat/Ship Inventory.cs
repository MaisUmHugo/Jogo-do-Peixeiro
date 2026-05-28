using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ShipInventory : MonoBehaviour
{
    public List<FishData> ownedFish = new List<FishData>();
    public List<FishData> OwnedFish => ownedFish;

    [SerializeField] private float maxFishCapacity;

    private float currentFishWeight = 0f;
    private bool wasFullLastUpdate = false;

    public bool IsFull => maxFishCapacity > 0f && currentFishWeight >= maxFishCapacity;

    public DebugShipInventory debugShipInventory;

    [SerializeField] private PlayerMoneyManager playerMoneyManager;

    public delegate void OnFishListChangeDelegate(List<FishData> fishList, float fishWeight);
    public event OnFishListChangeDelegate OnFishListChange;

    private void Awake()
    {
        if (playerMoneyManager == null)
            playerMoneyManager = FindFirstObjectByType<PlayerMoneyManager>();

        AttFishWeight();
    }

    public bool TryAddFish(FishData fish)
    {
        if (fish == null)
            return false;

        // Só bloqueia se já estiver cheio antes da adição.
        // Assim ainda permite passar do limite na última captura.
        if (IsFull)
        {
            Debug.Log($"Inventário cheio. Peso atual: {currentFishWeight} / {maxFishCapacity}");
            return false;
        }

        AddFish(fish);
        return true;
    }

    // Pode continuar existindo para usos futuros, mas não será usado
    // para bloquear a pescaria antes da última captura.
    public bool CanAddFish(FishData _fish)
    {
        if (_fish == null)
            return false;

        return currentFishWeight + _fish.weight <= maxFishCapacity;
    }

    private void AddFish(FishData _fish)
    {
        ownedFish.Add(_fish);
        ownedFish = MergeSort(ownedFish.ToArray()).ToList();
        Debug.Log("Added fish: " + _fish.typeOfFish.name + " with price: " + _fish.price);
        AttFishWeight(true);
    }

    public bool TryRemoveFish(FishData fish)
    {
        if (fish == null || !ownedFish.Remove(fish))
            return false;

        AttFishWeight();
        return true;
    }

    public bool TryRemoveFishAt(int index, out FishData fish)
    {
        fish = null;

        if (index < 0 || index >= ownedFish.Count)
            return false;

        fish = ownedFish[index];
        ownedFish.RemoveAt(index);
        AttFishWeight();
        return true;
    }

    public void ClearFish()
    {
        if (ownedFish.Count == 0)
            return;

        ownedFish.Clear();
        AttFishWeight();
    }

    public int RemoveFishUntilWeightLost(float _targetWeightLoss, out float _removedWeight)
    {
        _removedWeight = 0f;

        if (_targetWeightLoss <= 0f || ownedFish.Count == 0)
            return 0;

        int removedCount = 0;

        while (ownedFish.Count > 0 && _removedWeight < _targetWeightLoss)
        {
            FishData fish = ownedFish[0];
            _removedWeight += fish != null ? fish.weight : 0f;
            ownedFish.RemoveAt(0);
            removedCount++;
        }

        if (removedCount > 0)
            AttFishWeight();

        return removedCount;
    }

    public int RemoveFishByRarityPriority(int _targetCount, int[] _rarityPriority, out float _removedWeight)
    {
        _removedWeight = 0f;

        if (_targetCount <= 0 || ownedFish.Count == 0)
            return 0;

        int removedCount = 0;
        HashSet<int> handledRarities = new HashSet<int>();

        if (_rarityPriority != null)
        {
            foreach (int rarity in _rarityPriority)
            {
                if (TryRemoveByRarity(rarity, ref removedCount, _targetCount, ref _removedWeight))
                    handledRarities.Add(rarity);

                if (removedCount >= _targetCount)
                    break;
            }
        }

        while (removedCount < _targetCount && ownedFish.Count > 0)
        {
            int bestIndex = GetHighestRarityFishIndex(handledRarities);

            if (bestIndex < 0)
                break;

            RemoveFishAtForLoss(bestIndex, ref removedCount, ref _removedWeight);
        }

        if (removedCount > 0)
            AttFishWeight();

        return removedCount;
    }

    private bool TryRemoveByRarity(int _rarity, ref int _removedCount, int _targetCount, ref float _removedWeight)
    {
        bool removedAny = false;

        while (_removedCount < _targetCount)
        {
            int fishIndex = ownedFish.FindIndex(fish => fish != null && fish.typeOfFish != null && fish.typeOfFish.rarity == _rarity);

            if (fishIndex < 0)
                break;

            RemoveFishAtForLoss(fishIndex, ref _removedCount, ref _removedWeight);
            removedAny = true;
        }

        return removedAny;
    }

    private int GetHighestRarityFishIndex(HashSet<int> _ignoredRarities)
    {
        int bestIndex = -1;
        int bestRarity = int.MinValue;

        for (int i = 0; i < ownedFish.Count; i++)
        {
            FishData fish = ownedFish[i];

            if (fish == null || fish.typeOfFish == null)
                continue;

            int rarity = fish.typeOfFish.rarity;

            if (_ignoredRarities != null && _ignoredRarities.Contains(rarity))
                continue;

            if (rarity > bestRarity)
            {
                bestRarity = rarity;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    private void RemoveFishAtForLoss(int _fishIndex, ref int _removedCount, ref float _removedWeight)
    {
        FishData fish = ownedFish[_fishIndex];
        _removedWeight += fish != null ? fish.weight : 0f;
        ownedFish.RemoveAt(_fishIndex);
        _removedCount++;
    }

    public void ReplaceFish(IEnumerable<FishData> _fishList)
    {
        ownedFish.Clear();

        if (_fishList != null)
        {
            foreach (FishData fish in _fishList)
            {
                if (fish != null && fish.typeOfFish != null)
                    ownedFish.Add(fish);
            }
        }

        ownedFish = MergeSort(ownedFish.ToArray()).ToList();
        AttFishWeight();
    }

    public int GetTotalFishValue()
    {
        int totalValue = 0;

        foreach (FishData fish in ownedFish)
        {
            totalValue += FishPriceCalculator.CalculatePrice(fish);
        }

        return totalValue;
    }

    public bool TryPayFishWeight(int _weightFishPayment)
    {
        int fishWeight = 0;
        int fishIndex = -1;

        Debug.Log("tentou pagar peixe");

        foreach (FishData fish in ownedFish)
        {
            fishWeight += fish.weight;

            if (fishWeight >= _weightFishPayment)
            {
                fishIndex = ownedFish.IndexOf(fish);
                break;
            }
        }

        if (fishIndex != -1)
        {
            Debug.Log("conseguiu pagar o peixe");
            SellHalfPriceFish(fishIndex);
            SellRemainingFish();
            AttFishWeight();
            return true;
        }

        return false;
    }

    private void SellRemainingFish()
    {
        Debug.Log("tentando receber peixe");
        float money = 0;
        foreach (FishData _fish in ownedFish)
        {
            money += FishPriceCalculator.CalculatePrice(_fish);
        }

        ownedFish.Clear();
        Debug.Log($"moedas a receber: {money}");
        playerMoneyManager?.ReceiveMoney(money);
    }

    private void SellHalfPriceFish(int _fishIndex)
    {
        float money = 0;

        for (int i = 0; i < _fishIndex; i++)
        {

            money += FishPriceCalculator.CalculatePrice(ownedFish[i]);

        }

        ownedFish.RemoveRange(0, _fishIndex + 1);
        money = money / 2;
        playerMoneyManager?.ReceiveMoney(money);

    }

    private void AttFishWeight(bool _showFullWarning = false)
    {
        bool wasFullBeforeRecalculate = wasFullLastUpdate;
        currentFishWeight = 0f;

        foreach (FishData _fish in ownedFish)
        {
            if (_fish == null)
                continue;

            currentFishWeight += _fish.weight;
        }

        bool isFullNow = IsFull;

        if (_showFullWarning && isFullNow && !wasFullBeforeRecalculate)
        {
            if (HUDWarningUI.Instance != null)
                HUDWarningUI.Instance.ShowWarning("Inventário cheio");
        }

        wasFullLastUpdate = isFullNow;
        OnFishListChange?.Invoke(ownedFish, currentFishWeight);
    }

    public float GetCurrentWeight()
    {
        return currentFishWeight;
    }

    public float GetMaxCapacity()
    {
        return maxFishCapacity;
    }

    public void SetMaxCapacity(float _capacity)
    {
        maxFishCapacity = Mathf.Max(0f, _capacity);
        AttFishWeight();
    }

    public void AddMaxCapacity(float _amount)
    {
        if (_amount <= 0f)
            return;

        SetMaxCapacity(maxFishCapacity + _amount);
    }

    public int CountFish(FishScriptableObject _wantedFish)
    {
        if (_wantedFish == null)
            return 0;

        int currentQtt = 0;

        foreach (FishData fish in ownedFish)
        {
            if (fish.typeOfFish == _wantedFish)
                currentQtt++;
        }

        return currentQtt;
    }

    private bool TryFindFish(FishScriptableObject _wantedFish, int _wantedQtt = 1)
    {
        int currentQtt = 0;

        foreach (FishData fish in ownedFish)
        {
            if (fish.typeOfFish == _wantedFish)
            {
                currentQtt++;

                if (currentQtt == _wantedQtt)
                    return true;
            }
        }

        return false;
    }

    public bool TryPaySpecificFish(FishScriptableObject _wantedFish, int _wantedQtt)
    {
        if (TryFindFish(_wantedFish, _wantedQtt))
        {
            for (int i = 0; i < _wantedQtt; i++)
            {
                ownedFish.RemoveAt(ownedFish.FindIndex(i => i.typeOfFish == _wantedFish));
            }

            AttFishWeight();
            return true;
        }

        return false;
    }

    public bool TryPayTutorialRequest(FishScriptableObject _wantedFish, int _wantedQtt, int _requiredTotalWeight)
    {
        if (_wantedFish == null || _wantedQtt <= 0)
            return false;

        if (CountFish(_wantedFish) < _wantedQtt)
            return false;

        if (currentFishWeight < _requiredTotalWeight)
            return false;

        for (int i = 0; i < _wantedQtt; i++)
        {
            int fishIndex = ownedFish.FindIndex(fish => fish.typeOfFish == _wantedFish);

            if (fishIndex < 0)
                return false;

            ownedFish.RemoveAt(fishIndex);
        }

        AttFishWeight();
        return true;
    }

    private FishData[] MergeSort(FishData[] _fishArray)
    {
        int length = _fishArray.Length;

        if (length <= 1)
            return _fishArray;

        int middle = length / 2;
        FishData[] leftArray = new FishData[middle];
        FishData[] rightArray = new FishData[length - middle];

        for (int i = 0; i < middle; i++)
            leftArray[i] = _fishArray[i];

        for (int i = middle; i < length; i++)
            rightArray[i - middle] = _fishArray[i];

        leftArray = MergeSort(leftArray);
        rightArray = MergeSort(rightArray);

        return Merge(leftArray, rightArray);
    }

    private FishData[] Merge(FishData[] _leftArray, FishData[] _rightArray)
    {
        int leftLength = _leftArray.Length;
        int rightLength = _rightArray.Length;
        int leftIndex = 0;
        int rightIndex = 0;
        int resultIndex = 0;

        FishData[] result = new FishData[leftLength + rightLength];

        while (leftIndex < leftLength && rightIndex < rightLength)
        {
            if (_leftArray[leftIndex].price <= _rightArray[rightIndex].price)
            {
                result[resultIndex] = _leftArray[leftIndex];
                leftIndex++;
            }
            else
            {
                result[resultIndex] = _rightArray[rightIndex];
                rightIndex++;
            }

            resultIndex++;
        }

        while (leftIndex < leftLength)
        {
            result[resultIndex] = _leftArray[leftIndex];
            leftIndex++;
            resultIndex++;
        }

        while (rightIndex < rightLength)
        {
            result[resultIndex] = _rightArray[rightIndex];
            rightIndex++;
            resultIndex++;
        }

        return result;
    }
}
