using System.Collections;
using UnityEngine;

public class FishingManager : MonoBehaviour
{
    public static FishingManager instance;

    [SerializeField] private FishingResultUI fishingResultUI;
    [SerializeField] private float fishingTime = 10f;
    [SerializeField] private FishSkillCheck fishSkillCheck;
    [SerializeField] private bool useSkillCheck = true;

    public bool IsFishing { get; private set; }

    private ShipInventory currentShipInventory;
    private FishScriptableObject[] currentAvailableFish;

    private FishData pendingFish;

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
            ReturnToBoatState();
            return;
        }

        if (_availableFish == null || _availableFish.Length == 0)
        {
            Debug.LogWarning("Nenhum peixe configurado nesse spot.");
            ReturnToBoatState();
            return;
        }

        currentShipInventory = _shipInventory;
        currentAvailableFish = _availableFish;

        FishScriptableObject selectedFishType = PickRandomFishType();
        if (selectedFishType == null)
        {
            Debug.LogWarning("Falha ao selecionar um peixe.");
            ReturnToBoatState();
            return;
        }

        pendingFish = new FishData(selectedFishType);

        // Agora bloqueia só se já estiver cheio antes de começar a pescaria.
        if (currentShipInventory.IsFull)
        {
            Debug.Log("Inventário do barco cheio.");

            if (HUDWarningUI.Instance != null)
                HUDWarningUI.Instance.ShowWarning("Inventário cheio");

            pendingFish = null;
            ReturnToBoatState();
            return;
        }

        if (GameManager.instance != null)
        {
            GameManager.instance.SetState(GameManager.GameState.Fishing);
        }

        if (useSkillCheck && fishSkillCheck != null)
        {
            IsFishing = true;
            fishSkillCheck.StartSkillCheck(this, selectedFishType);
            return;
        }

        StartCoroutine(FishingRoutine());
    }

    private IEnumerator FishingRoutine()
    {
        IsFishing = true;

        Debug.Log("Pescando...");

        yield return new WaitForSeconds(fishingTime);

        GivePendingFish();
        EndFishing();
    }

    public void OnSkillCheckSuccess()
    {
        GivePendingFish();
        EndFishing();
    }

    public void OnSkillCheckFail()
    {
        Debug.Log("Falhou na pescaria.");
        EndFishing();
    }

    private FishScriptableObject PickRandomFishType()
    {
        if (currentAvailableFish == null || currentAvailableFish.Length == 0)
            return null;

        int randomIndex = Random.Range(0, currentAvailableFish.Length);
        return currentAvailableFish[randomIndex];
    }

    private void GivePendingFish()
    {
        if (currentShipInventory == null || pendingFish == null)
            return;

        bool addedSuccessfully = currentShipInventory.TryAddFish(pendingFish);

        if (addedSuccessfully)
        {
            Debug.Log($"Peixe capturado: {pendingFish.typeOfFish.fishName} - {pendingFish.weight}kg");

            if (HUDFishInfoUI.Instance != null)
            {
                HUDFishInfoUI.Instance.ShowFishInfo(
                    pendingFish.typeOfFish.fishName,
                    pendingFish.weight
                );
            }

            if (fishingResultUI != null)
            {
                fishingResultUI.ShowCatchResult(
                    pendingFish.typeOfFish.fishName,
                    pendingFish.weight
                );
            }
            if (TutorialHandler.Instance != null)
            {
                float currentWeight = currentShipInventory.GetCurrentWeight();
                Debug.Log($"Peso atual após capturar peixe: {currentWeight}");

                if (currentWeight >= 10f)
                {
                    TutorialHandler.Instance.isFinishedFishing = true;
                    TutorialHandler.Instance.GoNextObjective();
                }
                else
                {
                    TutorialHandler.Instance.AttFishWeightTutorialText();
                }
            }
        }
        else
        {
            Debug.Log("Inventário cheio.");

            if (HUDWarningUI.Instance != null)
                HUDWarningUI.Instance.ShowWarning("Sem espaço para mais peixes");
        }

        pendingFish = null;
    }

    private void EndFishing()
    {
        IsFishing = false;

        if (GameManager.instance != null)
            GameManager.instance.SetState(GameManager.GameState.OnBoat);

        currentShipInventory = null;
        currentAvailableFish = null;
        pendingFish = null;
    }

    private void ReturnToBoatState()
    {
        if (GameManager.instance != null)
            GameManager.instance.SetState(GameManager.GameState.OnBoat);
    }
}