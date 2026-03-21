using UnityEngine;


[CreateAssetMenu(fileName = "Fish", menuName = "New Fish")]
public class FishScriptableObject : ScriptableObject
{
    
    public int minWeight;
    public int maxWeight;
    [Range(1,3)] public int rarity;

    public int pricePerWeight;   

    public string name;
    [TextArea] public string description;

}
