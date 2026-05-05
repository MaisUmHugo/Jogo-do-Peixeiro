using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class FishResultUI : MonoBehaviour
{
    private List<FishScriptableObject> fishList = new List<FishScriptableObject>();
    [SerializeField] private GameObject newFishText;
    [SerializeField] private TMP_Text resultText;

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
    [SerializeField] private float minFishPitchAngle = -45f;
    [SerializeField] private float maxFishPitchAngle = 45f;
    private float fishAngle;
    private float fishPitchAngle;

    [Header("Display")]
    [SerializeField] private float minDisplayTime = 1f;
    [SerializeField] private bool hideDayCycleHudWhileShowing = true;
    [SerializeField] private DayCycle dayCycle;

    private int fishRarity;
    private Mesh fishMesh;
    private Material fishMaterial;
    private bool isShowing;
    private bool canSkip;
    private bool isInputSubscribed;
    private bool hasCameraLock;
    private bool hasStoredDayCycleHudVisibility;
    private bool wasHourTextVisible;
    private bool wasDayTextVisible;

    public bool IsShowing => isShowing;
    public event System.Action Closed;

    private void OnValidate()
    {
        minDisplayTime = Mathf.Max(0f, minDisplayTime);
        fishInputRotationSpeed = Mathf.Max(0f, fishInputRotationSpeed);
        fishMouseRotationSensitivity = Mathf.Max(0f, fishMouseRotationSensitivity);

        if (maxFishPitchAngle < minFishPitchAngle)
            maxFishPitchAngle = minFishPitchAngle;
    }

    private void Awake()
    {
        ResolveReferences();

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
        UnsubscribeInput();

        if (isShowing)
            NotifyClosed();
        else
        {
            RestoreDayCycleHud();
            ReleaseCameraLock();
        }
    }

    public void SetNewFish(FishData _fish)
    {
        if (_fish == null || _fish.typeOfFish == null)
            return;

        fishMesh = _fish.typeOfFish.mesh;
        fishMaterial = _fish.typeOfFish.material;
        fishRarity = _fish.typeOfFish.rarity;

        if (mesh != null)
            mesh.mesh = fishMesh;

        if (objectRenderer != null)
            objectRenderer.material = fishMaterial;

        ShowRarityStars(fishRarity);
        fishAngle = 0f;
        fishPitchAngle = 0f;

        if (!fishList.Contains(_fish.typeOfFish))
        {

            fishList.Add(_fish.typeOfFish);

            if (newFishText != null)
                newFishText.SetActive(true);

        }
        else
        {
            if (newFishText != null)
                newFishText.SetActive(false);
        }
    }

    public void ShowCatchResult(FishData _fish)
    {
        if (_fish == null || _fish.typeOfFish == null)
            return;

        isShowing = true;
        canSkip = false;
        AcquireCameraLock();
        HideDayCycleHud();

        gameObject.SetActive(true);
        ResolveReferences();
        SetNewFish(_fish);

        if (resultText != null)
            resultText.text = $"{_fish.typeOfFish.fishName} - {_fish.weight} kg";

        CancelInvoke();
        Invoke(nameof(EnableSkip), minDisplayTime);
    }

    private void ResolveReferences()
    {
        if (resultText == null)
            resultText = FindChildComponentByName<TMP_Text>("ResultText");

        if (dayCycle == null)
            dayCycle = FindFirstObjectByType<DayCycle>(FindObjectsInactive.Include);
    }

    private T FindChildComponentByName<T>(string _childName) where T : Component
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];

            if (child.name != _childName)
                continue;

            return child.GetComponent<T>();
        }

        return null;
    }

    private void TrySubscribeInput()
    {
        if (isInputSubscribed || InputHandler.instance == null)
            return;

        InputHandler.instance.onInteractPressed += TryClose;
        InputHandler.instance.onPausePressed += TryClose;
        InputHandler.instance.onAnyButtonPressed += TryClose;
        isInputSubscribed = true;
    }

    private void UnsubscribeInput()
    {
        if (!isInputSubscribed || InputHandler.instance == null)
            return;

        InputHandler.instance.onInteractPressed -= TryClose;
        InputHandler.instance.onPausePressed -= TryClose;
        InputHandler.instance.onAnyButtonPressed -= TryClose;
        isInputSubscribed = false;
    }

    private void EnableSkip()
    {
        canSkip = true;
    }

    private void TryClose()
    {
        if (!isShowing || !canSkip)
            return;

        if (IsMouseLeftButtonHeld())
            return;

        gameObject.SetActive(false);
    }

    private void HideImmediate()
    {
        isShowing = false;
        canSkip = false;
        gameObject.SetActive(false);
    }

    private void NotifyClosed()
    {
        isShowing = false;
        canSkip = false;
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

    private void RotateStarImage()
    {
        if (starImage == null) return;

        starAngle += rotationSpeed * Time.unscaledDeltaTime;
        starAngle %= 360f;
        starImage.localRotation = Quaternion.Euler(0, 0, starAngle);

    }

    private void RotateFish()
    {

        if (fishMesh == null || objectRenderer == null) return;

        Vector2 rotationDelta = GetFishRotationDelta();
        fishAngle = Mathf.Repeat(fishAngle + rotationDelta.x, 360f);
        fishPitchAngle = Mathf.Clamp(fishPitchAngle + rotationDelta.y, minFishPitchAngle, maxFishPitchAngle);

        objectRenderer.transform.rotation = Quaternion.Euler(90f + fishPitchAngle, fishAngle, 90f);        

    }

    private Vector2 GetFishRotationDelta()
    {
        if (InputHandler.instance != null)
        {
            if (InputDeviceDetector.CurrentDeviceType == InputDeviceType.GenericController)
                return GetControllerFishRotationDelta(InputHandler.instance.lookInput);

            return GetMouseFishRotationDelta();
        }

        float autoRotationSpeed = fishRotationSpeed > 0f ? fishRotationSpeed : rotationSpeed;
        return new Vector2(autoRotationSpeed * Time.unscaledDeltaTime, 0f);
    }

    private Vector2 GetControllerFishRotationDelta(Vector2 _lookInput)
    {
        if (_lookInput.sqrMagnitude <= 0.0001f)
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
        RotateFish();
    }
}
