using System;
using UnityEngine;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

public class FishDirectionPull : MonoBehaviour
{
    public enum FishForceDirection
    {
        Left,
        Right,
        Up,
        Down
    }

    public event Action PullStarted;
    public event Action PullStopped;
    public event Action PromptChanged;
    public event Action PromptStateChanged;
    public event Action<bool> PullActiveChanged;
    public event Action<float> ActiveDirectionTimerChanged;

    [Header("Settings")]
    [SerializeField] private bool _useDirectionalPull = true;
    [SerializeField, Range(0f, 1f)] private float _startDisplayProgress;
    [SerializeField, Range(0f, 1f)] private float _startInfluenceProgress;
    [SerializeField, Range(0.01f, 1f)] private float _fadeInRange = 0.25f;
    [SerializeField] private float _directionChangeInterval = 1.8f;
    [SerializeField] private float _inputThreshold = 0.35f;

    [Header("Progress Modifier")]
    [SerializeField] private float _correctPullProgressSpeed = 0.18f;
    [SerializeField] private float _wrongPullProgressPenalty = 0.16f;
    [SerializeField] private float _noInputProgressPenalty = 0.08f;
    [SerializeField] private float _rarity1NoInputPenaltyMultiplier = 1f;
    [SerializeField] private float _rarity2NoInputPenaltyMultiplier = 1.35f;
    [SerializeField] private float _rarity3NoInputPenaltyMultiplier = 1.8f;
    [FormerlySerializedAs("_minimumCompletionIntensity")]
    [SerializeField, Range(0f, 1f)] private float _minimumPullIntensity = 0.35f;

    [Header("Fish Movement")]
    [SerializeField] private bool _allowDownPrompts;

    [Header("Difficulty Multipliers")]
    [FormerlySerializedAs("_rarity1CompletionBuildMultiplier")]
    [SerializeField] private float _rarity1PullProgressMultiplier = 1f;
    [FormerlySerializedAs("_rarity2CompletionBuildMultiplier")]
    [SerializeField] private float _rarity2PullProgressMultiplier = 0.85f;
    [FormerlySerializedAs("_rarity3CompletionBuildMultiplier")]
    [SerializeField] private float _rarity3PullProgressMultiplier = 0.7f;
    [SerializeField] private float _rarity1DirectionIntervalMultiplier = 1f;
    [SerializeField] private float _rarity2DirectionIntervalMultiplier = 0.85f;
    [SerializeField] private float _rarity3DirectionIntervalMultiplier = 0.7f;

    public FishForceDirection CurrentFishDirection { get; private set; }
    public FishForceDirection RequiredPullDirection => GetOppositeDirection(CurrentFishDirection);
    public bool UseDirectionalPull => _useDirectionalPull;
    public bool IsPullActive { get; private set; }
    public bool HasPullInput { get; private set; }
    public bool IsCorrectPullInput { get; private set; }
    public float PullInputNormalized { get; private set; }
    public float CurrentIntensity { get; private set; }
    public float ActiveDirectionTimeNormalized { get; private set; } = 1f;
    public int CurrentPromptId { get; private set; }
    public Vector2 RequiredPullVector { get; private set; }
    public Vector2 ActiveRequiredPullVector => GetDirectionVector(ActiveRequiredPullDirection);
    public FishForceDirection ActiveRequiredPullDirection => _currentRequiredDirection;
    public Vector2 FishMovementVector => GetDirectionVector(CurrentFishDirection);
    public Vector2 CurrentPullInput { get; private set; }
    public bool ShouldBlockSkillCheck => UseDirectionalPull && IsPullActive && CurrentIntensity > 0f;
    public bool ShouldDecayProgressWithoutPull => UseDirectionalPull && IsPullActive && !HasPullInput;

    private FishForceDirection _currentRequiredDirection;
    private float _directionTimer;
    private float _pullProgressUpgradeMultiplier = 1f;
    private float _directionIntervalUpgradeMultiplier = 1f;
    private float _currentPullProgressMultiplier = 1f;
    private float _currentDirectionIntervalMultiplier = 1f;
    private float _currentNoInputPenaltyMultiplier = 1f;
    private BaitData _currentBait;
    private bool _isPullRunning;
    private float _lastNotifiedTimer = -1f;

