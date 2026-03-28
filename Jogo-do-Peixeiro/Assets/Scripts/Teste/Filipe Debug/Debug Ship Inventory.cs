using System.Threading.Tasks;
using TMPro;
using UnityEngine;

public class DebugShipInventory : MonoBehaviour
{
    private TMP_Text shipInventoryText;
    private TMP_Text fishesText;

    private ShipInventory shipInventory;

    public FishScriptableObject fish1;
    public FishScriptableObject fish2;
    public FishScriptableObject fish3;
    public FishScriptableObject fish4;

    public int fish1Qtt;
    public int fish2Qtt;
    public int fish3Qtt;
    public int fish4Qtt;

    private void Awake()
    {
        shipInventoryText = transform.GetChild(0).transform.GetChild(1).GetComponent<TMP_Text>();
        fishesText        = transform.GetChild(0).transform.GetChild(2).GetComponent<TMP_Text>();
        shipInventory = GetComponent<ShipInventory>();

        for(int i = 0; i < fish1Qtt; i++)
        {
            FishData newFish = new FishData(fish1);
            shipInventory.TryAddFish(newFish);
      

        }

        for (int i = 0; i < fish2Qtt; i++)
        {
            FishData newFish = new FishData(fish2);
            shipInventory.TryAddFish(newFish);


        }

        for (int i = 0; i < fish3Qtt; i++)
        {
            FishData newFish = new FishData(fish3);
            shipInventory.TryAddFish(newFish);
 

        }

        for (int i = 0; i < fish4Qtt; i++)
        {
            FishData newFish = new FishData(fish4);
            shipInventory.TryAddFish(newFish);


        }

        AttFishDebugText();
    }

    public void AttFishDebugText()
    {
        fishesText.text = "";
        foreach (FishData fish in shipInventory.ownedFish)
        {
            fishesText.text += $"{fish.typeOfFish.fishName}, weight: {fish.weight} \n \n";
        }

    }

    private void Update()
    {
        shipInventoryText.text = $"Peso de peixe total: {shipInventory.GetCurrentWeight()} / {shipInventory.GetMaxCapacity()}";

        
    }

}
