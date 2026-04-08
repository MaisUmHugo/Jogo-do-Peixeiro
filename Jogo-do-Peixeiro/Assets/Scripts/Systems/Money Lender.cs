using UnityEngine;
using UnityEngine.VFX;

public class MoneyLender : MonoBehaviour
{
    [Header("Weight Payment")]
    [SerializeField] private int initialFishWeightPaid = 100;
    [SerializeField] private int fishWeightPaidIncremetion = 20;

    [Header("Specific Fish Payment")]
    [SerializeField] private int qttSpecificFish;
    [SerializeField] private FishScriptableObject specificFish;

    [Header("References")]
    [SerializeField] private ShipInventory shipInventory;

    [Header("Firework VFX")]
    [SerializeField] private VisualEffect fireworkVFXPrefab;
    [SerializeField] private Transform fireworkSpawnPoint;
    [SerializeField] private float fireworkVFXLifetime = 3f;

    private int currentFishWeightPayment;
    private int timesPaid = 0;
    private bool tutorialFinishFireworksStarted = false;

    private void Awake()
    {
        CalculateNewPayment();
    }

    public bool TryGetFishWeightPayment()
    {
        if (shipInventory == null)
            return false;

        if (!shipInventory.TryPayFishWeight(currentFishWeightPayment))
        {
            Debug.Log("Não tem peso de peixe suficiente para pagar.");
            return false;
        }

        bool tutorialWasActive = TutorialHandler.Instance != null && !TutorialHandler.Instance.IsTutorialFinished;

        GetFishWeightPayment();

        if (TutorialHandler.Instance != null)
            TutorialHandler.Instance.GoNextObjective();

        bool tutorialFinishedNow = tutorialWasActive &&
                                   TutorialHandler.Instance != null &&
                                   TutorialHandler.Instance.IsTutorialFinished;

        if (tutorialFinishedNow)
            StartTutorialFinishFireworks();
        else
            PlayFireworkVFX();

        Debug.Log("Pagou o peso de peixe.");
        return true;
    }

    private void GetFishWeightPayment()
    {
        timesPaid++;
        CalculateNewPayment();
    }

    private void CalculateNewPayment()
    {
        currentFishWeightPayment = initialFishWeightPaid + fishWeightPaidIncremetion * timesPaid;
    }

    public bool TryGetSpecificFishPayment()
    {
        if (shipInventory == null || specificFish == null)
            return false;

        if (!shipInventory.TryPaySpecificFish(specificFish, qttSpecificFish))
        {
            Debug.Log($"Não tem quantidade suficiente de {specificFish.fishName}.");
            return false;
        }

        GetSpecificFishPayment();
        PlayFireworkVFX();

        Debug.Log($"Pagou {specificFish.fishName}.");
        return true;
    }

    private void GetSpecificFishPayment()
    {
        timesPaid++;
        CalculateNewPayment();
    }

    private void PlayFireworkVFX()
    {
        if (fireworkVFXPrefab == null || fireworkSpawnPoint == null)
            return;

        VisualEffect instance = Instantiate(
            fireworkVFXPrefab,
            fireworkSpawnPoint.position,
            fireworkSpawnPoint.rotation
        );

        instance.gameObject.SetActive(true);
        instance.Reinit();
        instance.Play();

        Destroy(instance.gameObject, fireworkVFXLifetime);
    }

    private void StartTutorialFinishFireworks()
    {
        if (tutorialFinishFireworksStarted)
            return;

        if (fireworkVFXPrefab == null || fireworkSpawnPoint == null)
            return;

        tutorialFinishFireworksStarted = true;

        VisualEffect instance = Instantiate(
            fireworkVFXPrefab,
            fireworkSpawnPoint.position,
            fireworkSpawnPoint.rotation
        );

        instance.gameObject.SetActive(true);
        instance.Reinit();
        instance.Play();
    }

    public int GetCurrentFishWeightPayment()
    {
        return currentFishWeightPayment;
    }

    public float GetCurrentOwnedWeight()
    {
        if (shipInventory == null)
            return 0f;

        return shipInventory.GetCurrentWeight();
    }

    public int GetTimesPaid()
    {
        return timesPaid;
    }

    public FishScriptableObject GetSpecificFish()
    {
        return specificFish;
    }

    public int GetSpecificFishQuantity()
    {
        return qttSpecificFish;
    }
}