    private void OnValidate()
    {
        _startDisplayProgress = Mathf.Clamp01(_startDisplayProgress);
        _startInfluenceProgress = Mathf.Clamp01(_startInfluenceProgress);
        _fadeInRange = Mathf.Max(0.01f, _fadeInRange);
        _directionChangeInterval = Mathf.Max(0.1f, _directionChangeInterval);
        _inputThreshold = Mathf.Max(0.01f, _inputThreshold);
        _correctPullProgressSpeed = Mathf.Max(0f, _correctPullProgressSpeed);
        _wrongPullProgressPenalty = Mathf.Max(0f, _wrongPullProgressPenalty);
        _noInputProgressPenalty = Mathf.Max(0f, _noInputProgressPenalty);
        _rarity1NoInputPenaltyMultiplier = Mathf.Max(0.01f, _rarity1NoInputPenaltyMultiplier);
        _rarity2NoInputPenaltyMultiplier = Mathf.Max(0.01f, _rarity2NoInputPenaltyMultiplier);
        _rarity3NoInputPenaltyMultiplier = Mathf.Max(0.01f, _rarity3NoInputPenaltyMultiplier);
        _minimumPullIntensity = Mathf.Clamp01(_minimumPullIntensity);
        _rarity1PullProgressMultiplier = Mathf.Max(0.01f, _rarity1PullProgressMultiplier);
        _rarity2PullProgressMultiplier = Mathf.Max(0.01f, _rarity2PullProgressMultiplier);
        _rarity3PullProgressMultiplier = Mathf.Max(0.01f, _rarity3PullProgressMultiplier);
        _rarity1DirectionIntervalMultiplier = Mathf.Max(0.01f, _rarity1DirectionIntervalMultiplier);
        _rarity2DirectionIntervalMultiplier = Mathf.Max(0.01f, _rarity2DirectionIntervalMultiplier);
        _rarity3DirectionIntervalMultiplier = Mathf.Max(0.01f, _rarity3DirectionIntervalMultiplier);
    }

    public void StartPull(FishScriptableObject _fishType = null)
    {
        StartPull(_fishType, null);
    }

    public void StartPull(FishScriptableObject _fishType, BaitData _bait)
    {
        _isPullRunning = true;
        _currentBait = _bait;
        SetPullActive(false);

        ActiveDirectionTimeNormalized = 1f;
        _lastNotifiedTimer = -1f;

        ResetInputFeedback();
        ApplyDifficultyFromFish(_fishType);
        GenerateNextPrompt();
        ResetDirectionTimer();

        PullStarted?.Invoke();
        NotifyPromptStateChanged();
    }

    public void StopPull()
    {
        _isPullRunning = false;
        _currentBait = null;
        SetPullActive(false);
        _directionTimer = 0f;
        ActiveDirectionTimeNormalized = 1f;

        ResetInputFeedback();

        PullStopped?.Invoke();
        NotifyPromptStateChanged();
    }

    public void PausePullFeedback()
    {
        if (!_isPullRunning)
            return;

        SetPullActive(false);
        ResetInputFeedback();
        NotifyPromptStateChanged();
    }

    public void SetUpgradeModifiers(float _pullProgressMultiplier, float _directionIntervalMultiplier)
    {
        _pullProgressUpgradeMultiplier = Mathf.Max(0.01f, _pullProgressMultiplier);
        _directionIntervalUpgradeMultiplier = Mathf.Max(0.01f, _directionIntervalMultiplier);
    }

    public float GetProgressModifier(Vector2 _input, float _progressNormalized)
    {
        if (!_useDirectionalPull)
        {
            ResetInputFeedback();
            return 0f;
        }

        CurrentIntensity = Mathf.Max(_minimumPullIntensity, GetIntensityByProgress(_progressNormalized));
        SetPullActive(_isPullRunning && _progressNormalized >= _startDisplayProgress);

        if (!IsPullActive)
        {
            ResetInputFeedback();
            NotifyPromptStateChanged();
            return 0f;
        }

        _directionTimer -= Time.deltaTime;

        if (_directionTimer <= 0f)
            GenerateNewDirection();

        UpdateActiveDirectionTimer();
        UpdateInputFeedback(_input);
        NotifyPromptStateChanged();

        float pullModifier = CurrentIntensity * _currentPullProgressMultiplier * _pullProgressUpgradeMultiplier;

        if (!HasPullInput)
            return -_noInputProgressPenalty * pullModifier * _currentNoInputPenaltyMultiplier;

        if (IsCorrectPullInput)
            return _correctPullProgressSpeed * pullModifier;

        return -_wrongPullProgressPenalty * pullModifier;
    }

    private void SetPullActive(bool _isActive)
    {
        if (IsPullActive == _isActive)
            return;

        IsPullActive = _isActive;
        PullActiveChanged?.Invoke(IsPullActive);
    }

    private void NotifyPromptStateChanged()
    {
        NotifyTimerChanged();
        PromptStateChanged?.Invoke();
    }

    private void NotifyTimerChanged()
    {
        if (Mathf.Approximately(_lastNotifiedTimer, ActiveDirectionTimeNormalized))
            return;

        _lastNotifiedTimer = ActiveDirectionTimeNormalized;
        ActiveDirectionTimerChanged?.Invoke(ActiveDirectionTimeNormalized);
    }

    private float GetIntensityByProgress(float _progressNormalized)
    {
        float endProgress = Mathf.Clamp01(_startInfluenceProgress + _fadeInRange);
        return Mathf.InverseLerp(_startInfluenceProgress, endProgress, _progressNormalized);
    }

    private void UpdateInputFeedback(Vector2 _input)
    {
        CurrentPullInput = _input;
        HasPullInput = _input.magnitude >= _inputThreshold;

        if (!HasPullInput)
        {
            PullInputNormalized = 0f;
            IsCorrectPullInput = false;
            return;
        }

        Vector2 requiredDirection = GetDirectionVector(_currentRequiredDirection);
        float requiredInput = Vector2.Dot(_input.normalized, requiredDirection);
        float fishDirectionInput = Vector2.Dot(_input.normalized, FishMovementVector);

        PullInputNormalized = Mathf.Clamp01(requiredInput);
        IsCorrectPullInput = requiredInput >= _inputThreshold;
        HasPullInput = requiredInput >= _inputThreshold || fishDirectionInput >= _inputThreshold;
    }

