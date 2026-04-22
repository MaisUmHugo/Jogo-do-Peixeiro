using UnityEngine;

[CreateAssetMenu(fileName = "Fish", menuName = "New Fish")]
public class FishScriptableObject : ScriptableObject
{
    public int minWeight;
    public int maxWeight;
    
    public Mesh mesh;
    public Material material;

    [Range(1, 3)] public int rarity;

    public int pricePerWeight;

    public string fishName;
    [TextArea] public string description;
}