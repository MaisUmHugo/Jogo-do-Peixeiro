using UnityEngine;
using Unity.Cinemachine;

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

    [Header("Cinemachine")]
    [SerializeField] private bool useCinemachine = true;
    [SerializeField] private CinemachineBrain cinemachineBrain;
    [SerializeField] private CinemachineCamera gameplayVirtualCamera;
    [SerializeField] private bool autoCreateGameplayVirtualCamera = true;
    [SerializeField] private bool manualUpdateCinemachine = true;
    [SerializeField] private string gameplayVirtualCameraName = "CM Gameplay Camera";
    [SerializeField] private int gameplayCameraPriority = 10;
    [SerializeField] private bool syncLensFromMainCamera = true;
    [SerializeField] private bool cutBlendWhenSwitchingDialogCameras = true;

    [Header("Scripted Dialog Camera")]
    [SerializeField] private bool useFreeCameraForScriptedDialogs = true;
    [SerializeField] private CinemachineCamera scriptedDialogVirtualCamera;
    [SerializeField] private bool autoCreateScriptedDialogVirtualCamera = true;
    [SerializeField] private string scriptedDialogVirtualCameraName = "CM Scripted Dialog Camera";
    [SerializeField] private int scriptedDialogCameraPriority = 40;
    [SerializeField] private int inactiveScriptedDialogCameraPriority = -10;
    [SerializeField, Min(0.01f)] private float defaultScriptedDialogCameraDistance = 6f;

    [Header("Final Dialog Camera")]
    [SerializeField] private bool useFreeCameraForFinalDialog = true;
    [SerializeField] private CinemachineCamera finalDialogVirtualCamera;
    [SerializeField] private bool autoCreateFinalDialogVirtualCamera = true;
    [SerializeField] private string finalDialogVirtualCameraName = "CM Final Dialog Camera";
    [SerializeField] private int finalDialogCameraPriority = 45;
    [SerializeField] private int inactiveFinalDialogCameraPriority = -15;
    [SerializeField, Min(0.01f)] private float defaultFinalDialogCameraDistance = 6f;

    private float yaw;
    private float pitch;
    private bool isDialogFocusActive;
    private DialogCameraFocusTarget dialogFocusTarget;
    private Camera sourceCamera;
    private bool isCutsceneCameraMode;
    private bool storedManualUpdateCinemachine;
    private bool hasStoredBrainUpdateMethods;
    private CinemachineBrain.UpdateMethods storedBrainUpdateMethod;
    private CinemachineBrain.BrainUpdateMethods storedBrainBlendUpdateMethod;
    private bool hasStoredGameplayCameraState;
    private bool hasDefaultGameplayDistance;
    private float defaultGameplayDistance;
    private float storedYaw;
    private float storedPitch;
    private float storedDistance;
    private Vector3 storedGameplayCameraPosition;
    private Quaternion storedGameplayCameraRotation;
    private LensSettings defaultGameplayCameraLens;
    private LensSettings storedGameplayCameraLens;
    private bool hasDefaultGameplayCameraLens;
    private bool hasStoredGameplayCameraLens;
    private bool isScriptedDialogCameraMode;
    private bool hasStoredScriptedDialogCameraState;
    private bool storedScriptedDialogCameraEnabled;
    private int storedScriptedDialogCameraPriority;
    private bool hasActiveScriptedDialogCameraPose;
    private float activeScriptedDialogYaw;
    private float activeScriptedDialogPitch;
    private float activeScriptedDialogDistance;
    private bool isFinalDialogCameraMode;
    private bool hasStoredFinalDialogCameraState;
    private bool storedFinalDialogCameraEnabled;
    private int storedFinalDialogCameraPriority;
    private bool hasActiveFinalDialogCameraPose;
    private float activeFinalDialogYaw;
    private float activeFinalDialogPitch;
    private float activeFinalDialogDistance;

    public static bool IsCameraLocked => cameraLockCount > 0;

    public static PlayerCamera Instance { get; private set; }

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
        Instance = this;
        CacheDefaultGameplayDistance();
        EnsureCinemachineSetup();
        SyncAnglesFromTransform();

        LoadSensitivity();
    }

    private void Awake()
    {
        Instance = this;
        CacheDefaultGameplayDistance();
    }

    private void OnEnable()
    {
        TextCanvaManager.DialogStarted += EnterDialogFocus;
        TextCanvaManager.DialogFinished += ExitDialogFocus;
        TextCanvaManager.DialogCameraFocusRequested += EnterDialogFocus;
        TextCanvaManager.DialogCameraFocusCleared += ExitDialogFocus;
    }

    private void OnDisable()
    {
        if (Instance == this)
            Instance = null;

        EndFinalDialogCameraPose(false);
        EndScriptedDialogCameraPose(false);
        EndCutsceneCameraMode();

        TextCanvaManager.DialogStarted -= EnterDialogFocus;
        TextCanvaManager.DialogFinished -= ExitDialogFocus;
        TextCanvaManager.DialogCameraFocusRequested -= EnterDialogFocus;
        TextCanvaManager.DialogCameraFocusCleared -= ExitDialogFocus;
    }

    private void LateUpdate()
    {
        EnsureCinemachineSetup();

        if (target == null || playerTransform == null)
        {
            UpdateCinemachineBrain();
            return;
        }

        if (isCutsceneCameraMode)
            return;

        if (isFinalDialogCameraMode)
        {
            UpdateFinalDialogCameraPose();
            UpdateCinemachineBrain();
            return;
        }

        if (isScriptedDialogCameraMode)
        {
            UpdateScriptedDialogCameraPose();
            UpdateCinemachineBrain();
            return;
        }

        if (isDialogFocusActive)
        {
            UpdateDialogFocusCamera();
            UpdateCinemachineBrain();
            return;
        }

        if (InputHandler.instance == null)
        {
            UpdateCinemachineBrain();
            return;
        }

        if (IsCameraLocked)
        {
            UpdateCinemachineBrain();
            return;
        }

        if (GameManager.instance != null)
        {
            if (GameManager.instance.currentState == GameManager.GameState.InUI ||
                GameManager.instance.currentState == GameManager.GameState.Paused)
            {
                UpdateCinemachineBrain();
                return;
            }
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
        Transform cameraTransform = GetCameraTransform();

        Vector3 cameraPosition = Vector3.Lerp(cameraTransform.position, desiredPosition, followSpeed * Time.deltaTime);
        Quaternion cameraRotation = GetLookRotation(targetPosition - cameraPosition, rotation);

        SetCameraPose(cameraPosition, cameraRotation);
        UpdateCinemachineBrain();
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

    public void BeginCutsceneCameraMode()
    {
        EnsureCinemachineSetup();
        EndFinalDialogCameraPose(false);
        EndScriptedDialogCameraPose(false);

        if (isCutsceneCameraMode)
            return;

        isCutsceneCameraMode = true;
        storedManualUpdateCinemachine = manualUpdateCinemachine;
        StoreGameplayCameraState();
        manualUpdateCinemachine = false;

        if (cinemachineBrain != null)
        {
            storedBrainUpdateMethod = cinemachineBrain.UpdateMethod;
            storedBrainBlendUpdateMethod = cinemachineBrain.BlendUpdateMethod;
            hasStoredBrainUpdateMethods = true;

            cinemachineBrain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
            cinemachineBrain.BlendUpdateMethod = CinemachineBrain.BrainUpdateMethods.LateUpdate;
        }
    }

    public void EndCutsceneCameraMode()
    {
        if (!isCutsceneCameraMode)
            return;

        isCutsceneCameraMode = false;
        manualUpdateCinemachine = storedManualUpdateCinemachine;

        if (cinemachineBrain != null && hasStoredBrainUpdateMethods)
        {
            cinemachineBrain.UpdateMethod = storedBrainUpdateMethod;
            cinemachineBrain.BlendUpdateMethod = storedBrainBlendUpdateMethod;
        }

        hasStoredBrainUpdateMethods = false;
        RestoreGameplayCameraState();
        UpdateCinemachineBrain();
    }

    public void RestoreGameplayCameraAfterCutscene()
    {
        EnsureCinemachineSetup();
        EndFinalDialogCameraPose(false);
        EndScriptedDialogCameraPose(false);

        if (isCutsceneCameraMode)
        {
            EndCutsceneCameraMode();
            return;
        }

        RestoreGameplayCameraLens();

        if (gameplayVirtualCamera != null)
        {
            gameplayVirtualCamera.enabled = true;
            gameplayVirtualCamera.Priority = gameplayCameraPriority;
            UpdateGameplayVirtualCameraTargets();
        }

        UpdateCinemachineBrain();
    }

    public void RestoreDefaultGameplayCameraInstant()
    {
        RestoreDefaultGameplayCameraInstant(yaw, pitch);
    }

    public void RestoreDefaultGameplayCameraInstant(float _yaw, float _pitch)
    {
        EnsureCinemachineSetup();
        EndFinalDialogCameraPose(false);
        EndScriptedDialogCameraPose(false);

        if (isCutsceneCameraMode)
            EndCutsceneCameraMode();

        RestoreGameplayCameraLens();

        if (gameplayVirtualCamera != null)
        {
            gameplayVirtualCamera.enabled = true;
            gameplayVirtualCamera.Priority = gameplayCameraPriority;
            UpdateGameplayVirtualCameraTargets();
        }

        SnapToGameplayTarget(_yaw, _pitch, GetDefaultGameplayDistance());
        UpdateCinemachineBrainAfterDialogCameraSwitch();
    }

    public void SnapToGameplayTarget()
    {
        EnsureCinemachineSetup();

        if (target == null)
            return;

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 targetPosition = target.position + Vector3.up * height;
        Vector3 cameraPosition = targetPosition + rotation * new Vector3(0f, 0f, -distance);
        Quaternion cameraRotation = GetLookRotation(targetPosition - cameraPosition, rotation);

        SetCameraPose(cameraPosition, cameraRotation);
        UpdateCinemachineBrain();
    }

    public void SnapToGameplayTarget(float _yaw, float _pitch, float _distance)
    {
        yaw = _yaw;
        pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

        if (_distance > 0f)
            distance = Mathf.Clamp(_distance, minDistance, maxDistance);

        SnapToGameplayTarget();
    }

    public bool TryShowScriptedDialogCameraPose(float _yaw, float _pitch, float _distance)
    {
        if (!useCinemachine || !useFreeCameraForScriptedDialogs)
            return false;

        EndFinalDialogCameraPose(false);
        EnsureCinemachineSetup();
        EnsureScriptedDialogVirtualCamera();

        if (scriptedDialogVirtualCamera == null || target == null)
            return false;

        activeScriptedDialogYaw = _yaw;
        activeScriptedDialogPitch = _pitch;
        activeScriptedDialogDistance = _distance;
        hasActiveScriptedDialogCameraPose = true;

        BeginScriptedDialogCameraPose();
        UpdateScriptedDialogCameraPose();
        UpdateCinemachineBrainAfterDialogCameraSwitch();
        return true;
    }

    public void EndScriptedDialogCameraPose(bool _snapGameplayCamera = true)
    {
        if (!isScriptedDialogCameraMode)
            return;

        isScriptedDialogCameraMode = false;

        if (scriptedDialogVirtualCamera != null)
        {
            if (hasStoredScriptedDialogCameraState)
            {
                scriptedDialogVirtualCamera.enabled = storedScriptedDialogCameraEnabled;
                scriptedDialogVirtualCamera.Priority = storedScriptedDialogCameraPriority;
            }
            else
            {
                scriptedDialogVirtualCamera.Priority = inactiveScriptedDialogCameraPriority;
                scriptedDialogVirtualCamera.enabled = false;
            }
        }

        hasStoredScriptedDialogCameraState = false;
        hasActiveScriptedDialogCameraPose = false;

        if (gameplayVirtualCamera != null)
        {
            gameplayVirtualCamera.enabled = true;
            gameplayVirtualCamera.Priority = gameplayCameraPriority;
        }

        if (_snapGameplayCamera)
            SnapToGameplayTarget();
        else
            UpdateCinemachineBrain();
    }

    public bool TryShowFinalDialogCameraPose(float _yaw, float _pitch, float _distance)
    {
        if (!useCinemachine || !useFreeCameraForFinalDialog)
            return false;

        EndScriptedDialogCameraPose(false);
        EnsureCinemachineSetup();
        EnsureFinalDialogVirtualCamera();

        if (finalDialogVirtualCamera == null || target == null)
            return false;

        activeFinalDialogYaw = _yaw;
        activeFinalDialogPitch = _pitch;
        activeFinalDialogDistance = _distance;
        hasActiveFinalDialogCameraPose = true;

        BeginFinalDialogCameraPose();
        UpdateFinalDialogCameraPose();
        UpdateCinemachineBrainAfterDialogCameraSwitch();
        return true;
    }

    public void EndFinalDialogCameraPose(bool _snapGameplayCamera = true)
    {
        if (!isFinalDialogCameraMode)
            return;

        isFinalDialogCameraMode = false;

        if (finalDialogVirtualCamera != null)
        {
            if (hasStoredFinalDialogCameraState)
            {
                finalDialogVirtualCamera.enabled = storedFinalDialogCameraEnabled;
                finalDialogVirtualCamera.Priority = storedFinalDialogCameraPriority;
            }
            else
            {
                finalDialogVirtualCamera.Priority = inactiveFinalDialogCameraPriority;
                finalDialogVirtualCamera.enabled = false;
            }
        }

        hasStoredFinalDialogCameraState = false;
        hasActiveFinalDialogCameraPose = false;

        if (gameplayVirtualCamera != null)
        {
            gameplayVirtualCamera.enabled = true;
            gameplayVirtualCamera.Priority = gameplayCameraPriority;
        }

        if (_snapGameplayCamera)
            SnapToGameplayTarget();
        else
            UpdateCinemachineBrain();
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
        Transform cameraTransform = GetCameraTransform();

        Vector3 cameraPosition = Vector3.Lerp(cameraTransform.position, desiredPosition, followT);
        Quaternion cameraRotation = orbitRotation;

        if (dialogFocusTarget != null && !dialogFocusTarget.UseFixedAngles)
        {
            Vector3 lookDirection = dialogFocusTarget.FocusPosition - cameraPosition;

            if (lookDirection.sqrMagnitude > 0.001f)
            {
                Quaternion lookRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
                cameraRotation = Quaternion.Slerp(cameraTransform.rotation, lookRotation, followT);
            }

            SetCameraPose(cameraPosition, cameraRotation);
            return;
        }

        cameraRotation = Quaternion.Slerp(cameraTransform.rotation, orbitRotation, followT);
        SetCameraPose(cameraPosition, cameraRotation);
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
        Transform cameraTransform = GetCameraTransform();
        Vector3 currentRotation = cameraTransform.eulerAngles;
        yaw = currentRotation.y;
        pitch = currentRotation.x;

        if (pitch > 180f)
            pitch -= 360f;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
    }

    private void StoreGameplayCameraState()
    {
        Transform cameraTransform = GetCameraTransform();

        storedYaw = yaw;
        storedPitch = pitch;
        storedDistance = distance;
        storedGameplayCameraPosition = cameraTransform.position;
        storedGameplayCameraRotation = cameraTransform.rotation;
        hasStoredGameplayCameraLens = false;

        if (gameplayVirtualCamera != null)
        {
            storedGameplayCameraLens = gameplayVirtualCamera.Lens;
            hasStoredGameplayCameraLens = true;
        }

        hasStoredGameplayCameraState = true;
    }

    private void CacheDefaultGameplayDistance()
    {
        if (hasDefaultGameplayDistance)
            return;

        defaultGameplayDistance = Mathf.Clamp(distance, minDistance, maxDistance);
        hasDefaultGameplayDistance = true;
    }

    private float GetDefaultGameplayDistance()
    {
        if (!hasDefaultGameplayDistance)
            CacheDefaultGameplayDistance();

        return Mathf.Clamp(defaultGameplayDistance, minDistance, maxDistance);
    }

    private void RestoreGameplayCameraState()
    {
        if (!hasStoredGameplayCameraState)
        {
            SyncAnglesFromTransform();
            return;
        }

        yaw = storedYaw;
        pitch = Mathf.Clamp(storedPitch, minPitch, maxPitch);
        distance = Mathf.Clamp(storedDistance, minDistance, maxDistance);
        SetCameraPose(storedGameplayCameraPosition, storedGameplayCameraRotation);
        RestoreGameplayCameraLens();
        hasStoredGameplayCameraState = false;
    }

    private Transform GetCameraTransform()
    {
        if (useCinemachine && gameplayVirtualCamera != null)
            return gameplayVirtualCamera.transform;

        return transform;
    }

    private Quaternion GetLookRotation(Vector3 _direction, Quaternion _fallbackRotation)
    {
        if (_direction.sqrMagnitude <= 0.001f)
            return _fallbackRotation;

        return Quaternion.LookRotation(_direction.normalized, Vector3.up);
    }

    private void SetCameraPose(Vector3 _position, Quaternion _rotation)
    {
        Transform cameraTransform = GetCameraTransform();
        cameraTransform.SetPositionAndRotation(_position, _rotation);

        if (useCinemachine && gameplayVirtualCamera != null)
            gameplayVirtualCamera.ForceCameraPosition(_position, _rotation);
    }

    private void EnsureCinemachineSetup()
    {
        if (!useCinemachine)
            return;

        if (sourceCamera == null)
        {
            sourceCamera = GetComponent<Camera>();

            if (sourceCamera == null)
                sourceCamera = Camera.main;
        }

        if (sourceCamera != null && cinemachineBrain == null)
            cinemachineBrain = sourceCamera.GetComponent<CinemachineBrain>();

        if (sourceCamera != null && cinemachineBrain == null)
            cinemachineBrain = sourceCamera.gameObject.AddComponent<CinemachineBrain>();

        if (cinemachineBrain != null && manualUpdateCinemachine)
        {
            cinemachineBrain.UpdateMethod = CinemachineBrain.UpdateMethods.ManualUpdate;
            cinemachineBrain.BlendUpdateMethod = CinemachineBrain.BrainUpdateMethods.LateUpdate;
        }

        if (gameplayVirtualCamera == null && autoCreateGameplayVirtualCamera)
            gameplayVirtualCamera = FindGameplayVirtualCamera();

        if (gameplayVirtualCamera == null && autoCreateGameplayVirtualCamera)
            gameplayVirtualCamera = CreateGameplayVirtualCamera();

        if (gameplayVirtualCamera == null)
            return;

        gameplayVirtualCamera.Priority = gameplayCameraPriority;
        UpdateGameplayVirtualCameraTargets();

        if (!hasDefaultGameplayCameraLens)
            CacheDefaultGameplayCameraLens();
    }

    private void CacheDefaultGameplayCameraLens()
    {
        if (gameplayVirtualCamera == null)
            return;

        if (syncLensFromMainCamera && sourceCamera != null)
            gameplayVirtualCamera.Lens = LensSettings.FromCamera(sourceCamera);

        defaultGameplayCameraLens = gameplayVirtualCamera.Lens;
        hasDefaultGameplayCameraLens = true;
    }

    private void RestoreGameplayCameraLens()
    {
        if (gameplayVirtualCamera == null)
            return;

        if (hasStoredGameplayCameraLens)
        {
            gameplayVirtualCamera.Lens = storedGameplayCameraLens;
            hasStoredGameplayCameraLens = false;
            return;
        }

        if (hasDefaultGameplayCameraLens)
            gameplayVirtualCamera.Lens = defaultGameplayCameraLens;
    }

    private CinemachineCamera FindGameplayVirtualCamera()
    {
        CinemachineCamera[] cameras = FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != null && cameras[i].name == gameplayVirtualCameraName)
                return cameras[i];
        }

        return null;
    }

    private void BeginScriptedDialogCameraPose()
    {
        EndFinalDialogCameraPose(false);

        if (isScriptedDialogCameraMode)
            return;

        isScriptedDialogCameraMode = true;

        if (scriptedDialogVirtualCamera != null && !hasStoredScriptedDialogCameraState)
        {
            storedScriptedDialogCameraEnabled = scriptedDialogVirtualCamera.enabled;
            storedScriptedDialogCameraPriority = scriptedDialogVirtualCamera.Priority;
            hasStoredScriptedDialogCameraState = true;
        }

        if (gameplayVirtualCamera != null)
        {
            gameplayVirtualCamera.enabled = true;
            gameplayVirtualCamera.Priority = gameplayCameraPriority;
        }

        if (scriptedDialogVirtualCamera != null)
        {
            scriptedDialogVirtualCamera.enabled = true;
            scriptedDialogVirtualCamera.Priority = Mathf.Max(scriptedDialogCameraPriority, gameplayCameraPriority + 1);
        }
    }

    private void ApplyScriptedDialogCameraPose(float _yaw, float _pitch, float _distance)
    {
        float resolvedDistance = _distance > 0f ? _distance : defaultScriptedDialogCameraDistance;
        Vector3 targetPosition = target.position + Vector3.up * height;
        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 cameraPosition = targetPosition + rotation * new Vector3(0f, 0f, -resolvedDistance);
        Quaternion cameraRotation = GetLookRotation(targetPosition - cameraPosition, rotation);

        scriptedDialogVirtualCamera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
        scriptedDialogVirtualCamera.ForceCameraPosition(cameraPosition, cameraRotation);
    }

    private void UpdateScriptedDialogCameraPose()
    {
        if (!hasActiveScriptedDialogCameraPose || scriptedDialogVirtualCamera == null || target == null)
            return;

        ApplyScriptedDialogCameraPose(
            activeScriptedDialogYaw,
            activeScriptedDialogPitch,
            activeScriptedDialogDistance);
    }

    private void BeginFinalDialogCameraPose()
    {
        EndScriptedDialogCameraPose(false);

        if (isFinalDialogCameraMode)
            return;

        isFinalDialogCameraMode = true;

        if (finalDialogVirtualCamera != null && !hasStoredFinalDialogCameraState)
        {
            storedFinalDialogCameraEnabled = finalDialogVirtualCamera.enabled;
            storedFinalDialogCameraPriority = finalDialogVirtualCamera.Priority;
            hasStoredFinalDialogCameraState = true;
        }

        if (gameplayVirtualCamera != null)
        {
            gameplayVirtualCamera.enabled = true;
            gameplayVirtualCamera.Priority = gameplayCameraPriority;
        }

        if (finalDialogVirtualCamera != null)
        {
            finalDialogVirtualCamera.enabled = true;
            finalDialogVirtualCamera.Priority = Mathf.Max(finalDialogCameraPriority, gameplayCameraPriority + 1);
        }
    }

    private void ApplyFinalDialogCameraPose(float _yaw, float _pitch, float _distance)
    {
        float resolvedDistance = _distance > 0f ? _distance : defaultFinalDialogCameraDistance;
        Vector3 targetPosition = target.position + Vector3.up * height;
        Quaternion rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3 cameraPosition = targetPosition + rotation * new Vector3(0f, 0f, -resolvedDistance);
        Quaternion cameraRotation = GetLookRotation(targetPosition - cameraPosition, rotation);

        finalDialogVirtualCamera.transform.SetPositionAndRotation(cameraPosition, cameraRotation);
        finalDialogVirtualCamera.ForceCameraPosition(cameraPosition, cameraRotation);
    }

    private void UpdateFinalDialogCameraPose()
    {
        if (!hasActiveFinalDialogCameraPose || finalDialogVirtualCamera == null || target == null)
            return;

        ApplyFinalDialogCameraPose(
            activeFinalDialogYaw,
            activeFinalDialogPitch,
            activeFinalDialogDistance);
    }

    private void EnsureScriptedDialogVirtualCamera()
    {
        if (!useCinemachine || !useFreeCameraForScriptedDialogs)
            return;

        if (scriptedDialogVirtualCamera == null && autoCreateScriptedDialogVirtualCamera)
            scriptedDialogVirtualCamera = FindScriptedDialogVirtualCamera();

        if (scriptedDialogVirtualCamera == null && autoCreateScriptedDialogVirtualCamera)
            scriptedDialogVirtualCamera = CreateScriptedDialogVirtualCamera();

        if (scriptedDialogVirtualCamera == null)
            return;

        CameraTarget cameraTarget = scriptedDialogVirtualCamera.Target;
        cameraTarget.TrackingTarget = null;
        cameraTarget.LookAtTarget = null;
        cameraTarget.CustomLookAtTarget = false;
        scriptedDialogVirtualCamera.Target = cameraTarget;

        if (!isScriptedDialogCameraMode && scriptedDialogVirtualCamera.Priority > inactiveScriptedDialogCameraPriority)
            scriptedDialogVirtualCamera.Priority = inactiveScriptedDialogCameraPriority;
    }

    private void EnsureFinalDialogVirtualCamera()
    {
        if (!useCinemachine || !useFreeCameraForFinalDialog)
            return;

        if (finalDialogVirtualCamera == null && autoCreateFinalDialogVirtualCamera)
            finalDialogVirtualCamera = FindFinalDialogVirtualCamera();

        if (finalDialogVirtualCamera == null && autoCreateFinalDialogVirtualCamera)
            finalDialogVirtualCamera = CreateFinalDialogVirtualCamera();

        if (finalDialogVirtualCamera == null)
            return;

        CameraTarget cameraTarget = finalDialogVirtualCamera.Target;
        cameraTarget.TrackingTarget = null;
        cameraTarget.LookAtTarget = null;
        cameraTarget.CustomLookAtTarget = false;
        finalDialogVirtualCamera.Target = cameraTarget;

        if (!isFinalDialogCameraMode && finalDialogVirtualCamera.Priority > inactiveFinalDialogCameraPriority)
            finalDialogVirtualCamera.Priority = inactiveFinalDialogCameraPriority;
    }

    private CinemachineCamera FindScriptedDialogVirtualCamera()
    {
        CinemachineCamera[] cameras = FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != null && cameras[i].name == scriptedDialogVirtualCameraName)
                return cameras[i];
        }

        return null;
    }

    private CinemachineCamera FindFinalDialogVirtualCamera()
    {
        CinemachineCamera[] cameras = FindObjectsByType<CinemachineCamera>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < cameras.Length; i++)
        {
            if (cameras[i] != null && cameras[i].name == finalDialogVirtualCameraName)
                return cameras[i];
        }

        return null;
    }

    private CinemachineCamera CreateScriptedDialogVirtualCamera()
    {
        GameObject virtualCameraObject = new GameObject(string.IsNullOrWhiteSpace(scriptedDialogVirtualCameraName)
            ? "CM Scripted Dialog Camera"
            : scriptedDialogVirtualCameraName);

        Transform virtualCameraTransform = virtualCameraObject.transform;
        virtualCameraTransform.SetParent(transform.parent, true);
        virtualCameraTransform.SetPositionAndRotation(transform.position, transform.rotation);

        CinemachineCamera virtualCamera = virtualCameraObject.AddComponent<CinemachineCamera>();
        virtualCamera.Priority = inactiveScriptedDialogCameraPriority;
        virtualCamera.enabled = false;

        if (sourceCamera != null)
            virtualCamera.Lens = LensSettings.FromCamera(sourceCamera);

        virtualCamera.ForceCameraPosition(virtualCameraTransform.position, virtualCameraTransform.rotation);
        return virtualCamera;
    }

    private CinemachineCamera CreateFinalDialogVirtualCamera()
    {
        GameObject virtualCameraObject = new GameObject(string.IsNullOrWhiteSpace(finalDialogVirtualCameraName)
            ? "CM Final Dialog Camera"
            : finalDialogVirtualCameraName);

        Transform virtualCameraTransform = virtualCameraObject.transform;
        virtualCameraTransform.SetParent(transform.parent, true);
        virtualCameraTransform.SetPositionAndRotation(transform.position, transform.rotation);

        CinemachineCamera virtualCamera = virtualCameraObject.AddComponent<CinemachineCamera>();
        virtualCamera.Priority = inactiveFinalDialogCameraPriority;
        virtualCamera.enabled = false;

        if (sourceCamera != null)
            virtualCamera.Lens = LensSettings.FromCamera(sourceCamera);

        virtualCamera.ForceCameraPosition(virtualCameraTransform.position, virtualCameraTransform.rotation);
        return virtualCamera;
    }

    private CinemachineCamera CreateGameplayVirtualCamera()
    {
        GameObject virtualCameraObject = new GameObject(string.IsNullOrWhiteSpace(gameplayVirtualCameraName)
            ? "CM Gameplay Camera"
            : gameplayVirtualCameraName);

        Transform virtualCameraTransform = virtualCameraObject.transform;
        virtualCameraTransform.SetParent(transform.parent, true);
        virtualCameraTransform.SetPositionAndRotation(transform.position, transform.rotation);

        CinemachineCamera virtualCamera = virtualCameraObject.AddComponent<CinemachineCamera>();
        virtualCamera.Priority = gameplayCameraPriority;

        if (sourceCamera != null)
            virtualCamera.Lens = LensSettings.FromCamera(sourceCamera);

        virtualCamera.ForceCameraPosition(virtualCameraTransform.position, virtualCameraTransform.rotation);

        return virtualCamera;
    }

    private void UpdateGameplayVirtualCameraTargets()
    {
        if (gameplayVirtualCamera == null)
            return;

        CameraTarget cameraTarget = gameplayVirtualCamera.Target;
        cameraTarget.TrackingTarget = target;
        cameraTarget.LookAtTarget = target;
        cameraTarget.CustomLookAtTarget = target != null;
        gameplayVirtualCamera.Target = cameraTarget;
    }

    private void UpdateCinemachineBrain()
    {
        if (!useCinemachine || !manualUpdateCinemachine || cinemachineBrain == null)
            return;

        if (cinemachineBrain.UpdateMethod == CinemachineBrain.UpdateMethods.ManualUpdate)
            cinemachineBrain.ManualUpdate();
    }

    private void UpdateCinemachineBrainAfterDialogCameraSwitch()
    {
        if (!cutBlendWhenSwitchingDialogCameras || cinemachineBrain == null)
        {
            UpdateCinemachineBrain();
            return;
        }

        CinemachineBlendDefinition previousDefaultBlend = cinemachineBrain.DefaultBlend;
        cinemachineBrain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0f);
        cinemachineBrain.ActiveBlend = null;
        UpdateCinemachineBrain();
        cinemachineBrain.DefaultBlend = previousDefaultBlend;
    }
}