    private void ResetInputFeedback()
    {
        HasPullInput = false;
        IsCorrectPullInput = false;
        PullInputNormalized = 0f;
        CurrentIntensity = 0f;
        CurrentPullInput = Vector2.zero;
    }

    private void GenerateNextPrompt()
    {
        ResetInputFeedback();

        FishForceDirection fishDirection = GetRandomAllowedDirection();
        SetFishDirection(fishDirection);
    }

    private void ResetDirectionTimer()
    {
        _directionTimer = GetCurrentDirectionInterval();
        ActiveDirectionTimeNormalized = 1f;
    }

    private void GenerateNewDirection()
    {
        FishForceDirection fishDirection = GetRandomAllowedDirection();
        SetFishDirection(fishDirection);
        ResetDirectionTimer();
    }

    private void SetFishDirection(FishForceDirection _fishDirection)
    {
        CurrentFishDirection = _fishDirection;
        _currentRequiredDirection = GetOppositeDirection(_fishDirection);
        RequiredPullVector = GetDirectionVector(_currentRequiredDirection);
        CurrentPromptId++;

        PromptChanged?.Invoke();
        NotifyPromptStateChanged();
    }

    private void UpdateActiveDirectionTimer()
    {
        float interval = GetCurrentDirectionInterval();
        ActiveDirectionTimeNormalized = Mathf.Clamp01(_directionTimer / interval);
    }

    private float GetCurrentDirectionInterval()
    {
        return Mathf.Max(
            0.1f,
            _directionChangeInterval * _currentDirectionIntervalMultiplier * _directionIntervalUpgradeMultiplier
        );
    }

    private void ApplyDifficultyFromFish(FishScriptableObject _fishType)
    {
        int rarity = _fishType != null ? _fishType.rarity : 1;

        switch (rarity)
        {
            case 1:
                _currentPullProgressMultiplier = _rarity1PullProgressMultiplier;
                _currentDirectionIntervalMultiplier = _rarity1DirectionIntervalMultiplier;
                _currentNoInputPenaltyMultiplier = _rarity1NoInputPenaltyMultiplier;
                break;

            case 2:
                _currentPullProgressMultiplier = _rarity2PullProgressMultiplier;
                _currentDirectionIntervalMultiplier = _rarity2DirectionIntervalMultiplier;
                _currentNoInputPenaltyMultiplier = _rarity2NoInputPenaltyMultiplier;
                break;

            case 3:
                _currentPullProgressMultiplier = _rarity3PullProgressMultiplier;
                _currentDirectionIntervalMultiplier = _rarity3DirectionIntervalMultiplier;
                _currentNoInputPenaltyMultiplier = _rarity3NoInputPenaltyMultiplier;
                break;

            case 4:
                _currentPullProgressMultiplier = _rarity3PullProgressMultiplier;
                _currentDirectionIntervalMultiplier = _rarity3DirectionIntervalMultiplier;
                _currentNoInputPenaltyMultiplier = _rarity3NoInputPenaltyMultiplier;
                break;

            default:
                _currentPullProgressMultiplier = _rarity1PullProgressMultiplier;
                _currentDirectionIntervalMultiplier = _rarity1DirectionIntervalMultiplier;
                _currentNoInputPenaltyMultiplier = _rarity1NoInputPenaltyMultiplier;
                break;
        }

        if (_currentBait != null)
            _currentDirectionIntervalMultiplier *= _currentBait.DirectionChangeIntervalMultiplier;
    }

    private FishForceDirection GetRandomAllowedDirection()
    {
        int directionOptions = _allowDownPrompts ? 4 : 3;
        int randomDirection = Random.Range(0, directionOptions);

        switch (randomDirection)
        {
            case 0:
                return FishForceDirection.Left;

            case 1:
                return FishForceDirection.Right;

            case 2:
                return FishForceDirection.Up;

            default:
                return FishForceDirection.Down;
        }
    }

    private FishForceDirection GetOppositeDirection(FishForceDirection _direction)
    {
        switch (_direction)
        {
            case FishForceDirection.Left:
                return FishForceDirection.Right;

            case FishForceDirection.Right:
                return FishForceDirection.Left;

            case FishForceDirection.Up:
                return FishForceDirection.Down;

            case FishForceDirection.Down:
                return FishForceDirection.Up;

            default:
                return FishForceDirection.Right;
        }
    }

    private Vector2 GetDirectionVector(FishForceDirection _direction)
    {
        switch (_direction)
        {
            case FishForceDirection.Left:
                return Vector2.left;

            case FishForceDirection.Right:
                return Vector2.right;

            case FishForceDirection.Up:
                return Vector2.up;

            case FishForceDirection.Down:
                return Vector2.down;

            default:
                return Vector2.zero;
        }
    }
}
