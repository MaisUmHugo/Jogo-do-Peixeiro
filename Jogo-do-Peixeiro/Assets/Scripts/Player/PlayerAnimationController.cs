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

    [Header("Movement Visual Hop")]
    [SerializeField] private bool _enableMovementVisualHop;
    [SerializeField] private Transform _movementHopTarget;
    [SerializeField, Min(0f)] private float _movementHopHeight = 0.04f;
    [SerializeField, Min(0f)] private float _movementHopFrequency = 9f;
    [SerializeField, Min(0f)] private float _movementHopReturnSpeed = 12f;
    [SerializeField] private bool _useAnimatorTimeForMovementHop = true;
    [SerializeField, Min(1f)] private float _movementHopLoopFrames = 40f;
    [SerializeField, Min(0f)] private float _movementHopFirstStartFrame = 0f;
    [SerializeField, Min(0f)] private float _movementHopFirstEndFrame = 20f;
    [SerializeField, Min(0f)] private float _movementHopSecondStartFrame = 23f;
    [SerializeField, Min(0f)] private float _movementHopSecondEndFrame = 35f;
    [SerializeField, Min(0f)] private float _afkIdleStartHopHeight = 0.04f;
    [SerializeField, Min(0f)] private float _afkIdleStartHopDelay = 0.5f;
    [SerializeField, Min(0.01f)] private float _afkIdleStartHopDuration = 0.28f;

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
    [SerializeField] private Transform _fishingOffsetTarget;
    [SerializeField] private float _fishingYOffset = -0.8f;
    [SerializeField] private float _fishingOffsetSmoothTime = 1.4f;
    [SerializeField] private float _fishingOffsetResetThreshold = 0.01f;

    [Header("Boat Visual Offset")]
    [SerializeField] private Transform _boatOffsetTarget;
    [SerializeField] private Vector3 _boatPositionOffset;
    [SerializeField] private Vector3 _boatRotationOffset;
    [SerializeField, Min(0f)] private float _fishingFacingMaxDegreesPerSecond = 540f;

    private float _currentFishingOffset;
    private float _fishingOffsetVelocity;
    private Transform _fishingOffsetParent;
    private Vector3 _fishingOffsetBaseLocalPosition;
    private bool _hasFishingOffsetBase;
    private Transform _boatOffsetParent;
    private Vector3 _boatOffsetBaseLocalPosition;
    private Quaternion _boatOffsetBaseLocalRotation;
    private bool _hasBoatOffsetBase;
    private Vector3 _fishingFacingTargetWorldPosition;
    private float _fishingFacingYawOffset;
    private bool _hasFishingFacingTarget;

    [Header("Debug")]
    [SerializeField] private bool _logMissingParameters = true;

    private readonly Dictionary<int, AnimatorControllerParameterType> _parameterTypes = new();
    private readonly HashSet<int> _loggedMissingParameters = new();

    private float _idleTimer;
    private bool _wasFishing;
    private bool _hasLastPosition;
    private bool _isAfkIdleActive;
    private bool _leftAfkIdleByMoveInput;
    private Transform _movementHopParent;
    private Vector3 _movementHopBaseLocalPosition;
    private bool _hasMovementHopBase;
    private float _movementHopPhase;
    private float _currentMovementHopOffset;
    private float _afkIdleStartHopDelayTimer;
    private float _afkIdleStartHopTimer;
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
        _fishingOffsetSmoothTime = Mathf.Max(0f, _fishingOffsetSmoothTime);
        _fishingOffsetResetThreshold = Mathf.Max(0f, _fishingOffsetResetThreshold);
        _fishingFacingMaxDegreesPerSecond = Mathf.Max(0f, _fishingFacingMaxDegreesPerSecond);
        _movementHopHeight = Mathf.Max(0f, _movementHopHeight);
        _movementHopFrequency = Mathf.Max(0f, _movementHopFrequency);
        _movementHopReturnSpeed = Mathf.Max(0f, _movementHopReturnSpeed);
        _movementHopLoopFrames = Mathf.Max(1f, _movementHopLoopFrames);
        _movementHopFirstStartFrame = Mathf.Max(0f, _movementHopFirstStartFrame);
        _movementHopFirstEndFrame = Mathf.Max(0f, _movementHopFirstEndFrame);
        _movementHopSecondStartFrame = Mathf.Max(0f, _movementHopSecondStartFrame);
        _movementHopSecondEndFrame = Mathf.Max(0f, _movementHopSecondEndFrame);
        _afkIdleStartHopHeight = Mathf.Max(0f, _afkIdleStartHopHeight);
        _afkIdleStartHopDelay = Mathf.Max(0f, _afkIdleStartHopDelay);
        _afkIdleStartHopDuration = Mathf.Max(0.01f, _afkIdleStartHopDuration);
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
        UpdateBoatVisualOffset();
    }

    private void LateUpdate()
    {
        UpdateMovementVisualHop();
    }

    private void OnAnimatorMove()
    {
        if (_animator == null)
            return;

        UpdateFishingVisualOffset();
    }

    public void ResetFishingVisualOffset()
    {
        Transform offsetTarget = ResolveFishingOffsetTarget();

        if (offsetTarget != null &&
            _hasFishingOffsetBase &&
            offsetTarget.parent == _fishingOffsetParent)
        {
            offsetTarget.localPosition = GetCurrentFishingBaseLocalPosition(offsetTarget);
        }

        ClearFishingOffsetState();
    }

    public void ApplyBoatVisualOffset()
    {
        Transform offsetTarget = ResolveBoatOffsetTarget();

        if (offsetTarget == null)
            return;

        if (!_hasBoatOffsetBase || offsetTarget.parent != _boatOffsetParent)
            CaptureBoatOffsetBase(offsetTarget);

        offsetTarget.localPosition = _boatOffsetBaseLocalPosition + _boatPositionOffset;
        offsetTarget.localRotation = GetSmoothedBoatVisualLocalRotation(offsetTarget);
    }

    public void ResetBoatVisualOffset()
    {
        Transform offsetTarget = ResolveBoatOffsetTarget();

        if (offsetTarget != null &&
            _hasBoatOffsetBase &&
            offsetTarget.parent == _boatOffsetParent)
        {
            offsetTarget.localPosition = _boatOffsetBaseLocalPosition;
            offsetTarget.localRotation = _boatOffsetBaseLocalRotation;
        }

        ClearBoatOffsetState();
    }

    public Transform GetBoatVisualOffsetTarget()
    {
        return ResolveBoatOffsetTarget();
    }

    public void SetFishingFacingTarget(Vector3 _targetWorldPosition, float _yawOffset)
    {
        _fishingFacingTargetWorldPosition = _targetWorldPosition;
        _fishingFacingYawOffset = _yawOffset;
        _hasFishingFacingTarget = true;
    }

    public void SnapFishingFacingToTarget()
    {
        if (!_hasFishingFacingTarget)
            return;

        Transform offsetTarget = ResolveBoatOffsetTarget();

        if (offsetTarget == null)
            return;

        if (!_hasBoatOffsetBase || offsetTarget.parent != _boatOffsetParent)
            CaptureBoatOffsetBase(offsetTarget);

        offsetTarget.localRotation = GetBoatVisualLocalRotation(offsetTarget);
    }

    public void ClearFishingFacingTarget()
    {
        _hasFishingFacingTarget = false;
    }

    private void UpdateFishingVisualOffset()
    {
        float targetOffset = 0f;
        bool isFishing = GameManager.instance != null && IsFishingState(GameManager.instance.currentState);
        Transform offsetTarget = ResolveFishingOffsetTarget();

        if (offsetTarget == null)
            return;

        if (!isFishing && !_hasFishingOffsetBase && Mathf.Abs(_currentFishingOffset) <= _fishingOffsetResetThreshold)
            return;

        if (_hasFishingOffsetBase && offsetTarget.parent != _fishingOffsetParent)
        {
            ClearFishingOffsetState();
            return;
        }

        if (isFishing)
        {
            if (!_hasFishingOffsetBase)
                CaptureFishingOffsetBase(offsetTarget);

            targetOffset = _fishingYOffset;
        }

        if (_fishingOffsetSmoothTime <= 0f)
        {
            _currentFishingOffset = targetOffset;
            _fishingOffsetVelocity = 0f;
        }
        else
        {
            _currentFishingOffset = Mathf.SmoothDamp(
                _currentFishingOffset,
                targetOffset,
                ref _fishingOffsetVelocity,
                _fishingOffsetSmoothTime
            );
        }

        if (_hasFishingOffsetBase)
        {
            Vector3 pos = GetCurrentFishingBaseLocalPosition(offsetTarget);
            pos.y += _currentFishingOffset;
            offsetTarget.localPosition = pos;
        }

        if (!isFishing && Mathf.Abs(_currentFishingOffset) <= _fishingOffsetResetThreshold)
        {
            if (_hasFishingOffsetBase)
                offsetTarget.localPosition = GetCurrentFishingBaseLocalPosition(offsetTarget);

            ClearFishingOffsetState();
        }
    }

    private Transform ResolveFishingOffsetTarget()
    {
        if (_fishingOffsetTarget != null)
            return _fishingOffsetTarget;

        return _animator != null ? _animator.transform : null;
    }

    private void UpdateMovementVisualHop()
    {
        Transform hopTarget = ResolveMovementHopTarget();

        if (hopTarget == null)
            return;

        if (_hasMovementHopBase && hopTarget.parent != _movementHopParent)
        {
            ClearMovementHopState();
            return;
        }

        bool shouldHop = ShouldApplyMovementHop();

        if (!shouldHop && !_hasMovementHopBase && Mathf.Abs(_currentMovementHopOffset) <= 0.001f)
            return;

        if (!_hasMovementHopBase)
            CaptureMovementHopBase(hopTarget);

        float targetOffset = GetMovementHopTargetOffset(shouldHop);

        if (IsMoving)
        {
            _currentMovementHopOffset = targetOffset;
        }
        else
        {
            float followT = _movementHopReturnSpeed > 0f
                ? 1f - Mathf.Exp(-_movementHopReturnSpeed * Time.deltaTime)
                : 1f;

            _currentMovementHopOffset = Mathf.Lerp(_currentMovementHopOffset, targetOffset, followT);
        }

        Vector3 localPosition = _movementHopBaseLocalPosition;
        localPosition.y += _currentMovementHopOffset;
        hopTarget.localPosition = localPosition;

        if (!shouldHop && Mathf.Abs(_currentMovementHopOffset) <= 0.001f)
        {
            hopTarget.localPosition = _movementHopBaseLocalPosition;
            ClearMovementHopState();
        }
    }

    private Transform ResolveMovementHopTarget()
    {
        return _movementHopTarget;
    }

    private bool ShouldApplyMovementHop()
    {
        if (!_enableMovementVisualHop || GameManager.instance == null)
            return false;

        if (GameManager.instance.currentState != GameManager.GameState.OnFoot)
            return false;

        return IsMoving ||
               _afkIdleStartHopDelayTimer > 0f ||
               _afkIdleStartHopTimer > 0f;
    }

    private float GetMovementHopTargetOffset(bool _shouldHop)
    {
        if (!_shouldHop)
            return 0f;

        if (IsMoving)
            return GetWalkMovementHopOffset();

        if (_afkIdleStartHopDelayTimer > 0f)
        {
            _afkIdleStartHopDelayTimer = Mathf.Max(0f, _afkIdleStartHopDelayTimer - Time.deltaTime);
            return 0f;
        }

        if (_afkIdleStartHopTimer <= 0f)
            return 0f;

        _afkIdleStartHopTimer = Mathf.Max(0f, _afkIdleStartHopTimer - Time.deltaTime);
        float normalized = 1f - (_afkIdleStartHopTimer / Mathf.Max(0.01f, _afkIdleStartHopDuration));
        return Mathf.Sin(normalized * Mathf.PI) * _afkIdleStartHopHeight;
    }

    private float GetWalkMovementHopOffset()
    {
        if (_useAnimatorTimeForMovementHop && _animator != null && _animator.runtimeAnimatorController != null)
        {
            AnimatorStateInfo stateInfo = _animator.GetCurrentAnimatorStateInfo(0);
            float frame = Mathf.Repeat(stateInfo.normalizedTime, 1f) * _movementHopLoopFrames;
            return GetWalkFrameHopOffset(frame);
        }

        _movementHopPhase += Time.deltaTime * _movementHopFrequency * Mathf.Lerp(0.8f, 1.2f, CurrentMoveSpeed);
        return Mathf.Abs(Mathf.Sin(_movementHopPhase)) * _movementHopHeight * CurrentMoveSpeed;
    }

    private float GetWalkFrameHopOffset(float _frame)
    {
        float firstHop = GetFrameWindowHopOffset(
            _frame,
            _movementHopFirstStartFrame,
            _movementHopFirstEndFrame
        );
        float secondHop = GetFrameWindowHopOffset(
            _frame,
            _movementHopSecondStartFrame,
            _movementHopSecondEndFrame
        );

        return Mathf.Max(firstHop, secondHop) * CurrentMoveSpeed;
    }

    private float GetFrameWindowHopOffset(float _frame, float _startFrame, float _endFrame)
    {
        if (_endFrame <= _startFrame || _frame < _startFrame || _frame > _endFrame)
            return 0f;

        float normalized = Mathf.InverseLerp(_startFrame, _endFrame, _frame);
        return Mathf.Sin(normalized * Mathf.PI) * _movementHopHeight;
    }

    private void CaptureMovementHopBase(Transform _hopTarget)
    {
        _movementHopParent = _hopTarget.parent;
        _movementHopBaseLocalPosition = _hopTarget.localPosition;
        _hasMovementHopBase = true;
    }

    private void ClearMovementHopState()
    {
        _hasMovementHopBase = false;
        _movementHopParent = null;
        _movementHopPhase = 0f;
        _currentMovementHopOffset = 0f;
        _afkIdleStartHopDelayTimer = 0f;
        _afkIdleStartHopTimer = 0f;
    }

    private void UpdateBoatVisualOffset()
    {
        bool shouldApplyBoatOffset = GameManager.instance != null &&
                                     IsBoatVisualOffsetState(GameManager.instance.currentState);

        if (shouldApplyBoatOffset)
        {
            ApplyBoatVisualOffset();
            return;
        }

        if (_hasBoatOffsetBase)
            ResetBoatVisualOffset();
    }

    private bool IsBoatVisualOffsetState(GameManager.GameState _state)
    {
        return _state == GameManager.GameState.OnBoat ||
               IsFishingState(_state) ||
               ((_state == GameManager.GameState.InUI || _state == GameManager.GameState.Paused) && _hasBoatOffsetBase);
    }

    private Transform ResolveBoatOffsetTarget()
    {
        if (_boatOffsetTarget != null)
            return _boatOffsetTarget;

        return _animator != null ? _animator.transform : null;
    }

    private Quaternion GetBoatVisualLocalRotation(Transform _offsetTarget)
    {
        Quaternion baseRotation = _boatOffsetBaseLocalRotation * Quaternion.Euler(_boatRotationOffset);

        if (!_hasFishingFacingTarget || _offsetTarget == null || _offsetTarget.parent == null)
            return baseRotation;

        Vector3 localDirection = _offsetTarget.parent.InverseTransformDirection(
            _fishingFacingTargetWorldPosition - _offsetTarget.position
        );
        localDirection.y = 0f;

        if (localDirection.sqrMagnitude <= 0.0001f)
            return baseRotation;

        Vector3 baseEuler = baseRotation.eulerAngles;
        float targetYaw = Quaternion.LookRotation(localDirection.normalized, Vector3.up).eulerAngles.y;

        return Quaternion.Euler(
            baseEuler.x,
            targetYaw + _fishingFacingYawOffset,
            baseEuler.z
        );
    }

    private Quaternion GetSmoothedBoatVisualLocalRotation(Transform _offsetTarget)
    {
        Quaternion targetRotation = GetBoatVisualLocalRotation(_offsetTarget);

        if (!_hasFishingFacingTarget ||
            _fishingFacingMaxDegreesPerSecond <= 0f ||
            !Application.isPlaying)
        {
            return targetRotation;
        }

        return Quaternion.RotateTowards(
            _offsetTarget.localRotation,
            targetRotation,
            _fishingFacingMaxDegreesPerSecond * Time.deltaTime
        );
    }

    private Vector3 GetCurrentFishingBaseLocalPosition(Transform _offsetTarget)
    {
        if (IsSameBoatOffsetTarget(_offsetTarget))
            return _boatOffsetBaseLocalPosition + _boatPositionOffset;

        return _fishingOffsetBaseLocalPosition;
    }

    private bool IsSameBoatOffsetTarget(Transform _offsetTarget)
    {
        return _offsetTarget != null &&
               _hasBoatOffsetBase &&
               _offsetTarget == ResolveBoatOffsetTarget() &&
               _offsetTarget.parent == _boatOffsetParent;
    }

    private void CaptureFishingOffsetBase(Transform _offsetTarget)
    {
        _fishingOffsetParent = _offsetTarget.parent;
        _fishingOffsetBaseLocalPosition = _offsetTarget.localPosition;
        _hasFishingOffsetBase = true;
    }

    private void ClearFishingOffsetState()
    {
        _currentFishingOffset = 0f;
        _fishingOffsetVelocity = 0f;
        _fishingOffsetParent = null;
        _hasFishingOffsetBase = false;
    }

    private void CaptureBoatOffsetBase(Transform _offsetTarget)
    {
        _boatOffsetParent = _offsetTarget.parent;
        _boatOffsetBaseLocalPosition = _offsetTarget.localPosition;
        _boatOffsetBaseLocalRotation = _offsetTarget.localRotation;
        _hasBoatOffsetBase = true;
    }

    private void ClearBoatOffsetState()
    {
        _boatOffsetParent = null;
        _boatOffsetBaseLocalPosition = Vector3.zero;
        _boatOffsetBaseLocalRotation = Quaternion.identity;
        _hasBoatOffsetBase = false;
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
        bool isOnBoat = IsBoatAnimationState(state, isFishing);
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

    private bool IsBoatAnimationState(GameManager.GameState _state, bool _isFishing)
    {
        if (_state == GameManager.GameState.OnBoat || _isFishing)
            return true;

        return (_state == GameManager.GameState.InUI || _state == GameManager.GameState.Paused) &&
               _hasBoatOffsetBase;
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
            _afkIdleStartHopDelayTimer = 0f;
            _afkIdleStartHopTimer = 0f;
            SetInteger(_idleStageParameter, _defaultIdleStage);
            return;
        }

        _idleTimer += Time.deltaTime;

        int idleStage =
            _idleTimer >= _afkIdleDelay
            ? _afkIdleStage
            : _defaultIdleStage;

        bool wasAfkIdleActive = _isAfkIdleActive;
        _isAfkIdleActive = idleStage == _afkIdleStage;

        if (!wasAfkIdleActive && _isAfkIdleActive)
        {
            _afkIdleStartHopDelayTimer = _afkIdleStartHopDelay;
            _afkIdleStartHopTimer = _afkIdleStartHopDuration;
        }

        SetInteger(_idleStageParameter, idleStage);
    }

    private void UpdateFishingTransition(bool _isFishing)
    {
        if (_isFishing && !_wasFishing)
            TriggerCastRod();

        if (!_isFishing && _wasFishing)
        {
            ResetFishingAnimationState();
            ResetFishingVisualOffset();
        }

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
            $"[PlayerAnimationController] Animator parameter '{_parameterName}' ({_expectedType}) não encontrado. " +
            "Crie o parametro no Animator Controller ou deixe o nome vazio para ignorar.",
            this
        );
    }
}
