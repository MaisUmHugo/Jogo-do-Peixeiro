using System;
using UnityEngine;
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
    public event Action<float> CompletionChanged;
    public event Action<float> PenaltyApplied;

    [Header("Settings")]
    [SerializeField] private bool _useDirectionalPull = true;
    [SerializeField, Range(0f, 1f)] private float _startDisplayProgress = 0.75f;
    [SerializeField, Range(0f, 1f)] private float _startInfluenceProgress = 0.75f;
    [SerializeField, Range(0.01f, 1f)] private float _fadeInRange = 0.25f;
    [SerializeField] private float _directionChangeInterval = 2f;
    [SerializeField] private float _inputThreshold = 0.35f;

    [Header("Progress Modifier")]
    [SerializeField] private float _correctPullProgressSpeed = 0.12f;
    [SerializeField] private float _wrongPullProgressPenalty = 0.1f;
    [SerializeField] private float _noInputProgressPenalty = 0.03f;

    [Header("Completion Gate")]
    [SerializeField] private bool _requireCompletionToFinish = true;
    [SerializeField, Range(0f, 1f)] private float _completionGateProgress = 0.99f;
    [SerializeField] private float _completionBuildSpeed = 0.85f;
    [SerializeField] private float _completionDecaySpeed = 0.2f;
    [SerializeField] private float _wrongInputCompletionPenalty = 0.45f;
    [SerializeField, Range(0f, 1f)] private float _minimumCompletionIntensity = 0.35f;

    [Header("Input Sequence")]
    [SerializeField] private bool _useInputSequence = true;
    [SerializeField] private bool _sequenceAffectsFishingProgress;
    [SerializeField] private bool _allowDownPrompts;
    [SerializeField] private int _rarity1SequenceSteps = 3;
    [SerializeField] private int _rarity2SequenceSteps = 4;
    [SerializeField] private int _rarity3SequenceSteps = 5;
    [SerializeField] private float _sequenceStepBuildSpeed = 3.5f;
    [SerializeField] private float _sequenceStepDecaySpeed = 0.15f;
    [SerializeField] private float _wrongInputStepPenalty = 0.5f;

    [Header("Input Sequence Timing")]
    [SerializeField] private bool _useSequenceTimer = true;
    [SerializeField] private float _sequenceDirectionDuration = 2.2f;
    [SerializeField, Range(0f, 1f)] private float _timeoutFishingProgressPenalty = 0.05f;

    [Header("Difficulty Multipliers")]
    [SerializeField] private float _rarity1CompletionBuildMultiplier = 1f;
    [SerializeField] private float _rarity2CompletionBuildMultiplier = 0.85f;
    [SerializeField] private float _rarity3CompletionBuildMultiplier = 0.7f;
    [SerializeField] private float _rarity1DirectionIntervalMultiplier = 1f;
    [SerializeField] private float _rarity2DirectionIntervalMultiplier = 0.85f;
    [SerializeField] private float _rarity3DirectionIntervalMultiplier = 0.7f;
    [SerializeField] private float _rarity1SequenceTimeMultiplier = 1f;
    [SerializeField] private float _rarity2SequenceTimeMultiplier = 1f;
    [SerializeField] private float _rarity3SequenceTimeMultiplier = 0.85f;

    public FishForceDirection CurrentFishDirection { get; private set; }
    public FishForceDirection RequiredPullDirection => GetOppositeDirection(CurrentFishDirection);
    public bool UseDirectionalPull => _useDirectionalPull;
    public bool RequiresCompletionToFinish => _useDirectionalPull && _requireCompletionToFinish;
    public bool IsPullActive { get; private set; }
    public bool HasPullInput { get; private set; }
    public bool IsCorrectPullInput { get; private set; }
    public float PullInputNormalized { get; private set; }
    public float CurrentIntensity { get; private set; }
    public float CompletionNormalized { get; private set; }
    public float CurrentStepCompletionNormalized { get; private set; }
    public float ActiveDirectionTimeNormalized { get; private set; } = 1f;
    public int ActiveRequiredDirectionIndex { get; private set; }
    public int RequiredPullDirectionCount => 1;
    public int CurrentSequenceStepIndex { get; private set; }
    public int SequenceStepCount { get; private set; }
    public int CurrentPromptId { get; private set; }
    public Vector2 RequiredPullVector { get; private set; }
    public Vector2 ActiveRequiredPullVector => GetDirectionVector(ActiveRequiredPullDirection);
    public FishForceDirection ActiveRequiredPullDirection => _currentRequiredDirection;
    public Vector2 CurrentPullInput { get; private set; }
    public float CompletionGateProgress => _completionGateProgress;
    public bool IsCompletionComplete => !RequiresCompletionToFinish || CompletionNormalized >= 1f;
    public bool ShouldBlockSkillCheck => UseDirectionalPull &&
                                         IsPullActive &&
                                         !IsCompletionComplete &&
                                         CurrentIntensity > 0f;

    private FishForceDirection _currentRequiredDirection;
    private float _directionTimer;
    private float _completionUpgradeMultiplier = 1f;
    private float _directionIntervalUpgradeMultiplier = 1f;
    private float _currentCompletionBuildMultiplier = 1f;
    private float _currentDirectionIntervalMultiplier = 1f;
    private float _currentSequenceTimeMultiplier = 1f;
    private float _activeDirectionTimer;
    private bool _isPullRunning;
    private float _lastNotifiedCompletion = -1f;
    private float _lastNotifiedTimer = -1f;

    private void OnValidate()
    {
        _startDisplayProgress = Mathf.Clamp01(_startDisplayProgress);
        _startInfluenceProgress = Mathf.Clamp01(_startInfluenceProgress);
        _fadeInRange = Mathf.Max(0.01f, _fadeInRange);
        _directionChangeInterval = Mathf.Max(0.1f, _directionChangeInterval);
        _inputThreshold = Mathf.Max(0.01f, _inputThreshold);
        _completionGateProgress = Mathf.Clamp01(_completionGateProgress);
        _completionBuildSpeed = Mathf.Max(0f, _completionBuildSpeed);
        _completionDecaySpeed = Mathf.Max(0f, _completionDecaySpeed);
        _wrongInputCompletionPenalty = Mathf.Max(0f, _wrongInputCompletionPenalty);
        _minimumCompletionIntensity = Mathf.Clamp01(_minimumCompletionIntensity);
        _rarity1SequenceSteps = Mathf.Max(1, _rarity1SequenceSteps);
        _rarity2SequenceSteps = Mathf.Max(1, _rarity2SequenceSteps);
        _rarity3SequenceSteps = Mathf.Max(1, _rarity3SequenceSteps);
        _sequenceStepBuildSpeed = Mathf.Max(0.01f, _sequenceStepBuildSpeed);
        _sequenceStepDecaySpeed = Mathf.Max(0f, _sequenceStepDecaySpeed);
        _wrongInputStepPenalty = Mathf.Max(0f, _wrongInputStepPenalty);
        _sequenceDirectionDuration = Mathf.Max(0.1f, _sequenceDirectionDuration);
        _timeoutFishingProgressPenalty = Mathf.Clamp01(_timeoutFishingProgressPenalty);
        _rarity1CompletionBuildMultiplier = Mathf.Max(0.01f, _rarity1CompletionBuildMultiplier);
        _rarity2CompletionBuildMultiplier = Mathf.Max(0.01f, _rarity2CompletionBuildMultiplier);
        _rarity3CompletionBuildMultiplier = Mathf.Max(0.01f, _rarity3CompletionBuildMultiplier);
        _rarity1DirectionIntervalMultiplier = Mathf.Max(0.01f, _rarity1DirectionIntervalMultiplier);
        _rarity2DirectionIntervalMultiplier = Mathf.Max(0.01f, _rarity2DirectionIntervalMultiplier);
        _rarity3DirectionIntervalMultiplier = Mathf.Max(0.01f, _rarity3DirectionIntervalMultiplier);
        _rarity1SequenceTimeMultiplier = Mathf.Max(0.01f, _rarity1SequenceTimeMultiplier);
        _rarity2SequenceTimeMultiplier = Mathf.Max(0.01f, _rarity2SequenceTimeMultiplier);
        _rarity3SequenceTimeMultiplier = Mathf.Max(0.01f, _rarity3SequenceTimeMultiplier);
    }

    public void StartPull(FishScriptableObject _fishType = null)
    {
        _isPullRunning = true;
        SetPullActive(false);

        CompletionNormalized = 0f;
        CurrentStepCompletionNormalized = 0f;
        ActiveRequiredDirectionIndex = 0;
        CurrentSequenceStepIndex = 0;
        ActiveDirectionTimeNormalized = 1f;
        _lastNotifiedCompletion = -1f;
        _lastNotifiedTimer = -1f;

        ResetInputFeedback();
        ApplyDifficultyFromFish(_fishType);
        GenerateNextPrompt();

        PullStarted?.Invoke();
        NotifyPromptStateChanged();
    }

    public void StopPull()
    {
        _isPullRunning = false;
        SetPullActive(false);

        CompletionNormalized = 0f;
        CurrentStepCompletionNormalized = 0f;
        ActiveRequiredDirectionIndex = 0;
        CurrentSequenceStepIndex = 0;
        SequenceStepCount = 0;
        _directionTimer = 0f;

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

    public void SetUpgradeModifiers(float _completionMultiplier, float _directionIntervalMultiplier)
    {
        _completionUpgradeMultiplier = Mathf.Max(0.01f, _completionMultiplier);
        _directionIntervalUpgradeMultiplier = Mathf.Max(0.01f, _directionIntervalMultiplier);
    }

    public bool ShouldHoldCompletion(float _progressNormalized)
    {
        return RequiresCompletionToFinish &&
               _isPullRunning &&
               _progressNormalized >= _completionGateProgress &&
               !IsCompletionComplete;
    }

    public float GetProgressModifier(Vector2 _input, float _progressNormalized)
    {
        if (!_useDirectionalPull)
        {
            ResetInputFeedback();
            return 0f;
        }

        CurrentIntensity = GetIntensityByProgress(_progressNormalized);
        SetPullActive(_isPullRunning && _progressNormalized >= _startDisplayProgress);

        if (!IsPullActive)
        {
            ResetInputFeedback();
            NotifyPromptStateChanged();
            return 0f;
        }

        if (!_useInputSequence)
        {
            _directionTimer -= Time.deltaTime;

            if (_directionTimer <= 0f)
                GenerateNewDirection();
        }

        UpdateInputFeedback(_input);
        UpdateCompletion(_progressNormalized);
        NotifyPromptStateChanged();

        if (_useInputSequence && !_sequenceAffectsFishingProgress)
        {
            if (HasPullInput && !IsCorrectPullInput)
                return -_wrongPullProgressPenalty * CurrentIntensity;

            return 0f;
        }

        if (_input.magnitude < _inputThreshold)
            return -_noInputProgressPenalty * CurrentIntensity;

        if (IsCorrectPullInput)
            return _correctPullProgressSpeed * CurrentIntensity;

        return -_wrongPullProgressPenalty * CurrentIntensity;
    }

    public FishForceDirection GetRequiredPullDirection(int _index)
    {
        return _currentRequiredDirection;
    }

    public bool IsRequiredPullDirectionActive(int _index)
    {
        return !IsCompletionComplete && _index == 0;
    }

    public bool IsRequiredPullDirectionCompleted(int _index)
    {
        return _index == 0 && CurrentStepCompletionNormalized >= 1f;
    }

    public float GetRequiredPullDirectionProgress(int _index)
    {
        if (_index != 0)
            return 0f;

        return CurrentStepCompletionNormalized;
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
        NotifyCompletionChanged();
        NotifyTimerChanged();
        PromptStateChanged?.Invoke();
    }

    private void NotifyCompletionChanged()
    {
        if (Mathf.Approximately(_lastNotifiedCompletion, CompletionNormalized))
            return;

        _lastNotifiedCompletion = CompletionNormalized;
        CompletionChanged?.Invoke(CompletionNormalized);
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

        PullInputNormalized = Mathf.Clamp01(requiredInput);
        IsCorrectPullInput = PullInputNormalized >= _inputThreshold;
    }

    private void ResetInputFeedback()
    {
        HasPullInput = false;
        IsCorrectPullInput = false;
        PullInputNormalized = 0f;
        CurrentIntensity = 0f;
        CurrentPullInput = Vector2.zero;
    }

    private void UpdateCompletion(float _progressNormalized)
    {
        if (!RequiresCompletionToFinish ||
            IsCompletionComplete ||
            !IsPullActive ||
            _progressNormalized < _startInfluenceProgress)
        {
            ActiveDirectionTimeNormalized = 1f;
            return;
        }

        float completionIntensity = Mathf.Max(_minimumCompletionIntensity, CurrentIntensity);

        if (_useInputSequence)
        {
            UpdateSequenceCompletion(completionIntensity);
            return;
        }

        if (IsCorrectPullInput)
        {
            CompletionNormalized += _completionBuildSpeed *
                                    _currentCompletionBuildMultiplier *
                                    _completionUpgradeMultiplier *
                                    completionIntensity *
                                    Time.deltaTime;
        }
        else if (HasPullInput)
        {
            CompletionNormalized -= _wrongInputCompletionPenalty * Time.deltaTime;
        }
        else
        {
            CompletionNormalized -= _completionDecaySpeed * Time.deltaTime;
        }

        CompletionNormalized = Mathf.Clamp01(CompletionNormalized);
    }

    private void UpdateSequenceCompletion(float _completionIntensity)
    {
        if (SequenceStepCount <= 0)
            SequenceStepCount = 1;

        UpdateActiveDirectionTimer();

        if (_useSequenceTimer && ActiveDirectionTimeNormalized <= 0f)
        {
            FailCurrentSequenceStep();
            return;
        }

        if (IsCorrectPullInput)
        {
            CurrentStepCompletionNormalized += _sequenceStepBuildSpeed *
                                               _currentCompletionBuildMultiplier *
                                               _completionUpgradeMultiplier *
                                               _completionIntensity *
                                               Time.deltaTime;
        }
        else if (HasPullInput)
        {
            CurrentStepCompletionNormalized -= _wrongInputStepPenalty * Time.deltaTime;
        }
        else
        {
            CurrentStepCompletionNormalized -= _sequenceStepDecaySpeed * Time.deltaTime;
        }

        CurrentStepCompletionNormalized = Mathf.Clamp01(CurrentStepCompletionNormalized);
        UpdateSequenceCompletionNormalized();

        if (CurrentStepCompletionNormalized >= 1f)
            CompleteCurrentSequenceStep();
    }

    private void CompleteCurrentSequenceStep()
    {
        CurrentSequenceStepIndex++;

        if (CurrentSequenceStepIndex >= SequenceStepCount)
        {
            CurrentStepCompletionNormalized = 1f;
            CompletionNormalized = 1f;
            ResetInputFeedback();
            NotifyPromptStateChanged();
            return;
        }

        GenerateNextPrompt();
        UpdateSequenceCompletionNormalized();
        NotifyPromptStateChanged();
    }

    private void FailCurrentSequenceStep()
    {
        float penaltyIntensity = Mathf.Max(_minimumCompletionIntensity, CurrentIntensity);
        float penalty = _timeoutFishingProgressPenalty * penaltyIntensity;

        PenaltyApplied?.Invoke(penalty);

        GenerateNextPrompt();
        UpdateSequenceCompletionNormalized();
        NotifyPromptStateChanged();
    }

    private void UpdateSequenceCompletionNormalized()
    {
        float stepProgress = Mathf.Clamp01(CurrentStepCompletionNormalized);
        CompletionNormalized = Mathf.Clamp01((CurrentSequenceStepIndex + stepProgress) / SequenceStepCount);
    }

    private void UpdateActiveDirectionTimer()
    {
        if (!_useSequenceTimer)
        {
            ActiveDirectionTimeNormalized = 1f;
            return;
        }

        float duration = GetActiveDirectionDuration();
        _activeDirectionTimer -= Time.deltaTime;
        ActiveDirectionTimeNormalized = Mathf.Clamp01(_activeDirectionTimer / duration);
    }

    private void GenerateNextPrompt()
    {
        CurrentStepCompletionNormalized = 0f;
        ActiveRequiredDirectionIndex = 0;
        ResetInputFeedback();
        ResetActiveDirectionTimer();

        FishForceDirection requiredDirection = GetRandomAllowedDirection();
        SetRequiredDirection(requiredDirection);

        CurrentFishDirection = GetOppositeDirection(requiredDirection);
    }

    private void GenerateNewDirection()
    {
        FishForceDirection requiredDirection = GetRandomAllowedDirection();
        SetRequiredDirection(requiredDirection);

        CurrentFishDirection = GetOppositeDirection(requiredDirection);
        _directionTimer = _directionChangeInterval * _currentDirectionIntervalMultiplier * _directionIntervalUpgradeMultiplier;
    }

    private void SetRequiredDirection(FishForceDirection _direction)
    {
        _currentRequiredDirection = _direction;
        RequiredPullVector = GetDirectionVector(_direction);
        CurrentPromptId++;

        PromptChanged?.Invoke();
        NotifyPromptStateChanged();
    }

    private void ResetActiveDirectionTimer()
    {
        _activeDirectionTimer = GetActiveDirectionDuration();
        ActiveDirectionTimeNormalized = 1f;
    }

    private float GetActiveDirectionDuration()
    {
        return Mathf.Max(
            0.1f,
            _sequenceDirectionDuration * _currentSequenceTimeMultiplier * _directionIntervalUpgradeMultiplier
        );
    }

    private void ApplyDifficultyFromFish(FishScriptableObject _fishType)
    {
        int rarity = _fishType != null ? _fishType.rarity : 1;

        switch (rarity)
        {
            case 1:
                _currentCompletionBuildMultiplier = _rarity1CompletionBuildMultiplier;
                _currentDirectionIntervalMultiplier = _rarity1DirectionIntervalMultiplier;
                _currentSequenceTimeMultiplier = _rarity1SequenceTimeMultiplier;
                SequenceStepCount = _rarity1SequenceSteps;
                break;

            case 2:
                _currentCompletionBuildMultiplier = _rarity2CompletionBuildMultiplier;
                _currentDirectionIntervalMultiplier = _rarity2DirectionIntervalMultiplier;
                _currentSequenceTimeMultiplier = _rarity2SequenceTimeMultiplier;
                SequenceStepCount = _rarity2SequenceSteps;
                break;

            case 3:
                _currentCompletionBuildMultiplier = _rarity3CompletionBuildMultiplier;
                _currentDirectionIntervalMultiplier = _rarity3DirectionIntervalMultiplier;
                _currentSequenceTimeMultiplier = _rarity3SequenceTimeMultiplier;
                SequenceStepCount = _rarity3SequenceSteps;
                break;

            default:
                _currentCompletionBuildMultiplier = _rarity1CompletionBuildMultiplier;
                _currentDirectionIntervalMultiplier = _rarity1DirectionIntervalMultiplier;
                _currentSequenceTimeMultiplier = _rarity1SequenceTimeMultiplier;
                SequenceStepCount = _rarity1SequenceSteps;
                break;
        }
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
