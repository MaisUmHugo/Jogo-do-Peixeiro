using UnityEngine;

[CreateAssetMenu(fileName = "Fish", menuName = "New Fish")]
public class FishScriptableObject : ScriptableObject
{
    public int minWeight;
    public int maxWeight;
    
    public Mesh mesh;
    public Material material;

    [Range(1, 4)] public int rarity;

    [Min(0)]
    public int pricePerWeight;
    public int BasePrice => pricePerWeight;

    public string fishName;
    [TextArea] public string description;
}
