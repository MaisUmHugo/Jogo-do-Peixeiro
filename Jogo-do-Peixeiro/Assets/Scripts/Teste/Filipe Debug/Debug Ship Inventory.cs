using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DebugShipInventory : MonoBehaviour
{    
    [SerializeField] private TMP_Text fishesText;

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

        shipInventory = GetComponent<ShipInventory>();        
        
    }

    private void Start()
    {        

        for (int i = 0; i < fish2Qtt; i++)
        {
            FishData newFish = new FishData(fish2);
            shipInventory.TryAddFish(newFish);


        }
        
        for(int i = 0; i < fish1Qtt; i++)
        {
            FishData newFish = new FishData(fish1);
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
    }

}
