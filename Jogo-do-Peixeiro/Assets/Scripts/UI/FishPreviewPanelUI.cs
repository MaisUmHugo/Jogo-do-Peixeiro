using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas))]
[RequireComponent(typeof(GraphicRaycaster))]
public class FishPreviewPanelUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panel;
    [SerializeField] private bool closeOnAwake = true;

    [Header("Modal")]
    [SerializeField] private bool pauseTimeWhileOpen = true;
    [SerializeField] private bool hideHudWhileOpen = true;
    [SerializeField] private bool blockPauseWhileOpen = true;
    [SerializeField] private bool lockCameraWhileOpen = true;

    [Header("Layering")]
    [SerializeField] private bool bringToFrontOnOpen = true;
    [SerializeField] private Canvas panelCanvas;
    [SerializeField] private bool useOverrideSortingWhileOpen;
    [SerializeField] private int sortingOrderWhileOpen = 500;

    [Header("Previous Panel")]
    [SerializeField] private bool hidePreviousPanelWhileOpen = true;
    [SerializeField] private bool addCanvasGroupWhenMissing = true;

    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Selectable firstSelected;

    [Header("Texts")]
    [SerializeField] private TMP_Text fishNameText;
    [SerializeField] private TMP_Text weightText;
    [SerializeField] private TMP_Text valueText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text captureCountText;
    [SerializeField] private string weightRangeFormat = "Peso: {0}-{1} kg";
    [SerializeField] private string weightFormat = "Peso: {0:0.#} kg";
    [SerializeField] private string valueFormat = "Valor: R$ {0}";
    [SerializeField] private string baseValueFormat = "Valor base: R$ {0}";
    [SerializeField] private string captureCountFormat = "Capturado {0} vez(es) neste save";

    [Header("Rotate Hint")]
    [SerializeField] private TMP_Text rotateHintText;
    [SerializeField] private string mouseRotateHint = "Segure o botão esquerdo do mouse para girar o peixe.";
    [SerializeField] private string controllerRotateHint = "Use o analógico direito para girar o peixe.";
    [SerializeField] private float hintPulseSpeed = 4f;
    [SerializeField] private float hintPulseScale = 0.08f;
    [SerializeField, Range(0f, 1f)] private float hintMinimumAlpha = 0.65f;
    [SerializeField, Range(0f, 1f)] private float hintMaximumAlpha = 1f;

    [Header("Fish Preview")]
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private Renderer fishRenderer;
    [SerializeField] private GameObject[] fishStarsRateObjects;

    [Header("Rotation")]
    [SerializeField] private float fishInputRotationSpeed = 180f;
    [SerializeField] private float fishMouseRotationSensitivity = 0.25f;
    [SerializeField, Range(0f, 1f)] private float fishControllerRotationDeadZone = 0.2f;
    [SerializeField] private float minFishPitchAngle = -45f;
    [SerializeField] private float maxFishPitchAngle = 45f;

    private FishScriptableObject currentFishType;
    private FishData currentFishData;
    private bool isOpen;
    private bool isOpeningFromShowFish;
    private bool isInputSubscribed;
    private bool areButtonsBound;
    private bool hasStoredPreviewPose;
    private Vector3 fishPreviewDefaultLocalPosition;
    private Vector3 fishPreviewDefaultLocalEulerAngles;
    private Vector3 fishPreviewDefaultLocalScale;
    private Vector3 rotateHintDefaultScale = Vector3.one;
    private float rotateHintDefaultAlpha = 1f;
    private float fishAngle;
    private float fishPitchAngle;
    private Selectable previousSelected;
    private int modalToken = UIModalManager.InvalidToken;
    private bool hasStoredRotateHintState;
    private bool hasStoredCanvasSorting;
    private bool previousOverrideSorting;
    private int previousSortingOrder;
    private GameObject previousPanelRoot;
    private CanvasGroup previousPanelCanvasGroup;
    private bool hasStoredPreviousPanelState;
    private float previousPanelAlpha = 1f;
    private bool previousPanelInteractable = true;
    private bool previousPanelBlocksRaycasts = true;

    private GameObject PanelObject => panel != null ? panel : gameObject;

    private void OnValidate()
    {
        fishInputRotationSpeed = Mathf.Max(0f, fishInputRotationSpeed);
        fishMouseRotationSensitivity = Mathf.Max(0f, fishMouseRotationSensitivity);
        fishControllerRotationDeadZone = Mathf.Clamp01(fishControllerRotationDeadZone);
        hintPulseSpeed = Mathf.Max(0f, hintPulseSpeed);
        hintPulseScale = Mathf.Max(0f, hintPulseScale);

        if (hintMaximumAlpha < hintMinimumAlpha)
            hintMaximumAlpha = hintMinimumAlpha;

        if (maxFishPitchAngle < minFishPitchAngle)
            maxFishPitchAngle = minFishPitchAngle;
    }

    private void Awake()
    {
        ResolveReferences();
        BindButtons();
        StoreFishPreviewDefaultPose();

        if (closeOnAwake && !isOpeningFromShowFish)
            CloseImmediate(false);
    }

    private void OnEnable()
    {
        ResolveReferences();
        BindButtons();
        TrySubscribeInput();
    }

    private void OnDisable()
    {
        UnsubscribeInput();
        UIModalManager.PopModal(ref modalToken);
    }

    private void Update()
    {
        if (!isOpen)
            return;

        ApplyFishModel();
        RotateFish();
        PulseRotateHint();
    }

    public void ShowFish(FishData _fish)
    {
        ShowFish(_fish, null);
    }

    public void ShowFish(FishData _fish, GameObject _previousPanelRoot)
    {
        currentFishData = _fish;
        currentFishType = _fish != null ? _fish.typeOfFish : null;
        previousPanelRoot = _previousPanelRoot;
        OpenPreview();
    }

    public void ShowFish(FishScriptableObject _fishType)
    {
        ShowFish(_fishType, null);
    }

    public void ShowFish(FishScriptableObject _fishType, GameObject _previousPanelRoot)
    {
        currentFishData = null;
        currentFishType = _fishType;
        previousPanelRoot = _previousPanelRoot;
        OpenPreview();
    }

    public void Close()
    {
        CloseImmediate(true);
    }

    private void OpenPreview()
    {
        if (currentFishType == null)
            return;

        previousSelected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null
            ? EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>()
            : null;

        isOpen = true;
        HidePreviousPanel();
        isOpeningFromShowFish = true;
        PanelObject.SetActive(true);
        isOpeningFromShowFish = false;
        BringPanelToFront();
        PushModalState();
        ApplyFishPreview();
        UpdateRotateHintText();
        SetRotateHintVisible(true);
        TrySubscribeInput();
        SelectInitialControl();
    }

    private void CloseImmediate(bool _restoreSelection)
    {
        isOpen = false;
        UIModalManager.PopModal(ref modalToken);
        RestorePanelSorting();
        SetRotateHintVisible(false);
        PanelObject.SetActive(false);
        RestorePreviousPanel();

        if (_restoreSelection && UISelectionHelper.IsUsable(previousSelected))
            UISelectionHelper.Select(previousSelected);
    }

    private void ResolveReferences()
    {
        if (panel == null)
            panel = gameObject;

        if (panelCanvas == null)
            panelCanvas = PanelObject.GetComponent<Canvas>();

        EnsureGraphicRaycaster();

        if (closeButton == null)
            closeButton = FindChildComponent<Button>("CloseButton", "BackButton", "VoltarButton", "FecharButton");

        if (fishNameText == null)
            fishNameText = FindChildComponent<TMP_Text>("FishNameText", "NameText", "NomeText");

        if (weightText == null)
            weightText = FindChildComponent<TMP_Text>("WeightText", "PesoText");

        if (valueText == null)
            valueText = FindChildComponent<TMP_Text>("ValueText", "ValorText");

        if (descriptionText == null)
            descriptionText = FindChildComponent<TMP_Text>("DescriptionText", "DescricaoText");

        if (captureCountText == null)
            captureCountText = FindChildComponent<TMP_Text>("CaptureCountText", "CaughtCountText", "CapturadoText");

        if (rotateHintText == null)
            rotateHintText = FindChildComponent<TMP_Text>("RotateHintText", "RotateHint");

        if (meshFilter == null)
            meshFilter = GetComponentInChildren<MeshFilter>(true);

        if (fishRenderer == null)
            fishRenderer = GetComponentInChildren<Renderer>(true);

        if (firstSelected == null)
            firstSelected = closeButton;

        StoreRotateHintDefaultState();
    }

    private void EnsureGraphicRaycaster()
    {
        if (panelCanvas == null)
            return;

        if (!panelCanvas.TryGetComponent(out GraphicRaycaster _))
            panelCanvas.gameObject.AddComponent<GraphicRaycaster>();
    }

    private T FindChildComponent<T>(params string[] _names) where T : Component
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];

            for (int j = 0; j < _names.Length; j++)
            {
                if (child.name == _names[j] && child.TryGetComponent(out T component))
                    return component;
            }
        }

        return null;
    }

    private void BindButtons()
    {
        if (areButtonsBound)
            return;

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        areButtonsBound = true;
    }

    private void UnbindButtons()
    {
        if (!areButtonsBound)
            return;

        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);

        areButtonsBound = false;
    }

    private void TrySubscribeInput()
    {
        if (isInputSubscribed || InputHandler.instance == null)
            return;

        InputHandler.instance.onPausePressed += HandlePausePressed;
        InputDeviceDetector.DeviceTypeChanged += HandleDeviceTypeChanged;
        isInputSubscribed = true;
    }

    private void UnsubscribeInput()
    {
        if (!isInputSubscribed || InputHandler.instance == null)
            return;

        InputHandler.instance.onPausePressed -= HandlePausePressed;
        InputDeviceDetector.DeviceTypeChanged -= HandleDeviceTypeChanged;
        isInputSubscribed = false;
    }

    private void HandlePausePressed()
    {
        if (!isOpen ||
            UIModalManager.WasBackHandledThisFrame ||
            !UIModalManager.IsTopModal(modalToken))
        {
            return;
        }

        UIModalManager.MarkBackHandledThisFrame();
        Close();
    }

    private void PushModalState()
    {
        if (modalToken != UIModalManager.InvalidToken)
            return;

        UIModalRequest request = UIModalRequest.Create(
            this,
            pauseTimeWhileOpen,
            hideHudWhileOpen,
            blockPauseWhileOpen,
            lockCameraWhileOpen,
            Close
        );

        modalToken = UIModalManager.PushModal(request);
    }

    private void BringPanelToFront()
    {
        if (bringToFrontOnOpen)
            PanelObject.transform.SetAsLastSibling();

        if (!useOverrideSortingWhileOpen || panelCanvas == null)
            return;

        if (!hasStoredCanvasSorting)
        {
            previousOverrideSorting = panelCanvas.overrideSorting;
            previousSortingOrder = panelCanvas.sortingOrder;
            hasStoredCanvasSorting = true;
        }

        panelCanvas.overrideSorting = true;
        panelCanvas.sortingOrder = sortingOrderWhileOpen;
    }

    private void RestorePanelSorting()
    {
        if (!hasStoredCanvasSorting || panelCanvas == null)
            return;

        panelCanvas.overrideSorting = previousOverrideSorting;
        panelCanvas.sortingOrder = previousSortingOrder;
        hasStoredCanvasSorting = false;
    }

    private void HidePreviousPanel()
    {
        if (!hidePreviousPanelWhileOpen || previousPanelRoot == null)
            return;

        if (previousPanelRoot == PanelObject || PanelObject.transform.IsChildOf(previousPanelRoot.transform))
            return;

        previousPanelCanvasGroup = previousPanelRoot.GetComponent<CanvasGroup>();

        if (previousPanelCanvasGroup == null)
        {
            if (!addCanvasGroupWhenMissing)
                return;

            previousPanelCanvasGroup = previousPanelRoot.AddComponent<CanvasGroup>();
        }

        previousPanelAlpha = previousPanelCanvasGroup.alpha;
        previousPanelInteractable = previousPanelCanvasGroup.interactable;
        previousPanelBlocksRaycasts = previousPanelCanvasGroup.blocksRaycasts;
        hasStoredPreviousPanelState = true;

        previousPanelCanvasGroup.alpha = 0f;
        previousPanelCanvasGroup.interactable = false;
        previousPanelCanvasGroup.blocksRaycasts = false;
    }

    private void RestorePreviousPanel()
    {
        if (!hasStoredPreviousPanelState || previousPanelCanvasGroup == null)
            return;

        previousPanelCanvasGroup.alpha = previousPanelAlpha;
        previousPanelCanvasGroup.interactable = previousPanelInteractable;
        previousPanelCanvasGroup.blocksRaycasts = previousPanelBlocksRaycasts;
        hasStoredPreviousPanelState = false;
        previousPanelCanvasGroup = null;
        previousPanelRoot = null;
    }

    private void SelectInitialControl()
    {
        Selectable target = firstSelected != null ? firstSelected : closeButton;
        UISelectionHelper.Select(target, PanelObject);
    }

    private void HandleDeviceTypeChanged(InputDeviceType _deviceType)
    {
        if (isOpen)
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

    private void StoreRotateHintDefaultState()
    {
        if (hasStoredRotateHintState || rotateHintText == null)
            return;

        rotateHintDefaultScale = rotateHintText.rectTransform.localScale;
        rotateHintDefaultAlpha = rotateHintText.color.a;
        hasStoredRotateHintState = true;
    }

    private void SetRotateHintVisible(bool _visible)
    {
        if (rotateHintText == null)
            return;

        rotateHintText.gameObject.SetActive(_visible);
        rotateHintText.enabled = _visible;

        if (!_visible)
        {
            rotateHintText.rectTransform.localScale = rotateHintDefaultScale;
            Color color = rotateHintText.color;
            color.a = rotateHintDefaultAlpha;
            rotateHintText.color = color;
        }
    }

    private void PulseRotateHint()
    {
        if (rotateHintText == null || !rotateHintText.enabled || !rotateHintText.gameObject.activeInHierarchy)
            return;

        float pulse = (Mathf.Sin(Time.unscaledTime * hintPulseSpeed) * 0.5f) + 0.5f;
        rotateHintText.rectTransform.localScale = rotateHintDefaultScale * (1f + (pulse * hintPulseScale));

        Color color = rotateHintText.color;
        color.a = rotateHintDefaultAlpha * Mathf.Lerp(hintMinimumAlpha, hintMaximumAlpha, pulse);
        rotateHintText.color = color;
    }

    private void ApplyFishPreview()
    {
        ResetFishRotation();
        ApplyFishModel();
        ApplyTexts();
        ShowRarityStars(currentFishType != null ? currentFishType.rarity : 0);
    }

    private void ApplyFishModel()
    {
        if (currentFishType == null)
            return;

        FishVisualUtility.ApplyModel(currentFishType, meshFilter, fishRenderer, true);
    }

    private void ApplyTexts()
    {
        if (currentFishType == null)
            return;

        if (fishNameText != null)
            fishNameText.text = GetFishDisplayName(currentFishType);

        if (weightText != null)
        {
            weightText.text = currentFishData != null
                ? string.Format(weightFormat, currentFishData.weight)
                : string.Format(weightRangeFormat, currentFishType.minWeight, currentFishType.maxWeight);
        }

        if (valueText != null)
        {
            valueText.text = currentFishData != null
                ? string.Format(valueFormat, FishPriceCalculator.CalculatePrice(currentFishData))
                : string.Format(baseValueFormat, currentFishType.BasePrice);
        }

        if (descriptionText != null)
            descriptionText.text = currentFishType.description;

        if (captureCountText != null)
            captureCountText.text = string.Format(captureCountFormat, FishCaptureHistory.GetCaptureCount(currentFishType));
    }

    private string GetFishDisplayName(FishScriptableObject _fish)
    {
        if (_fish == null)
            return "Peixe";

        return !string.IsNullOrWhiteSpace(_fish.fishName) ? _fish.fishName : _fish.name;
    }

    private void ShowRarityStars(int _rarity)
    {
        if (fishStarsRateObjects == null)
            return;

        for (int i = 0; i < fishStarsRateObjects.Length; i++)
        {
            if (fishStarsRateObjects[i] != null)
                fishStarsRateObjects[i].SetActive(i < _rarity);
        }
    }

    private void RotateFish()
    {
        if (fishRenderer == null)
            return;

        Vector2 rotationDelta = GetFishRotationDelta();
        fishAngle = Mathf.Repeat(fishAngle + rotationDelta.x, 360f);
        fishPitchAngle = Mathf.Clamp(fishPitchAngle + rotationDelta.y, minFishPitchAngle, maxFishPitchAngle);
        ApplyFishRotation();
    }

    private Vector2 GetFishRotationDelta()
    {
        if (InputHandler.instance != null && InputDeviceDetector.CurrentDeviceType == InputDeviceType.GenericController)
            return GetControllerFishRotationDelta(InputHandler.instance.lookInput);

        return GetMouseFishRotationDelta();
    }

    private Vector2 GetControllerFishRotationDelta(Vector2 _lookInput)
    {
        float deadZoneSqr = fishControllerRotationDeadZone * fishControllerRotationDeadZone;

        if (_lookInput.sqrMagnitude <= deadZoneSqr)
            return Vector2.zero;

        return _lookInput * fishInputRotationSpeed * Time.unscaledDeltaTime;
    }

    private Vector2 GetMouseFishRotationDelta()
    {
        if (Mouse.current == null || !Mouse.current.leftButton.isPressed)
            return Vector2.zero;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        if (mouseDelta.sqrMagnitude <= 0.0001f)
            return Vector2.zero;

        return mouseDelta * fishMouseRotationSensitivity;
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
        if (fishRenderer == null)
            return;

        StoreFishPreviewDefaultPose();
        Vector3 eulerAngles = fishPreviewDefaultLocalEulerAngles;
        eulerAngles.x += fishPitchAngle;
        eulerAngles.y += fishAngle;
        fishRenderer.transform.localRotation = Quaternion.Euler(eulerAngles);
    }

    private void StoreFishPreviewDefaultPose()
    {
        if (hasStoredPreviewPose || fishRenderer == null)
            return;

        Transform fishPreviewTransform = fishRenderer.transform;
        fishPreviewDefaultLocalPosition = fishPreviewTransform.localPosition;
        fishPreviewDefaultLocalEulerAngles = fishPreviewTransform.localEulerAngles;
        fishPreviewDefaultLocalScale = fishPreviewTransform.localScale;
        hasStoredPreviewPose = true;
    }

    private void RestoreFishPreviewDefaultPose()
    {
        if (!hasStoredPreviewPose || fishRenderer == null)
            return;

        Transform fishPreviewTransform = fishRenderer.transform;
        fishPreviewTransform.localPosition = fishPreviewDefaultLocalPosition;
        fishPreviewTransform.localRotation = Quaternion.Euler(fishPreviewDefaultLocalEulerAngles);
        fishPreviewTransform.localScale = fishPreviewDefaultLocalScale;
    }
}
