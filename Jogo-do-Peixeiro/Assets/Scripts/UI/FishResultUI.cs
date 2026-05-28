using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class FishResultUI : MonoBehaviour
{
    private List<FishScriptableObject> fishList = new List<FishScriptableObject>();

    [Header("Result Text")]
    [SerializeField] private GameObject newFishText;
    [SerializeField] private TMP_Text newFishTintText;
    [SerializeField] private Color newFishTintColor = new Color(1f, 0.78f, 0.18f, 1f);
    [SerializeField] private float newFishTintSpeed = 5f;
    [SerializeField] private float newFishTintScale = 0.08f;
    [SerializeField, Range(0f, 1f)] private float newFishTintStrength = 0.65f;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private string emptyDescriptionMessage = "Sem descrição.";
    [SerializeField] private Vector2 generatedDescriptionOffset = new Vector2(0f, -150f);
    [SerializeField] private Vector2 generatedDescriptionSize = new Vector2(700f, 260f);
    [SerializeField] private float generatedDescriptionFontSize = 30f;

    [Header("Star Image")]
    [SerializeField] private Transform starImage;
    [SerializeField] private float rotationSpeed;
    private float starAngle;

    [Header("Fish Variables")]
    [SerializeField] private MeshFilter mesh;
    [SerializeField] private Renderer objectRenderer;
    [SerializeField] private GameObject[] fishStarsRateObjects;
    [SerializeField] private float fishRotationSpeed;
    [SerializeField] private float fishInputRotationSpeed = 180f;
    [SerializeField] private float fishMouseRotationSensitivity = 0.25f;
    [SerializeField, Range(0f, 1f)] private float fishControllerRotationDeadZone = 0.2f;
    [SerializeField] private float minFishPitchAngle = -45f;
    [SerializeField] private float maxFishPitchAngle = 45f;
    private float fishAngle;
    private float fishPitchAngle;

    [Header("Display")]
    [SerializeField] private float minDisplayTime = 1f;
    [SerializeField] private bool hideDayCycleHudWhileShowing = true;
    [SerializeField] private DayCycle dayCycle;

    [Header("Modal")]
    [SerializeField] private bool useModalManager = true;
    [SerializeField] private bool pauseTimeWhileShowing = true;
    [SerializeField] private bool hideHudWhileShowing = true;
    [SerializeField] private bool blockPauseWhileShowing = true;
    [SerializeField] private bool lockCameraWhileShowing = true;

    [Header("Rotate Hint")]
    [SerializeField] private TMP_Text rotateHintText;
    [SerializeField] private string mouseRotateHint = "Segure o botão esquerdo do mouse para girar o peixe.";
    [SerializeField] private string controllerRotateHint = "Use o analógico direito para girar o peixe.";

    [Header("Close Hint")]
    [SerializeField] private TMP_Text closeHintText;
    [SerializeField] private string closeHintMessage = "Aperte qualquer botão para fechar.";

    [Header("Hint Pulse")]
    [SerializeField] private float hintPulseSpeed = 4f;
    [SerializeField] private float hintPulseScale = 0.08f;
    [SerializeField, Range(0f, 1f)] private float hintMinimumAlpha = 0.65f;
    [SerializeField, Range(0f, 1f)] private float hintMaximumAlpha = 1f;

    private int fishRarity;
    private Mesh fishMesh;
    private Material fishMaterial;
    private FishScriptableObject currentFishType;
    private bool hasFishVisual;
    private bool isShowing;
    private bool canSkip;
    private bool isInputSubscribed;
    private bool hasCameraLock;
    private bool hasStoredDayCycleHudVisibility;
    private bool wasHourTextVisible;
    private bool wasDayTextVisible;
    private bool hasStoredFishPreviewPose;
    private bool ignoreFishRotationThisFrame;
    private bool hasRotatedFish;
    private bool hasStoredRotateHintState;
    private bool hasStoredCloseHintState;
    private bool hasStoredNewFishTextState;
    private bool isNewFishTextVisible;
    private Vector3 fishPreviewDefaultLocalPosition;
    private Quaternion fishPreviewDefaultLocalRotation;
    private Vector3 fishPreviewDefaultLocalEulerAngles;
    private Vector3 fishPreviewDefaultLocalScale;
    private Vector3 rotateHintDefaultScale = Vector3.one;
    private Vector3 closeHintDefaultScale = Vector3.one;
    private Vector3 newFishTextDefaultScale = Vector3.one;
    private float rotateHintDefaultAlpha = 1f;
    private float closeHintDefaultAlpha = 1f;
    private Color newFishTextDefaultColor = Color.white;
    private int modalToken = UIModalManager.InvalidToken;
    private Coroutine enableSkipCoroutine;

    public bool IsShowing => isShowing;
    public event System.Action Closed;

    private void OnValidate()
    {
        minDisplayTime = Mathf.Max(0f, minDisplayTime);
        fishInputRotationSpeed = Mathf.Max(0f, fishInputRotationSpeed);
        fishMouseRotationSensitivity = Mathf.Max(0f, fishMouseRotationSensitivity);
        fishControllerRotationDeadZone = Mathf.Clamp01(fishControllerRotationDeadZone);
        hintPulseSpeed = Mathf.Max(0f, hintPulseSpeed);
        hintPulseScale = Mathf.Max(0f, hintPulseScale);
        newFishTintSpeed = Mathf.Max(0f, newFishTintSpeed);
        newFishTintScale = Mathf.Max(0f, newFishTintScale);
        newFishTintStrength = Mathf.Clamp01(newFishTintStrength);
        generatedDescriptionFontSize = Mathf.Max(1f, generatedDescriptionFontSize);
        generatedDescriptionSize.x = Mathf.Max(1f, generatedDescriptionSize.x);
        generatedDescriptionSize.y = Mathf.Max(1f, generatedDescriptionSize.y);

        if (hintMaximumAlpha < hintMinimumAlpha)
            hintMaximumAlpha = hintMinimumAlpha;

        if (maxFishPitchAngle < minFishPitchAngle)
            maxFishPitchAngle = minFishPitchAngle;
    }

    private void Awake()
    {
        ResolveReferences();
        StoreFishPreviewDefaultPose();

        if (!isShowing)
            HideImmediate();
    }

    private void OnEnable()
    {
        ResolveReferences();
        TrySubscribeInput();
    }

    private void OnDisable()
    {
        CancelInvoke();

        if (enableSkipCoroutine != null)
        {
            StopCoroutine(enableSkipCoroutine);
            enableSkipCoroutine = null;
        }

        UnsubscribeInput();

        if (isShowing)
            NotifyClosed();
        else
        {
            PopModalState();
            RestoreDayCycleHud();
            ReleaseCameraLock();
        }
    }

    public void SetNewFish(FishData _fish)
    {
        if (_fish == null || _fish.typeOfFish == null)
            return;

        currentFishType = _fish.typeOfFish;
        fishMesh = currentFishType.mesh;
        fishMaterial = currentFishType.material;
        fishRarity = currentFishType.rarity;
        hasFishVisual = FishVisualUtility.HasVisual(currentFishType);

        ApplyFishModel();

        ShowRarityStars(fishRarity);
        ResetFishRotation();

        if (!fishList.Contains(_fish.typeOfFish))
        {

            fishList.Add(_fish.typeOfFish);

            SetNewFishTextVisible(true);

        }
        else
        {
            SetNewFishTextVisible(false);
        }
    }

    public void ShowCatchResult(FishData _fish)
    {
        if (_fish == null || _fish.typeOfFish == null)
            return;

        isShowing = true;
        canSkip = false;
        hasRotatedFish = false;
        PushModalState();

        if (!useModalManager)
        {
            AcquireCameraLock();
            HideDayCycleHud();
        }

        gameObject.SetActive(true);
        ResolveReferences();
        StoreFishPreviewDefaultPose();
        SetNewFish(_fish);
        UpdateRotateHintText();
        UpdateCloseHintText();
        SetRotateHintVisible(true);
        SetCloseHintVisible(false);
        ignoreFishRotationThisFrame = true;

        if (resultText != null)
            resultText.text = $"{_fish.typeOfFish.fishName} - {_fish.weight} kg";

        SetDescriptionText(_fish.typeOfFish);

        if (enableSkipCoroutine != null)
            StopCoroutine(enableSkipCoroutine);

        enableSkipCoroutine = StartCoroutine(EnableSkipAfterDelay());
    }

    private IEnumerator EnableSkipAfterDelay()
    {
        if (minDisplayTime > 0f)
            yield return new WaitForSecondsRealtime(minDisplayTime);

        canSkip = true;
        SetCloseHintVisible(true);
        enableSkipCoroutine = null;
    }

    private void ResolveReferences()
    {
        if (resultText == null)
            resultText = FindChildComponentByName<TMP_Text>("ResultText");

        if (descriptionText == null)
            descriptionText = FindChildComponentByName<TMP_Text>("DescriptionText", "DescricaoText", "FishDescriptionText");

        if (descriptionText == null)
            CreateDescriptionTextFromResultText();

        if (newFishText == null)
        {
            TMP_Text foundNewFishText = FindChildComponentByName<TMP_Text>("NewFishText");

            if (foundNewFishText != null)
            {
                newFishText = foundNewFishText.gameObject;
                newFishTintText = foundNewFishText;
            }
        }

        if (newFishTintText == null && newFishText != null)
            newFishTintText = newFishText.GetComponentInChildren<TMP_Text>(true);

        if (rotateHintText == null)
            rotateHintText = FindChildComponentByName<TMP_Text>("RotateHintText");

        if (rotateHintText == null)
            rotateHintText = FindChildComponentByName<TMP_Text>("RotateHint");

        if (closeHintText == null)
            closeHintText = FindChildComponentByName<TMP_Text>("CloseHintText");

        if (closeHintText == null)
            closeHintText = FindChildComponentByName<TMP_Text>("CloseHint");

        if (dayCycle == null)
            dayCycle = FindFirstObjectByType<DayCycle>(FindObjectsInactive.Include);

        StoreHintDefaultStates();
        StoreNewFishTextDefaultState();
    }

    private T FindChildComponentByName<T>(params string[] _childNames) where T : Component
    {
        if (_childNames == null || _childNames.Length == 0)
            return null;

        Transform[] children = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];

            for (int nameIndex = 0; nameIndex < _childNames.Length; nameIndex++)
            {
                string childName = _childNames[nameIndex];

                if (string.IsNullOrWhiteSpace(childName))
                    continue;

                if (child.name != childName && child.name.Trim() != childName)
                    continue;

                return child.GetComponent<T>();
            }
        }

        return null;
    }

    private void CreateDescriptionTextFromResultText()
    {
        if (resultText == null || resultText.transform.parent == null)
            return;

        descriptionText = Instantiate(resultText, resultText.transform.parent);
        descriptionText.name = "DescriptionText";

        RectTransform resultRect = resultText.rectTransform;
        RectTransform descriptionRect = descriptionText.rectTransform;

        descriptionRect.SetSiblingIndex(resultRect.GetSiblingIndex() + 1);
        descriptionRect.anchorMin = resultRect.anchorMin;
        descriptionRect.anchorMax = resultRect.anchorMax;
        descriptionRect.pivot = resultRect.pivot;
        descriptionRect.anchoredPosition = resultRect.anchoredPosition + generatedDescriptionOffset;
        descriptionRect.sizeDelta = generatedDescriptionSize;

        descriptionText.text = string.Empty;
        descriptionText.fontSize = generatedDescriptionFontSize;
        descriptionText.fontSizeMax = generatedDescriptionFontSize;
        descriptionText.fontSizeMin = Mathf.Min(18f, generatedDescriptionFontSize);
        descriptionText.enableAutoSizing = true;
        descriptionText.alignment = TextAlignmentOptions.Top;
        descriptionText.color = new Color(resultText.color.r, resultText.color.g, resultText.color.b, resultText.color.a * 0.9f);
        descriptionText.raycastTarget = false;
        descriptionText.margin = new Vector4(12f, 0f, 12f, 0f);
    }

    private void SetDescriptionText(FishScriptableObject _fishType)
    {
        if (descriptionText == null)
            return;

        descriptionText.text = GetFishDescription(_fishType);
    }

    private string GetFishDescription(FishScriptableObject _fishType)
    {
        if (_fishType == null || string.IsNullOrWhiteSpace(_fishType.description))
            return emptyDescriptionMessage;

        return _fishType.description;
    }

    private void TrySubscribeInput()
    {
        if (isInputSubscribed || InputHandler.instance == null)
            return;

        InputHandler.instance.onInteractPressed += TryClose;
        InputHandler.instance.onPausePressed += TryClose;
        InputHandler.instance.onAnyButtonPressed += TryClose;
        InputDeviceDetector.DeviceTypeChanged += HandleDeviceTypeChanged;
        isInputSubscribed = true;
    }

    private void UnsubscribeInput()
    {
        if (!isInputSubscribed || InputHandler.instance == null)
            return;

        InputHandler.instance.onInteractPressed -= TryClose;
        InputHandler.instance.onPausePressed -= TryClose;
        InputHandler.instance.onAnyButtonPressed -= TryClose;
        InputDeviceDetector.DeviceTypeChanged -= HandleDeviceTypeChanged;
        isInputSubscribed = false;
    }

    private void EnableSkip()
    {
        canSkip = true;
    }

    private void TryClose()
    {
        if (UIModalManager.WasBackHandledThisFrame ||
            (modalToken != UIModalManager.InvalidToken && !UIModalManager.IsTopModal(modalToken)))
        {
            return;
        }

        if (!isShowing || !canSkip)
            return;

        if (IsMouseLeftButtonHeld())
            return;

        if (modalToken != UIModalManager.InvalidToken)
            UIModalManager.MarkBackHandledThisFrame();

        gameObject.SetActive(false);
    }

    private void HideImmediate()
    {
        isShowing = false;
        canSkip = false;
        hasRotatedFish = false;
        SetNewFishTextVisible(false);
        SetRotateHintVisible(false);
        SetCloseHintVisible(false);
        gameObject.SetActive(false);
    }

    private void NotifyClosed()
    {
        isShowing = false;
        canSkip = false;
        hasRotatedFish = false;
        SetNewFishTextVisible(false);
        SetRotateHintVisible(false);
        SetCloseHintVisible(false);
        PopModalState();
        RestoreDayCycleHud();
        ReleaseCameraLock();
        Closed?.Invoke();
    }

    private void HideDayCycleHud()
    {
        if (!hideDayCycleHudWhileShowing)
            return;

        ResolveReferences();

        if (dayCycle == null || hasStoredDayCycleHudVisibility)
            return;

        wasHourTextVisible = dayCycle.IsHourTextVisible;
        wasDayTextVisible = dayCycle.IsDayTextVisible;
        hasStoredDayCycleHudVisibility = true;

        dayCycle.SetDayCycleHudVisible(false);
    }

    private void RestoreDayCycleHud()
    {
        if (!hasStoredDayCycleHudVisibility || dayCycle == null)
            return;

        dayCycle.SetDayCycleHudVisible(wasHourTextVisible, wasDayTextVisible);
        hasStoredDayCycleHudVisibility = false;
    }

    private void AcquireCameraLock()
    {
        if (hasCameraLock)
            return;

        PlayerCamera.PushCameraLock();
        hasCameraLock = true;
    }

    private void ReleaseCameraLock()
    {
        if (!hasCameraLock)
            return;

        PlayerCamera.PopCameraLock();
        hasCameraLock = false;
    }

    private void PushModalState()
    {
        if (!useModalManager || modalToken != UIModalManager.InvalidToken)
            return;

        UIModalRequest request = UIModalRequest.Create(
            this,
            pauseTimeWhileShowing,
            hideHudWhileShowing,
            blockPauseWhileShowing,
            lockCameraWhileShowing,
            TryClose
        );

        modalToken = UIModalManager.PushModal(request);
    }

    private void PopModalState()
    {
        UIModalManager.PopModal(ref modalToken);
    }

    private void HandleDeviceTypeChanged(InputDeviceType _deviceType)
    {
        if (isShowing)
            UpdateRotateHintText();
    }

    private void UpdateRotateHintText()
    {
        if (rotateHintText == null)
            return;

        rotateHintText.text = InputDeviceDetector.CurrentDeviceType == InputDeviceType.GenericController
            ? controllerRotateHint
            : mouseRotateHint;
    }

    private void UpdateCloseHintText()
    {
        if (closeHintText == null)
            return;

        closeHintText.text = closeHintMessage;
    }

    private void StoreHintDefaultStates()
    {
        StoreHintDefaultState(rotateHintText, ref hasStoredRotateHintState, ref rotateHintDefaultScale, ref rotateHintDefaultAlpha);
        StoreHintDefaultState(closeHintText, ref hasStoredCloseHintState, ref closeHintDefaultScale, ref closeHintDefaultAlpha);
    }

    private void StoreHintDefaultState(TMP_Text _hintText, ref bool _hasStored, ref Vector3 _defaultScale, ref float _defaultAlpha)
    {
        if (_hasStored || _hintText == null)
            return;

        _defaultScale = _hintText.rectTransform.localScale;
        _defaultAlpha = _hintText.color.a;
        _hasStored = true;
    }

    private void UpdateHintPulses()
    {
        if (!isShowing)
            return;

        PulseHintText(rotateHintText, rotateHintDefaultScale, rotateHintDefaultAlpha);
        PulseHintText(closeHintText, closeHintDefaultScale, closeHintDefaultAlpha);
    }

    private void PulseHintText(TMP_Text _hintText, Vector3 _defaultScale, float _defaultAlpha)
    {
        if (_hintText == null || !_hintText.enabled || !_hintText.gameObject.activeInHierarchy)
            return;

        float pulse = (Mathf.Sin(Time.unscaledTime * hintPulseSpeed) * 0.5f) + 0.5f;
        _hintText.rectTransform.localScale = _defaultScale * (1f + (pulse * hintPulseScale));

        Color color = _hintText.color;
        color.a = _defaultAlpha * Mathf.Lerp(hintMinimumAlpha, hintMaximumAlpha, pulse);
        _hintText.color = color;
    }

    private void SetRotateHintVisible(bool _visible)
    {
        bool shouldShow = _visible && !hasRotatedFish;
        SetHintVisible(rotateHintText, shouldShow, rotateHintDefaultScale, rotateHintDefaultAlpha);
    }

    private void SetCloseHintVisible(bool _visible)
    {
        SetHintVisible(closeHintText, _visible && canSkip, closeHintDefaultScale, closeHintDefaultAlpha);
    }

    private void SetHintVisible(TMP_Text _hintText, bool _visible, Vector3 _defaultScale, float _defaultAlpha)
    {
        if (_hintText == null)
            return;

        if (_hintText.gameObject != gameObject)
            _hintText.gameObject.SetActive(_visible);

        _hintText.enabled = _visible;

        if (!_visible)
        {
            _hintText.rectTransform.localScale = _defaultScale;
            Color color = _hintText.color;
            color.a = _defaultAlpha;
            _hintText.color = color;
        }
    }

    private void StoreNewFishTextDefaultState()
    {
        if (hasStoredNewFishTextState || newFishTintText == null)
            return;

        newFishTextDefaultScale = newFishTintText.rectTransform.localScale;
        newFishTextDefaultColor = newFishTintText.color;
        hasStoredNewFishTextState = true;
    }

    private void SetNewFishTextVisible(bool _visible)
    {
        isNewFishTextVisible = _visible;

        if (newFishText != null)
            newFishText.SetActive(_visible);

        if (!_visible)
            ResetNewFishTextTint();
    }

    private void UpdateNewFishTextTint()
    {
        if (!isShowing || !isNewFishTextVisible || newFishTintText == null)
            return;

        StoreNewFishTextDefaultState();

        float pulse = (Mathf.Sin(Time.unscaledTime * newFishTintSpeed) * 0.5f) + 0.5f;
        float tintAmount = pulse * newFishTintStrength;

        newFishTintText.color = Color.Lerp(newFishTextDefaultColor, newFishTintColor, tintAmount);
        newFishTintText.rectTransform.localScale = newFishTextDefaultScale * (1f + (pulse * newFishTintScale));
    }

    private void ResetNewFishTextTint()
    {
        if (newFishTintText == null || !hasStoredNewFishTextState)
            return;

        newFishTintText.color = newFishTextDefaultColor;
        newFishTintText.rectTransform.localScale = newFishTextDefaultScale;
    }

    private void RotateStarImage()
    {
        if (starImage == null) return;

        starAngle += rotationSpeed * Time.unscaledDeltaTime;
        starAngle %= 360f;
        starImage.localRotation = Quaternion.Euler(0, 0, starAngle);

    }

    private void RotateFish()
    {

        if (!hasFishVisual || objectRenderer == null) return;

        if (ignoreFishRotationThisFrame)
        {
            ignoreFishRotationThisFrame = false;
            ApplyFishRotation();
            return;
        }

        Vector2 rotationDelta = GetFishRotationDelta(out bool isManualRotationInput);

        if (!hasRotatedFish && isManualRotationInput && rotationDelta.sqrMagnitude > 0.0001f)
        {
            hasRotatedFish = true;
            SetRotateHintVisible(false);
        }

        fishAngle = Mathf.Repeat(fishAngle + rotationDelta.x, 360f);
        fishPitchAngle = Mathf.Clamp(fishPitchAngle + rotationDelta.y, minFishPitchAngle, maxFishPitchAngle);

        ApplyFishRotation();

    }

    private void ApplyFishModel()
    {
        if (currentFishType == null)
            return;

        FishVisualUtility.ApplyModel(currentFishType, mesh, objectRenderer, false);
        FishVisualUtility.ApplyPreviewLighting(objectRenderer);
        hasFishVisual = FishVisualUtility.HasVisual(currentFishType);
    }

    private void ResetFishRotation()
    {
        fishAngle = 0f;
        fishPitchAngle = 0f;
        RestoreFishPreviewDefaultPose();
        ApplyFishRotation();
    }

    private void ApplyFishRotation()
    {
        if (objectRenderer == null)
            return;

        StoreFishPreviewDefaultPose();
        Vector3 eulerAngles = fishPreviewDefaultLocalEulerAngles;
        eulerAngles.x += fishPitchAngle;
        eulerAngles.y += fishAngle;
        objectRenderer.transform.localRotation = Quaternion.Euler(eulerAngles);
    }

    private void StoreFishPreviewDefaultPose()
    {
        if (hasStoredFishPreviewPose || objectRenderer == null)
            return;

        Transform fishPreviewTransform = objectRenderer.transform;
        fishPreviewDefaultLocalPosition = fishPreviewTransform.localPosition;
        fishPreviewDefaultLocalRotation = fishPreviewTransform.localRotation;
        fishPreviewDefaultLocalEulerAngles = fishPreviewTransform.localEulerAngles;
        fishPreviewDefaultLocalScale = fishPreviewTransform.localScale;
        hasStoredFishPreviewPose = true;
    }

    private void RestoreFishPreviewDefaultPose()
    {
        if (!hasStoredFishPreviewPose || objectRenderer == null)
            return;

        Transform fishPreviewTransform = objectRenderer.transform;
        fishPreviewTransform.localPosition = fishPreviewDefaultLocalPosition;
        fishPreviewTransform.localRotation = fishPreviewDefaultLocalRotation;
        fishPreviewTransform.localScale = fishPreviewDefaultLocalScale;
    }

    private Vector2 GetFishRotationDelta(out bool _isManualRotationInput)
    {
        _isManualRotationInput = false;

        if (InputHandler.instance != null)
        {
            Vector2 directGamepadRotation = GetDirectGamepadFishRotationDelta(out _isManualRotationInput);

            if (_isManualRotationInput)
                return directGamepadRotation;

            if (InputDeviceDetector.CurrentDeviceType == InputDeviceType.GenericController)
                return GetControllerFishRotationDelta(InputHandler.instance.lookInput, out _isManualRotationInput);

            return GetMouseFishRotationDelta(out _isManualRotationInput);
        }

        float autoRotationSpeed = fishRotationSpeed > 0f ? fishRotationSpeed : rotationSpeed;
        return new Vector2(autoRotationSpeed * Time.unscaledDeltaTime, 0f);
    }

    private Vector2 GetDirectGamepadFishRotationDelta(out bool _isManualRotationInput)
    {
        _isManualRotationInput = false;

        if (Gamepad.current == null)
            return Vector2.zero;

        Vector2 lookInput = Gamepad.current.rightStick.ReadValue();
        return GetControllerFishRotationDelta(lookInput, out _isManualRotationInput);
    }

    private Vector2 GetControllerFishRotationDelta(Vector2 _lookInput, out bool _isManualRotationInput)
    {
        _isManualRotationInput = false;
        float deadZoneSqr = fishControllerRotationDeadZone * fishControllerRotationDeadZone;

        if (_lookInput.sqrMagnitude <= deadZoneSqr)
            return Vector2.zero;

        _isManualRotationInput = true;
        return _lookInput * fishInputRotationSpeed * Time.unscaledDeltaTime;
    }

    private Vector2 GetMouseFishRotationDelta(out bool _isManualRotationInput)
    {
        _isManualRotationInput = false;

        if (Mouse.current == null || !Mouse.current.leftButton.isPressed)
            return Vector2.zero;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        if (mouseDelta.sqrMagnitude <= 0.0001f)
            return Vector2.zero;

        _isManualRotationInput = true;
        return mouseDelta * fishMouseRotationSensitivity;
    }

    private bool IsMouseLeftButtonHeld()
    {
        return Mouse.current != null &&
               (Mouse.current.leftButton.isPressed || Mouse.current.leftButton.wasPressedThisFrame);
    }

    private void ShowRarityStars(int _fishRarity)
    {
        if (fishStarsRateObjects == null)
            return;

        for (int i = 0; i < fishStarsRateObjects.Length; i++)
        {
            if (fishStarsRateObjects[i] != null)
                fishStarsRateObjects[i].SetActive(i < _fishRarity);
        }
    }

    private void Update()
    {

        RotateStarImage();       
        ApplyFishModel();
        RotateFish();
        UpdateHintPulses();
        UpdateNewFishTextTint();
    }
}
