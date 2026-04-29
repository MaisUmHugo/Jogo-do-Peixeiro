using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class InvertoryManager : MonoBehaviour
{

    [SerializeField] private ShipInventory shipInventory;
    [SerializeField] private TMP_Text inventoryText;
    [SerializeField] private TMP_Text kilogramText;

    private void Awake()
    {
        shipInventory.OnFishListChange += OnNewFishList;
    }

    private void OnNewFishList(List<FishData> _ownedFishes, float _fishweight)
    {
        inventoryText.text = "";
        foreach (FishData fish in _ownedFishes) {

            inventoryText.text += $"{fish.typeOfFish.fishName}, peso: {fish.weight} \n \n";

        }
        kilogramText.text = $"kilos de peixe: {_fishweight} Kg";

    }

    public void SetInventoryActive(InputAction.CallbackContext input)
    {
        if (input.started)
        {
            gameObject.SetActive(!gameObject.activeSelf);
        }
    }
}
