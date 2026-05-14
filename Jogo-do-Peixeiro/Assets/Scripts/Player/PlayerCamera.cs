using UnityEngine;

public class PlayerCamera : MonoBehaviour
{
    private static int cameraLockCount;

    [SerializeField] private Transform target;
    [SerializeField] private Transform playerTransform;

    [Header("Offset")]
    [SerializeField] private float distance = 3f;
    [SerializeField] private float minDistance = 1f;
    [SerializeField] private float maxDistance = 4f;
    [SerializeField] private float height = 1f;

    [Header("Sensitivity")]
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float controllerSensitivity = 90f;
    [SerializeField] private float controllerDeadZone = 0.12f;
    [SerializeField] private float controllerResponseExponent = 1.25f;

    [Header("Clamp")]
    [SerializeField] private float minPitch = -25f;
    [SerializeField] private float maxPitch = 60f;

    [Header("Follow")]
    [SerializeField] private float followSpeed = 10f;

    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 30f;

    [Header("Dialog Focus")]
    [SerializeField] private bool focusDuringDialog = true;
    [SerializeField, Min(0.01f)] private float dialogFocusSpeed = 8f;

    private float yaw;
    private float pitch;
    private bool isDialogFocusActive;
    private DialogCameraFocusTarget dialogFocusTarget;

    public static bool IsCameraLocked => cameraLockCount > 0;

    public static void PushCameraLock()
    {
        cameraLockCount++;
    }

    public static void PopCameraLock()
    {
        cameraLockCount = Mathf.Max(0, cameraLockCount - 1);
    }

    private void Start()
    {
        SyncAnglesFromTransform();

        LoadSensitivity();
    }

    private void OnEnable()
    {
        TextCanvaManager.DialogStarted += EnterDialogFocus;
        TextCanvaManager.DialogFinished += ExitDialogFocus;
    }

    private void OnDisable()
    {
        TextCanvaManager.DialogStarted -= EnterDialogFocus;
        TextCanvaManager.DialogFinished -= ExitDialogFocus;
    }

    private void LateUpdate()
    {
        if (target == null || playerTransform == null)
            return;

        if (isDialogFocusActive)
        {
            UpdateDialogFocusCamera();
            return;
        }

        if (InputHandler.instance == null)
            return;

        if (IsCameraLocked)
            return;

        if (GameManager.instance != null)
        {
            if (GameManager.instance.currentState == GameManager.GameState.InUI ||
                GameManager.instance.currentState == GameManager.GameState.Paused)
                return;
        }

        Vector2 lookInput = InputHandler.instance.lookInput;

        Vector2 cameraInput = GetProcessedLookInput(lookInput, out bool isControllerInput);
        float sensitivity = isControllerInput ? controllerSensitivity * Time.deltaTime : mouseSensitivity;

        yaw += cameraInput.x * sensitivity;
        pitch -= cameraInput.y * sensitivity;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        float scroll = InputHandler.instance.zoomInput;
        if (Mathf.Abs(scroll) > 0.01f)
        {
            distance -= scroll * zoomSpeed * Time.deltaTime;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);

        Vector3 targetPosition = target.position + Vector3.up * height;
        Vector3 desiredPosition = targetPosition + rotation * new Vector3(0f, 0f, -distance);

        transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
        transform.LookAt(targetPosition);
    }

    public void SetSensitivity(float _value)
    {
        mouseSensitivity = _value;
    }

    public void SetSensitivity(float _mouseSensitivity, float _controllerSensitivity)
    {
        mouseSensitivity = _mouseSensitivity;
        controllerSensitivity = _controllerSensitivity;
    }

    public float GetSensitivity()
    {
        return mouseSensitivity;
    }

    public void LoadSensitivity()
    {
        float savedMouseSensitivity = PlayerPrefs.GetFloat("CameraMouseSensitivity", PlayerPrefs.GetFloat("CameraSensitivity", mouseSensitivity));
        float savedControllerSensitivity = PlayerPrefs.GetFloat("CameraControllerSensitivity", controllerSensitivity);
        SetSensitivity(savedMouseSensitivity, savedControllerSensitivity);
    }

