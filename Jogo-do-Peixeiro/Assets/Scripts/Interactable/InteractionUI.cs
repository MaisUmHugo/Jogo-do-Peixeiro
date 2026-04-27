using UnityEngine;
using UnityEngine.UI;

public class InteractionUI : MonoBehaviour
{
    private enum PromptDisplayMode
    {
        WorldPoint,
        ScreenCenter
    }

    [SerializeField] private RectTransform interactButton;
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2f, 0f);
    [SerializeField] private float maxDistance = 4f;

    [Header("Screen Position")]
    [SerializeField] private PromptDisplayMode displayMode = PromptDisplayMode.WorldPoint;
    [SerializeField] private Vector2 screenCenterOffset = new Vector2(0f, -80f);
    [SerializeField] private float followSpeed = 18f;

    [Header("Input Icon")]
    [SerializeField] private InputIconDatabase _iconDatabase;
    [SerializeField] private Image _interactIconImage;
    [SerializeField] private InputIconAction _interactAction = InputIconAction.Interact;

    private Camera mainCamera;
    private Transform target;
    private Transform promptPoint;
    private Transform playerTransform;
    private InputDeviceType _lastDeviceType;

    private void Awake()
    {
        mainCamera = Camera.main;

        if (_interactIconImage == null && interactButton != null)
            _interactIconImage = interactButton.GetComponentInChildren<Image>();

        UpdateIcon(true);
        Hide();
    }

    private void OnEnable()
    {
        InputDeviceDetector.DeviceTypeChanged += HandleDeviceTypeChanged;
    }

    private void OnDisable()
    {
        InputDeviceDetector.DeviceTypeChanged -= HandleDeviceTypeChanged;
    }

    private void Update()
    {
        if (interactButton == null)
            return;

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (GameManager.instance != null &&
            (GameManager.instance.currentState == GameManager.GameState.Fishing ||
             GameManager.instance.IsGameplayBlocked()))
        {
            SetButtonVisible(false);
            return;
        }

        if (target == null || mainCamera == null || playerTransform == null)
        {
            SetButtonVisible(false);
            return;
        }

        Vector3 worldPosition = promptPoint != null ? promptPoint.position : target.position + worldOffset;
        float distanceToTarget = Vector3.Distance(playerTransform.position, worldPosition);

        if (distanceToTarget > maxDistance)
        {
            SetButtonVisible(false);
            return;
        }

        Vector3 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);

        if (screenPosition.z <= 0f)
        {
            SetButtonVisible(false);
            return;
        }

        SetButtonVisible(true);
        Vector3 targetScreenPosition = GetTargetScreenPosition(screenPosition);
        interactButton.position = Vector3.Lerp(
            interactButton.position,
            targetScreenPosition,
            Time.unscaledDeltaTime * followSpeed
        );
    }

    public void Show(Transform _target, Transform _playerTransform, Transform _promptPoint = null)
    {
        target = _target;
        playerTransform = _playerTransform;
        promptPoint = _promptPoint;

        UpdateIcon(false);
        SetButtonVisible(true);
    }

    private void HandleDeviceTypeChanged(InputDeviceType _deviceType)
    {
        UpdateIcon(true);
    }

    public void Hide()
    {
        target = null;
        playerTransform = null;
        promptPoint = null;

        SetButtonVisible(false);
    }

    private void UpdateIcon(bool _force)
    {
        if (_iconDatabase == null || _interactIconImage == null)
            return;

        InputDeviceType currentDeviceType = InputDeviceDetector.CurrentDeviceType;

        if (!_force && _lastDeviceType == currentDeviceType)
            return;

        _lastDeviceType = currentDeviceType;

        Sprite icon = _iconDatabase.GetIcon(currentDeviceType, _interactAction);

        _interactIconImage.enabled = icon != null;

        if (icon == null)
            return;

        _interactIconImage.sprite = icon;
        _interactIconImage.color = Color.white;
    }

    private void SetButtonVisible(bool _visible)
    {
        if (interactButton != null)
            interactButton.gameObject.SetActive(_visible);
    }

    private Vector3 GetTargetScreenPosition(Vector3 _worldScreenPosition)
    {
        if (displayMode == PromptDisplayMode.WorldPoint)
            return _worldScreenPosition;

        return new Vector3(
            (Screen.width * 0.5f) + screenCenterOffset.x,
            (Screen.height * 0.5f) + screenCenterOffset.y,
            0f
        );
    }
}
