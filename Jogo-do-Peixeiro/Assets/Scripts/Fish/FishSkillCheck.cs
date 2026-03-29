using UnityEngine;

public class FishSkillCheck : MonoBehaviour
{
    [Header("Timing")]
    [SerializeField] private float timingDuration = 1f;

    [Header("Fishing Progress")]
    [SerializeField] private int requiredSuccesses = 3;
    [SerializeField] private int maxFails = 3;
    [SerializeField] private int failPenalty = 1;

    [Header("Default Difficulty")]
    [SerializeField, Range(0.05f, 0.8f)] private float defaultSuccessZoneSize = 0.2f;
    [SerializeField] private float defaultIndicatorSpeed = 1f;

    [Header("Rarity 1")]
    [SerializeField, Range(0.05f, 0.8f)] private float rarity1SuccessZoneSize = 0.24f;
    [SerializeField] private float rarity1IndicatorSpeed = 0.9f;

    [Header("Rarity 2")]
    [SerializeField, Range(0.05f, 0.8f)] private float rarity2SuccessZoneSize = 0.18f;
    [SerializeField] private float rarity2IndicatorSpeed = 1.15f;

    [Header("Rarity 3")]
    [SerializeField, Range(0.05f, 0.8f)] private float rarity3SuccessZoneSize = 0.12f;
    [SerializeField] private float rarity3IndicatorSpeed = 1.4f;

    [Header("Zone Spawn")]
    [SerializeField, Range(0f, 1f)] private float minZoneStart = 0.1f;
    [SerializeField, Range(0f, 1f)] private float maxZoneStart = 0.75f;

    public float SuccessZoneStartNormalized { get; private set; }
    public float SuccessZoneEndNormalized { get; private set; }
    public float IndicatorNormalized { get; private set; }

    public int CurrentProgress => currentProgress;
    public int RequiredSuccesses => requiredSuccesses;
    public int CurrentFails => currentFails;
    public int MaxFails => maxFails;

    public float CurrentSuccessZoneSize => currentSuccessZoneSize;
    public float CurrentIndicatorSpeed => currentIndicatorSpeed;

    private FishingManager fishingManager;
    private FishScriptableObject currentFishType;

    private int currentProgress;
    private int currentFails;

    private float currentSuccessZoneSize;
    private float currentIndicatorSpeed;

    public void StartSkillCheck(FishingManager _fishingManager, FishScriptableObject _fishType)
    {
        fishingManager = _fishingManager;
        currentFishType = _fishType;

        currentProgress = 0;
        currentFails = 0;
        IndicatorNormalized = 0f;

        ApplyDifficultyFromFish();
        GenerateNewZone();

        gameObject.SetActive(true);
        enabled = true;
    }

    private void Start()
    {
        if (InputHandler.instance != null)
            InputHandler.instance.onInteractPressed += CheckClick;
    }

    private void OnDestroy()
    {
        if (InputHandler.instance != null)
            InputHandler.instance.onInteractPressed -= CheckClick;
    }

    private void Update()
    {
        UpdateIndicator();
    }

    private void UpdateIndicator()
    {
        IndicatorNormalized += (currentIndicatorSpeed / Mathf.Max(0.01f, timingDuration)) * Time.deltaTime;

        if (IndicatorNormalized >= 1f)
            RegisterFail();
    }

    private void CheckClick()
    {
        if (!enabled)
            return;

        if (IndicatorNormalized >= SuccessZoneStartNormalized &&
            IndicatorNormalized <= SuccessZoneEndNormalized)
        {
            RegisterSuccess();
            return;
        }

        RegisterFail();
    }

    private void RegisterSuccess()
    {
        currentProgress++;

        if (currentProgress >= requiredSuccesses)
        {
            WinMinigame();
            return;
        }

        ResetRound();
    }

    private void RegisterFail()
    {
        currentFails++;
        currentProgress = Mathf.Max(0, currentProgress - failPenalty);

        if (currentFails >= maxFails)
        {
            FailMinigame();
            return;
        }

        ResetRound();
    }

    private void ResetRound()
    {
        IndicatorNormalized = 0f;
        GenerateNewZone();
    }

    private void GenerateNewZone()
    {
        float clampedZoneSize = Mathf.Clamp(currentSuccessZoneSize, 0.01f, 0.95f);

        float allowedMaxStart = Mathf.Min(maxZoneStart, 1f - clampedZoneSize);
        float allowedMinStart = Mathf.Clamp(minZoneStart, 0f, allowedMaxStart);

        SuccessZoneStartNormalized = Random.Range(allowedMinStart, allowedMaxStart);
        SuccessZoneEndNormalized = SuccessZoneStartNormalized + clampedZoneSize;
    }

    private void ApplyDifficultyFromFish()
    {
        int rarity = currentFishType != null ? currentFishType.rarity : 1;

        switch (rarity)
        {
            case 1:
                currentSuccessZoneSize = rarity1SuccessZoneSize;
                currentIndicatorSpeed = rarity1IndicatorSpeed;
                break;

            case 2:
                currentSuccessZoneSize = rarity2SuccessZoneSize;
                currentIndicatorSpeed = rarity2IndicatorSpeed;
                break;

            case 3:
                currentSuccessZoneSize = rarity3SuccessZoneSize;
                currentIndicatorSpeed = rarity3IndicatorSpeed;
                break;

            default:
                currentSuccessZoneSize = defaultSuccessZoneSize;
                currentIndicatorSpeed = defaultIndicatorSpeed;
                break;
        }
    }

    private void FailMinigame()
    {
        enabled = false;
        gameObject.SetActive(false);

        IndicatorNormalized = 0f;
        currentProgress = 0;
        currentFails = 0;
        currentFishType = null;

        if (fishingManager != null)
            fishingManager.OnSkillCheckFail();
    }

    private void WinMinigame()
    {
        enabled = false;
        gameObject.SetActive(false);

        IndicatorNormalized = 0f;
        currentProgress = 0;
        currentFails = 0;
        currentFishType = null;

        if (fishingManager != null)
            fishingManager.OnSkillCheckSuccess();
    }
}