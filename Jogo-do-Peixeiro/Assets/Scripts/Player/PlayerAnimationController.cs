using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    public enum PullDirection
    {
        None = 0,
        Left = 1,
        Right = 2,
        Up = 3,
        Down = 4
    }

    [Header("References")]
    [SerializeField] private Animator _animator;
    [SerializeField] private FishDirectionPull _fishDirectionPull;
    [SerializeField] private bool _autoFindAnimator = true;
    [SerializeField] private bool _autoFindDirectionPull = true;

    [Header("Movement")]
    [SerializeField, Min(0f)] private float _moveThreshold = 0.1f;
    [SerializeField, Min(0f)] private float _positionMoveResetThreshold = 0.01f;
    [SerializeField, Min(0f)] private float _moveSpeedDampTime = 0.08f;

    [Header("Idle AFK")]
    [SerializeField, Min(0f)] private float _afkIdleDelay = 8f;
    [SerializeField] private int _defaultIdleStage;
    [SerializeField] private int _afkIdleStage = 1;
    [SerializeField] private bool _ignorePositionMovementWhileAfkIdle = true;
    [SerializeField] private bool _snapMoveSpeedWhenLeavingAfkIdle = true;

    [Header("Animator Parameters")]
    [SerializeField] private string _moveSpeedParameter = "MoveSpeed";
    [SerializeField] private string _isMovingParameter = "IsMoving";
    [SerializeField] private string _isOnBoatParameter = "IsOnBoat";
    [SerializeField] private string _isFishingParameter = "IsFishing";
    [SerializeField] private string _hasFishBittenParameter = "HasFishBitten";
    [SerializeField] private string _idleStageParameter = "IdleStage";
    [SerializeField] private string _castRodParameter = "CastRod";
    [SerializeField] private string _pullDirectionParameter = "PullDirection";

    [Header("Fishing Offset")]
    [SerializeField] private float _fishingYOffset = -0.8f;
    [SerializeField] private float _fishingOffsetSmoothTime = 1.4f;

    private float _currentFishingOffset;
    private float _fishingOffsetVelocity;

    [Header("Debug")]
    [SerializeField] private bool _logMissingParameters = true;

    private readonly Dictionary<int, AnimatorControllerParameterType> _parameterTypes = new();
    private readonly HashSet<int> _loggedMissingParameters = new();

    private float _idleTimer;
    private bool _wasFishing;
    private bool _hasLastPosition;
    private bool _isAfkIdleActive;
    private bool _leftAfkIdleByMoveInput;
    private Vector3 _lastPosition;

    public Vector2 CurrentMoveInput { get; private set; }
    public float CurrentMoveSpeed { get; private set; }
    public bool IsMoving { get; private set; }
    public PullDirection CurrentPullDirection { get; private set; }

    private void OnValidate()
    {
        _moveThreshold = Mathf.Max(0f, _moveThreshold);
        _positionMoveResetThreshold = Mathf.Max(0f, _positionMoveResetThreshold);
        _moveSpeedDampTime = Mathf.Max(0f, _moveSpeedDampTime);
        _afkIdleDelay = Mathf.Max(0f, _afkIdleDelay);
    }

    private void Awake()
    {
        ResolveReferences();
        CacheAnimatorParameters();
    }

    private void OnEnable()
    {
        ResolveReferences();
        CacheAnimatorParameters();
    }

    private void Update()
    {
        if (_animator == null)
            return;

        UpdateAnimatorState();
    }

    private void OnAnimatorMove()
    {
        if (_animator == null)
            return;

        float targetOffset = 0f;

        if (GameManager.instance != null && IsFishingState(GameManager.instance.currentState))
            targetOffset = _fishingYOffset;

        _currentFishingOffset = Mathf.SmoothDamp(
            _currentFishingOffset,
            targetOffset,
            ref _fishingOffsetVelocity,
            _fishingOffsetSmoothTime
        );

        Vector3 pos = _animator.transform.localPosition;
        pos.y = _currentFishingOffset;
        _animator.transform.localPosition = pos;
    }

    public void TriggerCastRod()
    {
        SetTrigger(_castRodParameter);
    }

    public void SetPullDirection(PullDirection _direction)
    {
        CurrentPullDirection = _direction;
        SetInteger(_pullDirectionParameter, (int)_direction);
    }

    public void ResetFishingAnimationState()
    {
        SetPullDirection(PullDirection.None);
        ResetTrigger(_castRodParameter);
    }

    private void ResolveReferences()
    {
        if (_animator == null && _autoFindAnimator)
            _animator = GetComponentInChildren<Animator>(true);

        if (_fishDirectionPull == null && _autoFindDirectionPull)
            _fishDirectionPull = FindFirstObjectByType<FishDirectionPull>(FindObjectsInactive.Include);
    }

    private void CacheAnimatorParameters()
    {
        _parameterTypes.Clear();
        _loggedMissingParameters.Clear();

        if (_animator == null || _animator.runtimeAnimatorController == null)
            return;

        AnimatorControllerParameter[] parameters = _animator.parameters;

        for (int i = 0; i < parameters.Length; i++)
            _parameterTypes[parameters[i].nameHash] = parameters[i].type;
    }

    private void UpdateAnimatorState()
    {
        GameManager.GameState state = GameManager.instance != null
            ? GameManager.instance.currentState
            : GameManager.GameState.OnFoot;

        bool isOnFoot = state == GameManager.GameState.OnFoot;
        bool isFishing = IsFishingState(state);
        bool isOnBoat = state == GameManager.GameState.OnBoat || isFishing;
        bool hasFishBitten = FishingManager.instance != null && FishingManager.instance.HasFishBitten;

        _leftAfkIdleByMoveInput = false;

        UpdateMovementState(isOnFoot);
        UpdateIdleState(isOnFoot);
        UpdateFishingTransition(isFishing);
        UpdatePullDirection(isFishing && hasFishBitten);

        float moveSpeedDampTime = _snapMoveSpeedWhenLeavingAfkIdle && _leftAfkIdleByMoveInput
            ? 0f
            : _moveSpeedDampTime;

        SetFloat(_moveSpeedParameter, CurrentMoveSpeed, moveSpeedDampTime);
        SetBool(_isMovingParameter, IsMoving);
        SetBool(_isOnBoatParameter, isOnBoat);
        SetBool(_isFishingParameter, isFishing);
        SetBool(_hasFishBittenParameter, hasFishBitten);
    }

    private bool IsFishingState(GameManager.GameState _state)
    {
        return _state == GameManager.GameState.Fishing ||
               (FishingManager.instance != null && FishingManager.instance.IsFishing);
    }

    private void UpdateMovementState(bool _isOnFoot)
    {
        CurrentMoveInput = _isOnFoot && InputHandler.instance != null
            ? InputHandler.instance.moveInput
            : Vector2.zero;

        CurrentMoveSpeed = Mathf.Clamp01(CurrentMoveInput.magnitude);

        bool hasMoveInput = _isOnFoot && CurrentMoveSpeed > _moveThreshold;

        _leftAfkIdleByMoveInput = _isAfkIdleActive && hasMoveInput;

        bool shouldIgnorePositionMovement =
            _ignorePositionMovementWhileAfkIdle &&
            _isAfkIdleActive &&
            !hasMoveInput;

        bool hasPositionMovement =
            shouldIgnorePositionMovement
            ? SyncLastPosition()
            : HasPositionMovement(_isOnFoot);

        IsMoving = hasMoveInput || hasPositionMovement;
    }

    private bool SyncLastPosition()
    {
        _lastPosition = transform.position;
        _hasLastPosition = true;
        return false;
    }

    private bool HasPositionMovement(bool _isOnFoot)
    {
        Vector3 currentPosition = transform.position;

        if (!_isOnFoot)
        {
            _lastPosition = currentPosition;
            _hasLastPosition = true;
            return false;
        }

        if (!_hasLastPosition)
        {
            _lastPosition = currentPosition;
            _hasLastPosition = true;
            return false;
        }

        Vector3 delta = currentPosition - _lastPosition;

        delta.y = 0f;

        _lastPosition = currentPosition;

        float threshold = Mathf.Max(0f, _positionMoveResetThreshold);

        return delta.sqrMagnitude > threshold * threshold;
    }

    private void UpdateIdleState(bool _isOnFoot)
    {
        if (!_isOnFoot || IsMoving)
        {
            _idleTimer = 0f;
            _isAfkIdleActive = false;
            SetInteger(_idleStageParameter, _defaultIdleStage);
            return;
        }

        _idleTimer += Time.deltaTime;

        int idleStage =
            _idleTimer >= _afkIdleDelay
            ? _afkIdleStage
            : _defaultIdleStage;

        _isAfkIdleActive = idleStage == _afkIdleStage;

        SetInteger(_idleStageParameter, idleStage);
    }

    private void UpdateFishingTransition(bool _isFishing)
    {
        if (_isFishing && !_wasFishing)
            TriggerCastRod();

        if (!_isFishing && _wasFishing)
            ResetFishingAnimationState();

        _wasFishing = _isFishing;
    }

    private void UpdatePullDirection(bool _shouldUsePullDirection)
    {
        if (!_shouldUsePullDirection ||
            _fishDirectionPull == null ||
            !_fishDirectionPull.IsPullActive)
        {
            SetPullDirection(PullDirection.None);
            return;
        }

        SetPullDirection(
            ToPullDirection(
                _fishDirectionPull.ActiveRequiredPullDirection
            )
        );
    }

    private PullDirection ToPullDirection(FishDirectionPull.FishForceDirection _direction)
    {
        return _direction switch
        {
            FishDirectionPull.FishForceDirection.Left => PullDirection.Left,
            FishDirectionPull.FishForceDirection.Right => PullDirection.Right,
            FishDirectionPull.FishForceDirection.Up => PullDirection.Up,
            FishDirectionPull.FishForceDirection.Down => PullDirection.Down,
            _ => PullDirection.None
        };
    }

    private void SetFloat(string _parameterName, float _value, float _dampTime = 0f)
    {
        if (!HasParameter(_parameterName, AnimatorControllerParameterType.Float))
            return;

        int parameterHash = Animator.StringToHash(_parameterName);

        if (_dampTime > 0f)
            _animator.SetFloat(parameterHash, _value, _dampTime, Time.deltaTime);
        else
            _animator.SetFloat(parameterHash, _value);
    }

    private void SetInteger(string _parameterName, int _value)
    {
        if (!HasParameter(_parameterName, AnimatorControllerParameterType.Int))
            return;

        _animator.SetInteger(Animator.StringToHash(_parameterName), _value);
    }

    private void SetBool(string _parameterName, bool _value)
    {
        if (!HasParameter(_parameterName, AnimatorControllerParameterType.Bool))
            return;

        _animator.SetBool(Animator.StringToHash(_parameterName), _value);
    }

    private void SetTrigger(string _parameterName)
    {
        if (!HasParameter(_parameterName, AnimatorControllerParameterType.Trigger))
            return;

        _animator.SetTrigger(Animator.StringToHash(_parameterName));
    }

    private void ResetTrigger(string _parameterName)
    {
        if (!HasParameter(_parameterName, AnimatorControllerParameterType.Trigger))
            return;

        _animator.ResetTrigger(Animator.StringToHash(_parameterName));
    }

    private bool HasParameter(string _parameterName, AnimatorControllerParameterType _expectedType)
    {
        if (string.IsNullOrWhiteSpace(_parameterName))
            return false;

        int parameterHash = Animator.StringToHash(_parameterName);

        if (_parameterTypes.TryGetValue(parameterHash, out AnimatorControllerParameterType type) &&
            type == _expectedType)
        {
            return true;
        }

        LogMissingParameterOnce(parameterHash, _parameterName, _expectedType);

        return false;
    }

    private void LogMissingParameterOnce(
        int _parameterHash,
        string _parameterName,
        AnimatorControllerParameterType _expectedType)
    {
        if (!_logMissingParameters ||
            _loggedMissingParameters.Contains(_parameterHash))
            return;

        _loggedMissingParameters.Add(_parameterHash);

        Debug.LogWarning(
            $"[PlayerAnimationController] Animator parameter '{_parameterName}' ({_expectedType}) nao encontrado. " +
            "Crie o parametro no Animator Controller ou deixe o nome vazio para ignorar.",
            this
        );
    }
}