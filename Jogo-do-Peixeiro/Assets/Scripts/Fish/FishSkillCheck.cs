using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

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

    [Header("Spooky")]
    [FormerlySerializedAs("peixeexe")]
    [SerializeField] private GameObject _spookyFishObject;

    [Header("References")]
    [SerializeField] private FishDirectionPull _fishDirectionPull;
    [SerializeField] private bool _delayWhileDirectionPullActive;

    public event Action<FeedbackResult> OnFeedbackTriggered;
    public event Action OnFailShake;

    [Header("Timing")]
    [FormerlySerializedAs("timingDuration")]
    [SerializeField] private float _timingDuration = 1.25f;
    [SerializeField, Range(0.25f, 2f)] private float _indicatorSpeedMultiplier = 0.65f;

    [Header("Fail Settings")]
    [FormerlySerializedAs("maxFails")]
    [SerializeField] private int _maxFails = 3;

    [Header("Progress Impact")]
    [FormerlySerializedAs("successBonus")]
    [SerializeField] private float _successBonus = 0.2f;
    [FormerlySerializedAs("failPenaltyProgress")]
    [SerializeField] private float _failPenaltyProgress = 0.12f;

    [Header("Skill Check Frequency")]
    [FormerlySerializedAs("minSkillCheckInterval")]
    [SerializeField] private float _minSkillCheckInterval = 3f;
    [FormerlySerializedAs("maxSkillCheckInterval")]
    [SerializeField] private float _maxSkillCheckInterval = 6f;

    [Header("Default Difficulty")]
    [FormerlySerializedAs("defaultSuccessZoneSize")]
    [SerializeField, Range(0.05f, 0.8f)] private float _defaultSuccessZoneSize = 0.28f;
    [FormerlySerializedAs("defaultIndicatorSpeed")]
    [SerializeField] private float _defaultIndicatorSpeed = 1f;

    [Header("Rarity 1")]
    [FormerlySerializedAs("rarity1SuccessZoneSize")]
    [SerializeField, Range(0.05f, 0.8f)] private float _rarity1SuccessZoneSize = 0.32f;
    [FormerlySerializedAs("rarity1IndicatorSpeed")]
    [SerializeField] private float _rarity1IndicatorSpeed = 0.9f;

    [Header("Rarity 2")]
    [FormerlySerializedAs("rarity2SuccessZoneSize")]
    [SerializeField, Range(0.05f, 0.8f)] private float _rarity2SuccessZoneSize = 0.26f;
    [FormerlySerializedAs("rarity2IndicatorSpeed")]
    [SerializeField] private float _rarity2IndicatorSpeed = 1.15f;

    [Header("Rarity 3")]
    [FormerlySerializedAs("rarity3SuccessZoneSize")]
    [SerializeField, Range(0.05f, 0.8f)] private float _rarity3SuccessZoneSize = 0.2f;
    [FormerlySerializedAs("rarity3IndicatorSpeed")]
    [SerializeField] private float _rarity3IndicatorSpeed = 1.4f;

    [Header("Zone Spawn")]
    [FormerlySerializedAs("minZoneStart")]
    [SerializeField, Range(0f, 1f)] private float _minZoneStart = 0.1f;
    [FormerlySerializedAs("maxZoneStart")]
    [SerializeField, Range(0f, 1f)] private float _maxZoneStart = 0.75f;

    [Header("Accuracy Thresholds")]
    [SerializeField, Range(0f, 1f)] private float _minPerfectThreshold = 0.08f;
    [FormerlySerializedAs("perfectThreshold")]
    [SerializeField, Range(0f, 1f)] private float _perfectThreshold = 0.15f;
    [FormerlySerializedAs("greatThreshold")]
    [SerializeField, Range(0f, 1f)] private float _greatThreshold = 0.35f;
    [SerializeField, Range(0.25f, 1f)] private float _successZoneSizeMultiplier = 0.45f;
    [SerializeField, Range(0.5f, 1f)] private float _successHitScale = 0.9f;
    [SerializeField, Range(0f, 0.03f)] private float _successInputTolerance = 0.003f;

    [Header("Zone Variation")]
    [FormerlySerializedAs("zoneVariationPercent")]
    [SerializeField] private float _zoneVariationPercent = 0.15f;

    public float SuccessZoneStartNormalized { get; private set; }
    public float SuccessZoneEndNormalized { get; private set; }
    public float IndicatorNormalized { get; private set; }
    public bool IsSkillCheckActive { get; private set; }

    public int CurrentFails => _currentFails;
    public int MaxFails => _maxFails;
    public float MinPerfectThreshold => _minPerfectThreshold;
    public float MaxPerfectThreshold => _perfectThreshold;
    public float CurrentPerfectThreshold => _currentPerfectThreshold;

    private FishingManager _fishingManager;
    private FishScriptableObject _currentFishType;

    private int _currentFails;
    private float _currentSuccessZoneSize;
    private float _currentIndicatorSpeed;
    private float _currentPerfectThreshold;
    private float _nextSkillCheckTimer;
    private bool _isSessionActive;

    private void OnValidate()
    {
        _timingDuration = Mathf.Max(0.25f, _timingDuration);
        _indicatorSpeedMultiplier = Mathf.Max(0.01f, _indicatorSpeedMultiplier);
        _minSkillCheckInterval = Mathf.Max(0f, _minSkillCheckInterval);
        _maxSkillCheckInterval = Mathf.Max(_minSkillCheckInterval, _maxSkillCheckInterval);
        _defaultIndicatorSpeed = Mathf.Max(0.01f, _defaultIndicatorSpeed);
        _rarity1IndicatorSpeed = Mathf.Max(0.01f, _rarity1IndicatorSpeed);
        _rarity2IndicatorSpeed = Mathf.Max(0.01f, _rarity2IndicatorSpeed);
        _rarity3IndicatorSpeed = Mathf.Max(0.01f, _rarity3IndicatorSpeed);
        ClampAccuracySettings();
    }

    public void StartSkillCheck(FishingManager _fishingManagerReference, FishScriptableObject _fishType)
    {
        ClampAccuracySettings();
        AutoAssignMissingReferences();

        _fishingManager = _fishingManagerReference;
        _currentFishType = _fishType;

        _currentFails = 0;
        IndicatorNormalized = 0f;
        IsSkillCheckActive = false;
        _isSessionActive = true;

        ApplyDifficultyFromFish();
        ScheduleNextSkillCheck();

        gameObject.SetActive(true);
        enabled = true;
    }

    public void StopSkillCheck()
    {
        _isSessionActive = false;
        IsSkillCheckActive = false;
        IndicatorNormalized = 0f;
        _currentFails = 0;
        _currentFishType = null;

        enabled = false;
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (InputHandler.instance != null)
            InputHandler.instance.onInteractPressed += CheckClick;
    }

    private void OnDisable()
    {
        if (InputHandler.instance != null)
            InputHandler.instance.onInteractPressed -= CheckClick;
    }

    private void Update()
    {
        if (!_isSessionActive)
            return;

        if (!IsSkillCheckActive)
        {
            UpdateSkillCheckTimer();
            return;
        }

        UpdateIndicator();
    }

    private void UpdateSkillCheckTimer()
    {
        if (ShouldDelayForDirectionPull())
            return;

        _nextSkillCheckTimer -= Time.deltaTime;

        if (_nextSkillCheckTimer <= 0f)
            ActivateSkillCheck();
    }

    private void AutoAssignMissingReferences()
    {
        if (_fishDirectionPull == null)
            _fishDirectionPull = FindFirstObjectByType<FishDirectionPull>(FindObjectsInactive.Include);
    }

    private bool ShouldDelayForDirectionPull()
    {
        return _delayWhileDirectionPullActive &&
               _fishDirectionPull != null &&
               _fishDirectionPull.ShouldBlockSkillCheck;
    }

    private void ActivateSkillCheck()
    {
        IsSkillCheckActive = true;
        IndicatorNormalized = 0f;
        GenerateNewZone();
    }

    private void UpdateIndicator()
    {
        float effectiveIndicatorSpeed = _currentIndicatorSpeed * _indicatorSpeedMultiplier;
        IndicatorNormalized += (effectiveIndicatorSpeed / Mathf.Max(0.25f, _timingDuration)) * Time.deltaTime;

        if (IndicatorNormalized >= 1f)
            RegisterFail();
    }

    private void CheckClick()
    {
        if (!_isSessionActive || !IsSkillCheckActive)
            return;

        float center = (SuccessZoneStartNormalized + SuccessZoneEndNormalized) * 0.5f;
        float halfSize = (SuccessZoneEndNormalized - SuccessZoneStartNormalized) * 0.5f;
        float scaledHalfSize = halfSize * _successHitScale;
        float hitStart = Mathf.Clamp01(center - scaledHalfSize - _successInputTolerance);
        float hitEnd = Mathf.Clamp01(center + scaledHalfSize + _successInputTolerance);

        if (IndicatorNormalized >= hitStart && IndicatorNormalized <= hitEnd)
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

        if (_fishingManager != null)
        {
            _fishingManager.OnSkillCheckSuccessTick(feedback);
            _fishingManager.AddSkillCheckProgressBonus(GetBonusByFeedback(feedback));
        }

        CloseCurrentSkillCheck();
    }

    private void RegisterFail()
    {
        _currentFails++;

        FeedbackResult feedback = CalculateFailFeedback();

        OnFeedbackTriggered?.Invoke(feedback);
        OnFailShake?.Invoke();

        if (_fishingManager != null)
            _fishingManager.ApplySkillCheckPenalty(_failPenaltyProgress);

        if (_currentFails >= _maxFails)
        {
            FailMinigame();
            return;
        }

        CloseCurrentSkillCheck();
    }

    private void CloseCurrentSkillCheck()
    {
        IsSkillCheckActive = false;
        IndicatorNormalized = 0f;
        ScheduleNextSkillCheck();
    }

    private void ScheduleNextSkillCheck()
    {
        _nextSkillCheckTimer = UnityEngine.Random.Range(_minSkillCheckInterval, _maxSkillCheckInterval);
    }

    private float GetBonusByFeedback(FeedbackResult _feedback)
    {
        switch (_feedback)
        {
            case FeedbackResult.Great:
                return _successBonus * 1.25f;

            case FeedbackResult.Perfect:
                return _successBonus * 1.6f;

            default:
                return _successBonus;
        }
    }

    private FeedbackResult CalculateSuccessFeedback()
    {
        float center = (SuccessZoneStartNormalized + SuccessZoneEndNormalized) * 0.5f;
        float halfSize = (SuccessZoneEndNormalized - SuccessZoneStartNormalized) * 0.5f;

        float distanceFromCenter = Mathf.Abs(IndicatorNormalized - center);
        float normalizedDistance = halfSize > 0f ? distanceFromCenter / halfSize : 1f;

        if (normalizedDistance <= _currentPerfectThreshold)
            return FeedbackResult.Perfect;

        if (normalizedDistance <= _greatThreshold)
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

    private void GenerateNewZone()
    {
        float targetZoneSize = _currentSuccessZoneSize * _successZoneSizeMultiplier;
        float variation = targetZoneSize * _zoneVariationPercent;

        float variedSize = targetZoneSize + UnityEngine.Random.Range(-variation, variation);
        variedSize = Mathf.Clamp(variedSize, 0.02f, 0.9f);

        float allowedMaxStart = Mathf.Min(_maxZoneStart, 1f - variedSize);
        float allowedMinStart = Mathf.Clamp(_minZoneStart, 0f, allowedMaxStart);

        SuccessZoneStartNormalized = UnityEngine.Random.Range(allowedMinStart, allowedMaxStart);
        SuccessZoneEndNormalized = SuccessZoneStartNormalized + variedSize;
        _currentPerfectThreshold = UnityEngine.Random.Range(_minPerfectThreshold, _perfectThreshold);
    }

    private void ClampAccuracySettings()
    {
        _minPerfectThreshold = Mathf.Clamp01(_minPerfectThreshold);
        _perfectThreshold = Mathf.Clamp01(_perfectThreshold);
        _greatThreshold = Mathf.Clamp01(_greatThreshold);
        _successZoneSizeMultiplier = Mathf.Clamp(_successZoneSizeMultiplier, 0.25f, 1f);
        _successHitScale = Mathf.Clamp(_successHitScale, 0.5f, 1f);
        _successInputTolerance = Mathf.Clamp(_successInputTolerance, 0f, 0.03f);

        if (_perfectThreshold < _minPerfectThreshold)
            _perfectThreshold = _minPerfectThreshold;

        if (_greatThreshold < _perfectThreshold)
            _greatThreshold = _perfectThreshold;
    }

    private void ApplyDifficultyFromFish()
    {
        int rarity = _currentFishType != null ? _currentFishType.rarity : 1;

        switch (rarity)
        {
            case 1:
                _currentSuccessZoneSize = _rarity1SuccessZoneSize;
                _currentIndicatorSpeed = _rarity1IndicatorSpeed;
                break;

            case 2:
                _currentSuccessZoneSize = _rarity2SuccessZoneSize;
                _currentIndicatorSpeed = _rarity2IndicatorSpeed;
                break;

            case 3:
                _currentSuccessZoneSize = _rarity3SuccessZoneSize;
                _currentIndicatorSpeed = _rarity3IndicatorSpeed;
                break;

            default:
                _currentSuccessZoneSize = _defaultSuccessZoneSize;
                _currentIndicatorSpeed = _defaultIndicatorSpeed;
                break;
        }
    }

    private void FailMinigame()
    {
        if (PlayerPrefs.GetInt("SpookyMode", 0) == 1 && _spookyFishObject != null)
            CoroutineRunner.instance.StartCoroutine(FailSpookyRoutine());

        StopSkillCheck();

        if (_fishingManager != null)
            _fishingManager.OnSkillCheckFail();
    }

    private IEnumerator FailSpookyRoutine()
    {
        _spookyFishObject.SetActive(true);
        yield return new WaitForSecondsRealtime(0.5f);
        _spookyFishObject.SetActive(false);
    }
}
