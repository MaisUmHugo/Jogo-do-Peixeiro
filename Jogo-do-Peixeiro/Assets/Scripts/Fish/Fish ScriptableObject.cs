using UnityEngine;


[CreateAssetMenu(fileName = "Fish", menuName = "New Fish")]
public class FishScriptableObject : ScriptableObject
{
    
    public int MinWeight;
    public int MaxWeight;
    [Range(1,3)] public int Rarity;

    public int PricePerWeight;   

    public string Name;
    [TextArea] public string Description;

}
