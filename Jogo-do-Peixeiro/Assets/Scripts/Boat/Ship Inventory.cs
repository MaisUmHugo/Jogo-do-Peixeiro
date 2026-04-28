using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class ShipInventory : MonoBehaviour
{
    private List<FishData> ownedFish = new List<FishData>();

    public delegate void OnFishListChangeDelegate(List<FishData> fishList, float fishWeight);
    public event OnFishListChangeDelegate OnFishListChange;

    [SerializeField] private float maxFishCapacity;

    private float currentFishWeight = 0f;
    private bool wasFullLastUpdate = false;

    public List<FishData> OwnedFish => ownedFish;
    public bool IsFull => currentFishWeight >= maxFishCapacity;    

    [SerializeField] private PlayerMoneyManager playerMoneyManager;


    private int[] intsTest = new int[10] {10,8,6,8,1,2,6,7,3,5};

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
        Debug.Log("adicionou peixe: " + _fish.typeOfFish.name + "de preço: " + _fish.price);
        ownedFish.Add(_fish);       
        MergeSort(ownedFish.ToArray());        
        AttFishWeight();

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
        
        float money = 0;
        foreach (FishData _fish in ownedFish)
        {
            money += _fish.price;
        }

        ownedFish.Clear();
        
        playerMoneyManager.ReciveMoney(money);
    }

    private void SellHalfPriceFish(int _fishIndex)
    {
        float money = 0;

        for (int i = 0; i < _fishIndex; i++)
        {

            money += ownedFish[i].price;

        }

        ownedFish.RemoveRange(0, _fishIndex + 1);
        money = money / 2;
        playerMoneyManager.ReciveMoney(money);

    }

    private void AttFishWeight()
    {
        currentFishWeight = 0f;

        foreach (FishData _fish in ownedFish)
        {
            currentFishWeight += _fish.weight;
        }        

        bool isFullNow = IsFull;

        if (isFullNow && !wasFullLastUpdate)
        {
            if (HUDWarningUI.Instance != null)
                HUDWarningUI.Instance.ShowWarning("Inventário cheio");
        }

        wasFullLastUpdate = isFullNow;
        OnFishListChange?.Invoke(ownedFish,currentFishWeight);
    }

    public float GetCurrentWeight()
    {
        return currentFishWeight;
    }

    public float GetMaxCapacity()
    {
        return maxFishCapacity;
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

    private void MergeSort(FishData[] _fishArray)
    {
        if (_fishArray.Length <= 1) return;        

        int mid = _fishArray.Length / 2;
        FishData[] leftArray = new FishData[mid];
        FishData[] rightArray = new FishData[_fishArray.Length - mid];

        int j = 0;
        int i = 0;

        for (;i < _fishArray.Length; i++)
        {
            if (i < mid)
            {
                leftArray[i] = _fishArray[i];
            }else
            {
                rightArray[j] = _fishArray[i];
                j++;
            }

        }

        MergeSort(leftArray);
        MergeSort(rightArray);
        Merge(leftArray, rightArray, _fishArray);

        ownedFish = _fishArray.ToList();
    }

    private void Merge(FishData[] _leftArray, FishData[] _rightArray, FishData[] _result)
    {
        int leftLenght = _result.Length / 2;
        int rightLenght = _result.Length - leftLenght;
        int l = 0; int i = 0; int r = 0;

        while (l < leftLenght && r < rightLenght)
        {

            if(_leftArray[l].price < _rightArray[r].price)
            {
                _result[i] = _leftArray[l];
                i++;
                l++;

            }
            else
            {
                _result[i] = _rightArray[r];
                i++; 
                r++;

            }
        }

        while (l < leftLenght)
        {
            _result[i] = _leftArray[l];
            i++;
            l++;

        }

        while (r < rightLenght)
        {
            _result[i] = _rightArray[r];
            i++;
            r++;

        }
    }

    
}