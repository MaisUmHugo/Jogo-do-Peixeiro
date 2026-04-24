using System;
using System.Collections;
using UnityEngine;

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
    [SerializeField] private GameObject _spookyFishObject;

    public event Action<FeedbackResult> OnFeedbackTriggered;
    public event Action OnFailShake;

    [Header("Timing")]
    [SerializeField] private float _timingDuration = 1f;

    [Header("Fail Settings")]
    [SerializeField] private int _maxFails = 3;

    [Header("Progress Impact")]
    [SerializeField] private float _successBonus = 0.18f;
    [SerializeField] private float _failPenaltyProgress = 0.12f;

    [Header("Skill Check Frequency")]
    [SerializeField] private float _minSkillCheckInterval = 3f;
    [SerializeField] private float _maxSkillCheckInterval = 6f;

    [Header("Default Difficulty")]
    [SerializeField, Range(0.05f, 0.8f)] private float _defaultSuccessZoneSize = 0.2f;
    [SerializeField] private float _defaultIndicatorSpeed = 1f;

    [Header("Rarity 1")]
    [SerializeField, Range(0.05f, 0.8f)] private float _rarity1SuccessZoneSize = 0.24f;
    [SerializeField] private float _rarity1IndicatorSpeed = 0.9f;

    [Header("Rarity 2")]
    [SerializeField, Range(0.05f, 0.8f)] private float _rarity2SuccessZoneSize = 0.18f;
    [SerializeField] private float _rarity2IndicatorSpeed = 1.15f;

    [Header("Rarity 3")]
    [SerializeField, Range(0.05f, 0.8f)] private float _rarity3SuccessZoneSize = 0.12f;
    [SerializeField] private float _rarity3IndicatorSpeed = 1.4f;

    [Header("Zone Spawn")]
    [SerializeField, Range(0f, 1f)] private float _minZoneStart = 0.1f;
    [SerializeField, Range(0f, 1f)] private float _maxZoneStart = 0.75f;

    [Header("Accuracy Thresholds")]
    [SerializeField, Range(0f, 1f)] private float _perfectThreshold = 0.15f;
    [SerializeField, Range(0f, 1f)] private float _greatThreshold = 0.35f;

    [Header("Zone Variation")]
    [SerializeField] private float _zoneVariationPercent = 0.25f;

    public float SuccessZoneStartNormalized { get; private set; }
    public float SuccessZoneEndNormalized { get; private set; }
    public float IndicatorNormalized { get; private set; }
    public bool IsSkillCheckActive { get; private set; }

    public int CurrentFails => _currentFails;
    public int MaxFails => _maxFails;

    private FishingManager _fishingManager;
    private FishScriptableObject _currentFishType;

    private int _currentFails;
    private float _currentSuccessZoneSize;
    private float _currentIndicatorSpeed;
    private float _nextSkillCheckTimer;
    private bool _isSessionActive;

    public void StartSkillCheck(FishingManager _fishingManagerReference, FishScriptableObject _fishType)
    {
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
        _nextSkillCheckTimer -= Time.deltaTime;

        if (_nextSkillCheckTimer <= 0f)
            ActivateSkillCheck();
    }

    private void ActivateSkillCheck()
    {
        IsSkillCheckActive = true;
        IndicatorNormalized = 0f;
        GenerateNewZone();
    }

    private void UpdateIndicator()
    {
        IndicatorNormalized += (_currentIndicatorSpeed / Mathf.Max(0.01f, _timingDuration)) * Time.deltaTime;

        if (IndicatorNormalized >= 1f)
            RegisterFail();
    }

    private void CheckClick()
    {
        if (!_isSessionActive || !IsSkillCheckActive)
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

        if (normalizedDistance <= _perfectThreshold)
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
        float variation = _currentSuccessZoneSize * _zoneVariationPercent;

        float variedSize = _currentSuccessZoneSize + UnityEngine.Random.Range(-variation, variation);
        variedSize = Mathf.Clamp(variedSize, 0.05f, 0.9f);

        float allowedMaxStart = Mathf.Min(_maxZoneStart, 1f - variedSize);
        float allowedMinStart = Mathf.Clamp(_minZoneStart, 0f, allowedMaxStart);

        SuccessZoneStartNormalized = UnityEngine.Random.Range(allowedMinStart, allowedMaxStart);
        SuccessZoneEndNormalized = SuccessZoneStartNormalized + variedSize;
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