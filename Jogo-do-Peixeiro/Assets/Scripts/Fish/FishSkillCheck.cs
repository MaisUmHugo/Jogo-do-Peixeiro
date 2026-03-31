using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FishSkillCheck : MonoBehaviour
{
    public enum FeedbackResult
    {
        Terrible,
        Bad,
        Near,
        Good,
        Great,
        Perfect
    }
    public GameObject peixeexe;

    public event Action<FeedbackResult> OnFeedbackTriggered;
    public event Action OnFailShake;

    [Header("Timing")]
    [SerializeField] private float timingDuration = 1f;

    [Header("Fail Settings")]
    [SerializeField] private int maxFails = 3;

    [Header("Progress System")]
    [SerializeField] private float passiveProgressSpeed = 0.08f;
    [SerializeField] private float successBonus = 0.18f;
    [SerializeField] private float failPenaltyProgress = 0.12f;

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

    [Header("Accuracy Thresholds")]
    [SerializeField, Range(0f, 1f)] private float perfectThreshold = 0.15f;
    [SerializeField, Range(0f, 1f)] private float greatThreshold = 0.35f;
    [SerializeField, Range(0f, 1f)] private float nearMissThreshold = 1.25f;
    [SerializeField, Range(0f, 1f)] private float badMissThreshold = 2.2f;

    [Header("Zone Variation")]
    [SerializeField] private float zoneVariationPercent = 0.25f;

    public float SuccessZoneStartNormalized { get; private set; }
    public float SuccessZoneEndNormalized { get; private set; }
    public float IndicatorNormalized { get; private set; }
    public float ProgressNormalized { get; private set; }

    public int CurrentFails => currentFails;
    public int MaxFails => maxFails;

    public float CurrentSuccessZoneSize => currentSuccessZoneSize;
    public float CurrentIndicatorSpeed => currentIndicatorSpeed;

    private FishingManager fishingManager;
    private FishScriptableObject currentFishType;

    private int currentFails;
    private float currentSuccessZoneSize;
    private float currentIndicatorSpeed;

    public void StartSkillCheck(FishingManager _fishingManager, FishScriptableObject _fishType)
    {
        fishingManager = _fishingManager;
        currentFishType = _fishType;

        currentFails = 0;
        IndicatorNormalized = 0f;
        ProgressNormalized = 0f;

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
        UpdateProgress();
    }

    private void UpdateIndicator()
    {
        IndicatorNormalized += (currentIndicatorSpeed / Mathf.Max(0.01f, timingDuration)) * Time.deltaTime;

        if (IndicatorNormalized >= 1f)
            RegisterFail();
    }

    private void UpdateProgress()
    {
        ProgressNormalized += passiveProgressSpeed * Time.deltaTime;
        ProgressNormalized = Mathf.Clamp01(ProgressNormalized);

        if (ProgressNormalized >= 1f)
            WinMinigame();
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
        FeedbackResult feedback = CalculateSuccessFeedback();
        OnFeedbackTriggered?.Invoke(feedback);

        float bonus = successBonus;

        switch (feedback)
        {
            case FeedbackResult.Good:
                bonus *= 1f;
                break;

            case FeedbackResult.Great:
                bonus *= 1.25f;
                break;

            case FeedbackResult.Perfect:
                bonus *= 1.6f;
                break;
        }

        ProgressNormalized += bonus;
        ProgressNormalized = Mathf.Clamp01(ProgressNormalized);

        if (ProgressNormalized >= 1f)
        {
            WinMinigame();
            return;
        }

        ResetRound();
    }

    private void RegisterFail()
    {
        currentFails++;

        ProgressNormalized -= failPenaltyProgress;
        ProgressNormalized = Mathf.Clamp01(ProgressNormalized);

        FeedbackResult feedback = CalculateFailFeedback();
        OnFeedbackTriggered?.Invoke(feedback);
        OnFailShake?.Invoke();

        if (currentFails >= maxFails)
        {
            FailMinigame();
            return;
        }

        ResetRound();
    }

    private FeedbackResult CalculateSuccessFeedback()
    {
        float center = (SuccessZoneStartNormalized + SuccessZoneEndNormalized) * 0.5f;
        float halfSize = (SuccessZoneEndNormalized - SuccessZoneStartNormalized) * 0.5f;

        float distanceFromCenter = Mathf.Abs(IndicatorNormalized - center);
        float normalizedDistance = halfSize > 0f ? distanceFromCenter / halfSize : 1f;

        if (normalizedDistance <= perfectThreshold)
            return FeedbackResult.Perfect;

        if (normalizedDistance <= greatThreshold)
            return FeedbackResult.Great;

        return FeedbackResult.Good;
    }

    private FeedbackResult CalculateFailFeedback()
    {
        float center = (SuccessZoneStartNormalized + SuccessZoneEndNormalized) * 0.5f;

        float distance = Mathf.Abs(IndicatorNormalized - center);
        float normalized = distance / 0.5f;

        if (normalized <= 0.25f)
            return FeedbackResult.Near;

        if (normalized <= 0.6f)
            return FeedbackResult.Bad;

        return FeedbackResult.Terrible;
    }

    private void ResetRound()
    {
        IndicatorNormalized = 0f;
        GenerateNewZone();
    }

    private void GenerateNewZone()
    {
        float variation = currentSuccessZoneSize * zoneVariationPercent;

        float variedSize = currentSuccessZoneSize + UnityEngine.Random.Range(-variation, variation);
        variedSize = Mathf.Clamp(variedSize, 0.05f, 0.9f);

        float allowedMaxStart = Mathf.Min(maxZoneStart, 1f - variedSize);
        float allowedMinStart = Mathf.Clamp(minZoneStart, 0f, allowedMaxStart);

        SuccessZoneStartNormalized = UnityEngine.Random.Range(allowedMinStart, allowedMaxStart);
        SuccessZoneEndNormalized = SuccessZoneStartNormalized + variedSize;
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
        if (PlayerPrefs.GetInt("SpookyMode", 0) == 1)
        {
            CoroutineRunner.instance.StartCoroutine(FalhaSpooky());
        }

        enabled = false;
        gameObject.SetActive(false);

        if (fishingManager != null)
            fishingManager.OnSkillCheckFail();
    }

    private void WinMinigame()
    {
        enabled = false;
        gameObject.SetActive(false);

        IndicatorNormalized = 0f;
        ProgressNormalized = 0f;
        currentFails = 0;
        currentFishType = null;

        if (fishingManager != null)
            fishingManager.OnSkillCheckSuccess();
    }
    private IEnumerator FalhaSpooky()
    {
        peixeexe.SetActive(true);
        yield return new WaitForSecondsRealtime(0.5f);
        peixeexe.SetActive(false);
    }
}