    private Vector2 GetProcessedLookInput(Vector2 _lookInput, out bool _isControllerInput)
    {
        _isControllerInput = InputDeviceDetector.CurrentDeviceType == InputDeviceType.GenericController;

        if (!_isControllerInput)
            return _lookInput;

        float magnitude = _lookInput.magnitude;

        if (magnitude <= controllerDeadZone)
            return Vector2.zero;

        Vector2 direction = _lookInput / magnitude;
        float normalizedMagnitude = Mathf.InverseLerp(controllerDeadZone, 1f, Mathf.Clamp01(magnitude));
        float curvedMagnitude = Mathf.Pow(normalizedMagnitude, controllerResponseExponent);

        return direction * curvedMagnitude;
    }

    private void EnterDialogFocus(DialogCameraFocusTarget _focusTarget)
    {
        if (!focusDuringDialog)
            return;

        dialogFocusTarget = _focusTarget;
        isDialogFocusActive = true;
        SyncAnglesFromTransform();
    }

    private void ExitDialogFocus()
    {
        if (!isDialogFocusActive)
            return;

        isDialogFocusActive = false;
        dialogFocusTarget = null;
        SyncAnglesFromTransform();
    }

    private void UpdateDialogFocusCamera()
    {
        Vector3 cameraPivot = target.position + Vector3.up * height;
        float targetYaw = yaw;
        float targetPitch = pitch;
        float transitionSpeed = dialogFocusTarget != null
            ? dialogFocusTarget.TransitionSpeed
            : dialogFocusSpeed;

        if (dialogFocusTarget != null)
        {
            if (dialogFocusTarget.UseFixedAngles)
            {
                targetYaw = dialogFocusTarget.FixedYaw;
                targetPitch = Mathf.Clamp(dialogFocusTarget.FixedPitch, minPitch, maxPitch);
            }
            else
            {
                GetFocusAngles(cameraPivot, dialogFocusTarget.FocusPosition, out targetYaw, out targetPitch);
            }
        }

        float followT = 1f - Mathf.Exp(-transitionSpeed * Time.unscaledDeltaTime);
        yaw = Mathf.LerpAngle(yaw, targetYaw, followT);
        pitch = Mathf.Lerp(pitch, targetPitch, followT);
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Quaternion orbitRotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPosition = cameraPivot + orbitRotation * new Vector3(0f, 0f, -distance);

        transform.position = Vector3.Lerp(transform.position, desiredPosition, followT);

        if (dialogFocusTarget != null && !dialogFocusTarget.UseFixedAngles)
        {
            Vector3 lookDirection = dialogFocusTarget.FocusPosition - transform.position;

            if (lookDirection.sqrMagnitude > 0.001f)
            {
                Quaternion lookRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, followT);
            }

            return;
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, orbitRotation, followT);
    }

    private void GetFocusAngles(Vector3 _cameraPivot, Vector3 _focusPosition, out float _targetYaw, out float _targetPitch)
    {
        Vector3 direction = _focusPosition - _cameraPivot;

        if (direction.sqrMagnitude <= 0.001f)
        {
            _targetYaw = yaw;
            _targetPitch = pitch;
            return;
        }

        Vector2 horizontalDirection = new Vector2(direction.x, direction.z);
        float horizontalMagnitude = Mathf.Max(0.001f, horizontalDirection.magnitude);

        _targetYaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
        _targetPitch = -Mathf.Atan2(direction.y, horizontalMagnitude) * Mathf.Rad2Deg;
        _targetPitch = Mathf.Clamp(_targetPitch, minPitch, maxPitch);
    }

    private void SyncAnglesFromTransform()
    {
        Vector3 currentRotation = transform.eulerAngles;
        yaw = currentRotation.y;
        pitch = currentRotation.x;

        if (pitch > 180f)
            pitch -= 360f;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }
}
