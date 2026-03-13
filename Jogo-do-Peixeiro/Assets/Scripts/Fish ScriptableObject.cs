using UnityEngine;


[CreateAssetMenu(fileName = "Fish", menuName = "New Fish")]
public class FishScriptableObject : ScriptableObject
{
    
    public float MaxWeight;
    public float MinWeight;

    public int PricePerWeght;   

    public string Name;
    [TextArea] public string Description;

}
