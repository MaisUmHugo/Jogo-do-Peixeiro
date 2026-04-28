using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class FishDirectionPullUI : MonoBehaviour
{
    [Header("References")]
    [FormerlySerializedAs("directionPull")]
    [SerializeField] private FishDirectionPull _directionPull;
    [FormerlySerializedAs("iconDatabase")]
    [SerializeField] private InputIconDatabase _iconDatabase;
    [FormerlySerializedAs("directionIcon")]
    [SerializeField] private Image _directionIcon;
    [SerializeField] private Vector2 _iconAnchoredPosition = new Vector2(0f, 120f);
    [SerializeField] private Vector2 _iconSize = new Vector2(72f, 72f);

    [Header("Position")]
    [SerializeField] private float _horizontalPullOffset = 260f;
    [FormerlySerializedAs("_verticalPullOffset")]
    [SerializeField] private float _upPullOffset = 180f;
    [SerializeField] private float _downPullOffset = 180f;
    [SerializeField] private bool _randomizePositionOnDirectionChange;
    [SerializeField] private float _sideVerticalOffset = 110f;
    [SerializeField, Range(0f, 1f)] private float _sideButtonHeightMultiplier = 0.55f;
    [SerializeField] private float _sideHorizontalJitter = 45f;
    [SerializeField] private float _topHorizontalRange = 260f;
    [SerializeField] private float _positionFollowSpeed = 18f;

    [Header("Canvas Relative Position")]
    [SerializeField] private bool _useCanvasRelativeOffsets;
    [SerializeField, Range(0f, 0.5f)] private float _horizontalCanvasOffset = 0.32f;
    [SerializeField, Range(0f, 0.5f)] private float _sideVerticalCanvasOffset = 0.12f;
    [SerializeField, Range(0f, 0.5f)] private float _upCanvasOffset = 0.28f;
    [SerializeField, Range(0f, 0.5f)] private float _downCanvasOffset = 0.28f;
    [SerializeField] private bool _clampInsideParent = true;
    [SerializeField] private Vector2 _parentPadding = new Vector2(72f, 72f);

    [Header("Pulse")]
    [SerializeField] private float _pulseSpeed = 6f;
    [SerializeField] private float _activeIconBaseScale = 1f;
    [SerializeField] private float _activeIconMinimumScale = 0.45f;
    [SerializeField] private float _idlePulseScale = 0.1f;
    [SerializeField] private float _activePulseScale = 0.22f;
    [SerializeField, Range(0f, 1f)] private float _idleAlpha = 0.72f;
    [SerializeField, Range(0f, 1f)] private float _activeAlpha = 1f;

    [Header("Input Color")]
    [SerializeField] private Color _idleIconColor = Color.white;
    [SerializeField] private Color _pressedIconColor = new Color(0.25f, 1f, 0.35f, 1f);

    private RectTransform _directionIconRect;
    private Vector2 _currentIconTargetPosition;
    private int _lastPromptId = -1;
    private bool _hasIconTargetPosition;

    private void OnValidate()
    {
        _iconSize.x = Mathf.Max(1f, _iconSize.x);
        _iconSize.y = Mathf.Max(1f, _iconSize.y);
        _horizontalPullOffset = Mathf.Max(0f, _horizontalPullOffset);
        _upPullOffset = Mathf.Max(0f, _upPullOffset);
        _downPullOffset = Mathf.Max(0f, _downPullOffset);
        _sideVerticalOffset = Mathf.Max(0f, _sideVerticalOffset);
        _sideButtonHeightMultiplier = Mathf.Clamp01(_sideButtonHeightMultiplier);
        _sideHorizontalJitter = Mathf.Max(0f, _sideHorizontalJitter);
        _topHorizontalRange = Mathf.Max(0f, _topHorizontalRange);
        _positionFollowSpeed = Mathf.Max(0f, _positionFollowSpeed);
        _parentPadding.x = Mathf.Max(0f, _parentPadding.x);
        _parentPadding.y = Mathf.Max(0f, _parentPadding.y);
        _pulseSpeed = Mathf.Max(0f, _pulseSpeed);
        _activeIconBaseScale = Mathf.Max(0.01f, _activeIconBaseScale);
        _activeIconMinimumScale = Mathf.Clamp(_activeIconMinimumScale, 0.01f, _activeIconBaseScale);
        _idlePulseScale = Mathf.Max(0f, _idlePulseScale);
        _activePulseScale = Mathf.Max(_idlePulseScale, _activePulseScale);

        if (Application.isPlaying && isActiveAndEnabled)
            RefreshRuntimePosition();
    }

    private void Awake()
    {
        if (_directionPull == null)
            _directionPull = FindFirstObjectByType<FishDirectionPull>();

        if (_directionIcon == null)
            _directionIcon = GetComponent<Image>();

        if (_directionIcon == null)
            _directionIcon = CreateDirectionIcon();

        _directionIconRect = _directionIcon.rectTransform;
        SetVisible(false);
    }

    private void OnEnable()
    {
        SubscribeToEvents();
        RefreshDisplay();
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
        SetVisible(false);
    }

    private void SubscribeToEvents()
    {
        if (_directionPull != null)
        {
            _directionPull.PullActiveChanged += HandlePullActiveChanged;
            _directionPull.PullStopped += HandlePullStopped;
            _directionPull.PromptChanged += HandlePromptChanged;
            _directionPull.PromptStateChanged += HandlePromptStateChanged;
        }

        InputDeviceDetector.DeviceTypeChanged += HandleDeviceTypeChanged;
    }

    private void UnsubscribeFromEvents()
    {
        if (_directionPull != null)
        {
            _directionPull.PullActiveChanged -= HandlePullActiveChanged;
            _directionPull.PullStopped -= HandlePullStopped;
            _directionPull.PromptChanged -= HandlePromptChanged;
            _directionPull.PromptStateChanged -= HandlePromptStateChanged;
        }

        InputDeviceDetector.DeviceTypeChanged -= HandleDeviceTypeChanged;
    }

    private void HandlePullActiveChanged(bool _isActive)
    {
        RefreshDisplay();
    }

    private void HandlePullStopped()
    {
        SetVisible(false);
    }

    private void HandlePromptChanged()
    {
        _hasIconTargetPosition = false;
        _lastPromptId = -1;
        RefreshDisplay();
    }

    private void HandlePromptStateChanged()
    {
        RefreshDisplay();
    }

    private void HandleDeviceTypeChanged(InputDeviceType _deviceType)
    {
        RefreshDisplay();
    }

    private void RefreshDisplay()
    {
        if (!CanShowDirectionPull())
        {
            SetVisible(false);
            return;
        }

        if (!UpdateIcon())
            return;

        SetVisible(true);
        UpdateIconFeedback();
    }

    private bool UpdateIcon()
    {
        if (_directionIcon == null || _iconDatabase == null)
            return false;

        InputIconAction action = GetDirectionAction(_directionPull.RequiredPullDirection);
        Sprite icon = _iconDatabase.GetIcon(InputDeviceDetector.CurrentDeviceType, action);

        if (icon == null)
        {
            SetVisible(false);
            return false;
        }

        _directionIcon.sprite = icon;
        return true;
    }

    private void UpdateIconFeedback()
    {
        if (_directionIcon == null || _directionIconRect == null || _directionPull == null)
            return;

        UpdateIconTargetPosition();

        float followT = 1f - Mathf.Exp(-_positionFollowSpeed * Time.unscaledDeltaTime);
        float pulseT = (Mathf.Sin(Time.unscaledTime * _pulseSpeed) * 0.5f) + 0.5f;
        float inputAmount = _directionPull.PullInputNormalized;

        _directionIconRect.anchoredPosition = Vector2.Lerp(
            _directionIconRect.anchoredPosition,
            _currentIconTargetPosition,
            followT
        );

        float timerScale = Mathf.Lerp(
            _activeIconMinimumScale,
            _activeIconBaseScale,
            _directionPull.ActiveDirectionTimeNormalized
        );
        float pulseAmount = Mathf.Lerp(_idlePulseScale, _activePulseScale, inputAmount);
        _directionIconRect.localScale = Vector3.one * (timerScale + (pulseT * pulseAmount));

        Color color = Color.Lerp(_idleIconColor, _pressedIconColor, inputAmount);
        color.a = Mathf.Lerp(_idleAlpha, _activeAlpha, inputAmount);
        _directionIcon.color = color;
    }

    private bool CanShowDirectionPull()
    {
        return _directionPull != null &&
               _directionPull.UseDirectionalPull &&
               _directionPull.IsPullActive &&
               _iconDatabase != null &&
               _directionIcon != null;
    }

    private InputIconAction GetDirectionAction(FishDirectionPull.FishForceDirection _direction)
    {
        switch (_direction)
        {
            case FishDirectionPull.FishForceDirection.Left:
                return InputIconAction.MoveLeft;

            case FishDirectionPull.FishForceDirection.Right:
                return InputIconAction.MoveRight;

            case FishDirectionPull.FishForceDirection.Up:
                return InputIconAction.MoveUp;

            case FishDirectionPull.FishForceDirection.Down:
                return InputIconAction.MoveDown;

            default:
                return InputIconAction.MoveLeft;
        }
    }

    private void UpdateIconTargetPosition()
    {
        int promptId = _directionPull.CurrentPromptId;

        if (_hasIconTargetPosition && promptId == _lastPromptId)
            return;

        _lastPromptId = promptId;
        _currentIconTargetPosition = ClampToParent(GetIconPosition(_directionPull.RequiredPullDirection));
        _hasIconTargetPosition = true;
    }

    private Vector2 GetIconPosition(FishDirectionPull.FishForceDirection _direction)
    {
        switch (_direction)
        {
            case FishDirectionPull.FishForceDirection.Left:
                return GetSidePullPosition(-1f);

            case FishDirectionPull.FishForceDirection.Right:
                return GetSidePullPosition(1f);

            case FishDirectionPull.FishForceDirection.Up:
                return GetTopPullPosition();

            case FishDirectionPull.FishForceDirection.Down:
                return GetBottomPullPosition();

            default:
                return _iconAnchoredPosition;
        }
    }

    private Vector2 GetSidePullPosition(float _side)
    {
        float sideJitter = _randomizePositionOnDirectionChange ? Random.Range(-_sideHorizontalJitter, _sideHorizontalJitter) : 0f;
        float verticalOffset = _randomizePositionOnDirectionChange ? Random.Range(0f, GetSideVerticalOffset()) : GetSideVerticalOffset();

        return new Vector2(
            _iconAnchoredPosition.x + (GetHorizontalOffset() * _side) + sideJitter,
            _iconAnchoredPosition.y + verticalOffset
        );
    }

    private Vector2 GetTopPullPosition()
    {
        float horizontalOffset = _randomizePositionOnDirectionChange ? Random.Range(-_topHorizontalRange, _topHorizontalRange) : 0f;

        return new Vector2(
            _iconAnchoredPosition.x + horizontalOffset,
            _iconAnchoredPosition.y + GetUpOffset()
        );
    }

    private Vector2 GetBottomPullPosition()
    {
        float horizontalOffset = _randomizePositionOnDirectionChange ? Random.Range(-_topHorizontalRange, _topHorizontalRange) : 0f;

        return new Vector2(
            _iconAnchoredPosition.x + horizontalOffset,
            _iconAnchoredPosition.y - GetDownOffset()
        );
    }

    private float GetHorizontalOffset()
    {
        if (!_useCanvasRelativeOffsets)
            return _horizontalPullOffset;

        RectTransform parentRect = GetParentRect();
        return parentRect != null ? parentRect.rect.width * _horizontalCanvasOffset : _horizontalPullOffset;
    }

    private float GetSideVerticalOffset()
    {
        if (!_useCanvasRelativeOffsets)
            return _sideVerticalOffset * _sideButtonHeightMultiplier;

        RectTransform parentRect = GetParentRect();
        return parentRect != null
            ? parentRect.rect.height * _sideVerticalCanvasOffset * _sideButtonHeightMultiplier
            : _sideVerticalOffset * _sideButtonHeightMultiplier;
    }

    private float GetUpOffset()
    {
        if (!_useCanvasRelativeOffsets)
            return _upPullOffset;

        RectTransform parentRect = GetParentRect();
        return parentRect != null ? parentRect.rect.height * _upCanvasOffset : _upPullOffset;
    }

    private float GetDownOffset()
    {
        if (!_useCanvasRelativeOffsets)
            return _downPullOffset;

        RectTransform parentRect = GetParentRect();
        return parentRect != null ? parentRect.rect.height * _downCanvasOffset : _downPullOffset;
    }

    private Vector2 ClampToParent(Vector2 _position)
    {
        if (!_clampInsideParent)
            return _position;

        RectTransform parentRect = GetParentRect();

        if (parentRect == null)
            return _position;

        Rect rect = parentRect.rect;
        float minX = rect.xMin + _parentPadding.x;
        float maxX = rect.xMax - _parentPadding.x;
        float minY = rect.yMin + _parentPadding.y;
        float maxY = rect.yMax - _parentPadding.y;

        if (minX > maxX)
            minX = maxX = 0f;

        if (minY > maxY)
            minY = maxY = 0f;

        return new Vector2(
            Mathf.Clamp(_position.x, minX, maxX),
            Mathf.Clamp(_position.y, minY, maxY)
        );
    }

    private RectTransform GetParentRect()
    {
        if (_directionIconRect != null && _directionIconRect.parent is RectTransform iconParentRect)
            return iconParentRect;

        return transform as RectTransform;
    }

    private void SetVisible(bool _visible)
    {
        if (_directionIcon != null)
            _directionIcon.gameObject.SetActive(_visible);

        if (!_visible && _directionIconRect != null)
            _directionIconRect.localScale = Vector3.one;

        if (!_visible)
        {
            _hasIconTargetPosition = false;
            _lastPromptId = -1;
        }
    }

    private void RefreshRuntimePosition()
    {
        _hasIconTargetPosition = false;
        _lastPromptId = -1;
        RefreshDisplay();
    }

    [ContextMenu("Log Current Direction Pull UI Settings")]
    private void LogCurrentSettings()
    {
        Debug.Log(
            $"DirectionPullUI settings - Icon Anchored Position: {_iconAnchoredPosition}, " +
            $"Horizontal Pull Offset: {_horizontalPullOffset}, Up Pull Offset: {_upPullOffset}, " +
            $"Down Pull Offset: {_downPullOffset}, Side Vertical Offset: {_sideVerticalOffset}, " +
            $"Side Button Height Multiplier: {_sideButtonHeightMultiplier}, " +
            $"Use Canvas Relative Offsets: {_useCanvasRelativeOffsets}, Horizontal Canvas Offset: {_horizontalCanvasOffset}, " +
            $"Up Canvas Offset: {_upCanvasOffset}, Down Canvas Offset: {_downCanvasOffset}"
        );
    }

    private Image CreateDirectionIcon()
    {
        GameObject iconObject = new GameObject("DirectionPullIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObject.transform.SetParent(transform, false);

        RectTransform rectTransform = iconObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = _iconAnchoredPosition;
        rectTransform.sizeDelta = _iconSize;

        Image iconImage = iconObject.GetComponent<Image>();
        iconImage.raycastTarget = false;
        iconImage.preserveAspect = true;
        iconImage.enabled = true;

        return iconImage;
    }
}
