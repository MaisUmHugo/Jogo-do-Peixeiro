using System.Collections;
using UnityEngine;

public class FishingManager : MonoBehaviour
{
    public static FishingManager instance;

    [SerializeField] private float fishingTime = 2f;
    [SerializeField] private FishSkillCheck fishSkillCheck;
    [SerializeField] private bool useSkillCheck = true;

    public bool IsFishing { get; private set; }

    private GameManager.GameState previousState;
    private ShipInventory currentShipInventory;
    private FishScriptableObject[] currentAvailableFish;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    public void StartFishing(ShipInventory _shipInventory, FishScriptableObject[] _availableFish)
    {
        if (IsFishing)
            return;

        if (_shipInventory == null)
        {
            if (GameManager.instance != null)
                GameManager.instance.SetState(GameManager.GameState.OnBoat);
            return;
        }

        if (_availableFish == null || _availableFish.Length == 0)
        {
            Debug.LogWarning("Nenhum peixe configurado nesse spot.");

            if (GameManager.instance != null)
                GameManager.instance.SetState(GameManager.GameState.OnBoat);

            return;
        }

        currentShipInventory = _shipInventory;
        currentAvailableFish = _availableFish;

        if (GameManager.instance != null)
        {
            previousState = GameManager.instance.currentState;
            GameManager.instance.SetState(GameManager.GameState.Fishing);
        }

        if (useSkillCheck && fishSkillCheck != null)
        {
            IsFishing = true;
            fishSkillCheck.StartSkillCheck(this);
            return;
        }

        StartCoroutine(FishingRoutine());
    }

    private IEnumerator FishingRoutine()
    {
        IsFishing = true;

        Debug.Log("Pescando...");

        yield return new WaitForSeconds(fishingTime);

        GiveRandomFish();

        EndFishing();
    }

    public void OnSkillCheckSuccess()
    {
        GiveRandomFish();
        EndFishing();
    }

    public void OnSkillCheckFail()
    {
        Debug.Log("Falhou na pescaria.");
        EndFishing();
    }

    private void GiveRandomFish()
    {
        if (currentShipInventory == null || currentAvailableFish == null || currentAvailableFish.Length == 0)
            return;

        int randomIndex = Random.Range(0, currentAvailableFish.Length);
        FishData fish = new FishData(currentAvailableFish[randomIndex]);

        bool addedSuccessfully = currentShipInventory.TryAddFish(fish);

        if (addedSuccessfully)
            Debug.Log($"Peixe capturado: {fish.typeOfFish.fishName} - {fish.weight}kg");
        else
            Debug.Log("Inventário cheio.");
    }

    private void EndFishing()
    {
        IsFishing = false;

        if (GameManager.instance != null)
            GameManager.instance.SetState(previousState);

        currentShipInventory = null;
        currentAvailableFish = null;
    }
}