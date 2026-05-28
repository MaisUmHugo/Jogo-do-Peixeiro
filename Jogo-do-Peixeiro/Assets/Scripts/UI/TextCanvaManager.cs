using System;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class TextCanvaManager : MonoBehaviour
{
    public static event Action<DialogCameraFocusTarget> DialogStarted;
    public static event Action DialogFinished;
    public static event Action<DialogCameraFocusTarget> DialogCameraFocusRequested;
    public static event Action DialogCameraFocusCleared;

    [Header("Dialog Componentes")]
    [SerializeField] private TMP_Text dialogTMPText;
    [SerializeField] private TMP_Text nameTMPtext;
    [SerializeField] private Image dialogBackGroundImage;
    [SerializeField] private Image nameBackGroundImage;

    [Header("HUD During Dialog")]
    [SerializeField] private bool hideHudDuringDialog = true;
    [SerializeField] private bool lockInteractionsDuringDialog = true;
    [SerializeField] private bool blockPauseDuringDialog;
    [SerializeField] private GameObject[] extraHudRootsToHideDuringDialog;

    [Header("Hold Skip Prompt")]
    [SerializeField] private bool useHoldSkipPrompt = true;
    [SerializeField] private GameObject holdSkipPromptRoot;
    [SerializeField] private Image holdSkipIconImage;
    [SerializeField] private Image holdSkipFillImage;
    [SerializeField] private InputIconDatabase holdSkipIconDatabase;
    [SerializeField] private InputIconAction holdSkipIconAction = InputIconAction.Interact;
    [SerializeField] private bool allowHoldSkipWithoutPromptReferences;
    [SerializeField, Min(0.1f)] private float holdSkipDuration = 0.8f;
    [SerializeField, Min(0f)] private float holdSkipInputGraceDuration = 0.2f;
    [SerializeField] private Color holdSkipFillColor = new Color(0.24f, 0.92f, 0.28f, 0.78f);
    [SerializeField] private bool configureHoldSkipFillImage = true;
    [SerializeField] private bool syncHoldSkipFillSpriteWithIcon = true;
    [SerializeField] private Image.FillMethod holdSkipFillMethod = Image.FillMethod.Radial360;
    [SerializeField] private int holdSkipFillOrigin = 2;

    private readonly List<DialogSequenceLineData> dialogLines = new List<DialogSequenceLineData>();
    private int textIndex = 0;
    private bool isWritting = false;
    private Action onDialogFinished;
    private DialogCameraFocusTarget defaultCameraFocusTarget;
    private DialogCameraFocusTarget currentCameraFocusTarget;
    private int dialogStartedFrame = -1;
    private int lastAdvanceFrame = -1;
    private bool isSubscribedToInput;
    private float dialogStartedUnscaledTime;
    private float holdSkipProgress;
    private bool hasLoggedMissingHoldSkipIcon;
    private int dialogHudModalToken = UIModalManager.InvalidToken;
    private bool hasInteractionLock;
    public float TextSpeed;
    public bool IsDialogActive { get; private set; }

    public static void RequestCameraFocus(DialogCameraFocusTarget _focusTarget)
    {
        DialogCameraFocusRequested?.Invoke(_focusTarget);
    }

    public static void ClearCameraFocus()
    {
        DialogCameraFocusCleared?.Invoke();
    }

    private void OnEnable()
    {
        InputDeviceDetector.DeviceTypeChanged += HandleDeviceTypeChanged;
        TrySubscribeInput();
        ResolveHoldSkipPromptReferences();
        ConfigureHoldSkipPrompt();
        UpdateHoldSkipIcon();
        SetHoldSkipPromptVisible(IsDialogActive);
    }

    private void Start()
    {
        TrySubscribeInput();
        ResolveHoldSkipPromptReferences();
        ConfigureHoldSkipPrompt();
        UpdateHoldSkipIcon();
        SetHoldSkipPromptVisible(IsDialogActive);
    }

    private void Update()
    {
        if (!IsDialogActive)
        {
            ResetHoldSkipProgress();
            return;
        }

        if (UpdateHoldSkipPrompt())
            return;

        if (WasKeyboardAdvancePressedThisFrame() || WasMouseAdvancePressedThisFrame())
            AdvanceDialog();
    }

    private void OnDisable()
    {
        InputDeviceDetector.DeviceTypeChanged -= HandleDeviceTypeChanged;
        UnsubscribeInput();
        ReleaseDialogPresentationLocks();
    }

    public void Click(InputAction.CallbackContext input)
    {
        if (input.started)
            AdvanceDialog();
    }

    public void AdvanceDialog()
    {
        if (!IsDialogActive)
            return;

        if (Time.frameCount == dialogStartedFrame || Time.frameCount == lastAdvanceFrame)
            return;

        lastAdvanceFrame = Time.frameCount;

        if (!isWritting)
        {
            if (textIndex >= dialogLines.Count - 1)
            {
                FinishDialog();
                return;
            }

            textIndex++;

            ApplyCurrentLine(true);
            StartCoroutine(TypeLineCourotine());
        }
        else
        {
            FinishWriteSentence();
        }
    }

    public void InitializeDialog(DialogSequenceData _dialog)
    {
        InitializeDialog(_dialog, (Action)null);
    }

    public void InitializeDialog(DialogSequenceData _dialog, Action _onFinished)
    {
        InitializeDialog(_dialog, _onFinished, null);
    }

    public void InitializeDialog(DialogSequenceData _dialog, DialogCameraFocusTarget _cameraFocusTarget)
    {
        InitializeDialog(_dialog, (Action)null, _cameraFocusTarget);
    }

    public void InitializeDialog(DialogSequenceData _dialog, Action _onFinished, DialogCameraFocusTarget _cameraFocusTarget)
    {
        if (_dialog == null || !_dialog.HasLines)
        {
            _onFinished?.Invoke();
            return;
        }

        if (IsDialogActive)
            NotifyDialogFinished();

        TrySubscribeInput();
        StopAllCoroutines();
        dialogLines.Clear();
        textIndex = 0;
        isWritting = false;
        onDialogFinished = _onFinished;
        defaultCameraFocusTarget = _cameraFocusTarget;
        dialogStartedFrame = Time.frameCount;
        dialogStartedUnscaledTime = Time.unscaledTime;
        lastAdvanceFrame = -1;
        ResetHoldSkipProgress();

        dialogLines.AddRange(_dialog.GetLines());

        SetDialogActive(true);
        ApplyCurrentLine(false);
        NotifyDialogStarted();
        StartCoroutine(TypeLineCourotine());
    }

    public void CloseDialog()
    {
        CloseDialog(false);
    }

    public void CloseDialog(bool _invokeCallback)
    {
        if (!IsDialogActive && dialogLines.Count == 0)
            return;

        StopAllCoroutines();

        Action callback = _invokeCallback ? onDialogFinished : null;
        onDialogFinished = null;
        textIndex = 0;
        dialogLines.Clear();
        isWritting = false;
        lastAdvanceFrame = -1;
        ResetHoldSkipProgress();
        SetDialogActive(false);
        NotifyDialogFinished();
        callback?.Invoke();
    }

    private void FinishDialog()
    {
        CloseDialog(true);
    }

    private void FinishWriteSentence()
    {
        if (dialogLines.Count == 0)
            return;

        StopAllCoroutines();
        
        if (dialogTMPText != null)
            dialogTMPText.text = GetCurrentSentence();

        isWritting = false;

    }

    private IEnumerator TypeLineCourotine()
    {
        if (dialogLines.Count == 0 || dialogTMPText == null)
            yield break;

        isWritting = true;

        foreach (char _character in GetCurrentSentence())
        {
            dialogTMPText.text += _character;
            yield return new WaitForSecondsRealtime(TextSpeed);
        }

        isWritting = false;
    }

    private void ApplyCurrentLine(bool _requestCameraFocus)
    {
        if (dialogTMPText != null)
            dialogTMPText.text = "";

        DialogSequenceLineData currentLine = GetCurrentLine();
        string currentSpeakerName = currentLine != null ? currentLine.SpeakerName : string.Empty;
        bool hasSpeakerName = !string.IsNullOrWhiteSpace(currentSpeakerName);

        if (nameTMPtext != null)
        {
            nameTMPtext.text = hasSpeakerName ? currentSpeakerName : string.Empty;
            nameTMPtext.gameObject.SetActive(IsDialogActive && hasSpeakerName);
        }

        if (nameBackGroundImage != null)
            nameBackGroundImage.gameObject.SetActive(IsDialogActive && hasSpeakerName);

        currentCameraFocusTarget = currentLine != null && currentLine.CameraFocusTarget != null
            ? currentLine.CameraFocusTarget
            : defaultCameraFocusTarget;

        if (_requestCameraFocus && currentCameraFocusTarget != null)
            RequestCameraFocus(currentCameraFocusTarget);
    }

    private DialogSequenceLineData GetCurrentLine()
    {
        if (textIndex < 0 || textIndex >= dialogLines.Count)
            return null;

        return dialogLines[textIndex];
    }

    private string GetCurrentSentence()
    {
        DialogSequenceLineData currentLine = GetCurrentLine();
        return currentLine != null ? currentLine.Sentence : string.Empty;
    }

    private void HandleInteractPressed()
    {
        AdvanceDialog();
    }

    private bool WasKeyboardAdvancePressedThisFrame()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return false;

        return keyboard.spaceKey.wasPressedThisFrame ||
               keyboard.enterKey.wasPressedThisFrame ||
               keyboard.numpadEnterKey.wasPressedThisFrame;
    }

    private bool WasMouseAdvancePressedThisFrame()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
            return false;

        return true;
    }

    private void TrySubscribeInput()
    {
        if (isSubscribedToInput || InputHandler.instance == null)
            return;

        InputHandler.instance.onInteractPressed += HandleInteractPressed;
        isSubscribedToInput = true;
    }

    private void UnsubscribeInput()
    {
        if (!isSubscribedToInput || InputHandler.instance == null)
            return;

        InputHandler.instance.onInteractPressed -= HandleInteractPressed;
        isSubscribedToInput = false;
    }

    private void SetDialogActive(bool _active)
    {
        IsDialogActive = _active;

        if (_active)
            PushDialogPresentationLocks();
        else
            ReleaseDialogPresentationLocks();

        if (dialogTMPText != null)
        {
            dialogTMPText.text = "";
            dialogTMPText.gameObject.SetActive(_active);
        }

        if (nameTMPtext != null)
            nameTMPtext.gameObject.SetActive(_active);

        if (dialogBackGroundImage != null)
            dialogBackGroundImage.gameObject.SetActive(_active);

        if (nameBackGroundImage != null)
            nameBackGroundImage.gameObject.SetActive(_active);

        SetHoldSkipPromptVisible(_active);
    }

    private void HandleDeviceTypeChanged(InputDeviceType _deviceType)
    {
        UpdateHoldSkipIcon();
        ConfigureHoldSkipPrompt();
    }

    private bool UpdateHoldSkipPrompt()
    {
        if (!useHoldSkipPrompt)
        {
            ResetHoldSkipProgress();
            return false;
        }

        if (!allowHoldSkipWithoutPromptReferences && !HasHoldSkipPromptReferences())
        {
            ResetHoldSkipProgress();
            return false;
        }

        bool canReadHoldInput =
            Time.frameCount != dialogStartedFrame &&
            Time.unscaledTime - dialogStartedUnscaledTime >= holdSkipInputGraceDuration;
        bool isHeld = canReadHoldInput && IsHoldSkipInputHeld();

        if (isHeld)
            holdSkipProgress += Time.unscaledDeltaTime;
        else
            holdSkipProgress = 0f;

        SetHoldSkipFillAmount(holdSkipDuration > 0f ? holdSkipProgress / holdSkipDuration : 1f);

        if (holdSkipProgress < holdSkipDuration)
            return false;

        SkipEntireDialog();
        return true;
    }

    private void SkipEntireDialog()
    {
        if (!IsDialogActive)
            return;

        holdSkipProgress = 0f;
        SetHoldSkipFillAmount(0f);
        CloseDialog(true);
    }

    private bool IsHoldSkipInputHeld()
    {
        if (InputHandler.instance != null && InputHandler.instance.IsInteractHeld)
            return true;

        Keyboard keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (keyboard.eKey.isPressed ||
                keyboard.spaceKey.isPressed ||
                keyboard.enterKey.isPressed ||
                keyboard.numpadEnterKey.isPressed)
            {
                return true;
            }
        }

        Mouse mouse = Mouse.current;

        if (mouse != null && mouse.leftButton.isPressed)
            return true;

        Gamepad gamepad = Gamepad.current;
        return gamepad != null && gamepad.buttonSouth.isPressed;
    }

    private bool HasHoldSkipPromptReferences()
    {
        ResolveHoldSkipPromptReferences();
        return holdSkipPromptRoot != null ||
               holdSkipIconImage != null ||
               holdSkipFillImage != null;
    }

    private void ResolveHoldSkipPromptReferences()
    {
        if (holdSkipPromptRoot == null && holdSkipIconImage != null)
            holdSkipPromptRoot = holdSkipIconImage.transform.parent != null
                ? holdSkipIconImage.transform.parent.gameObject
                : holdSkipIconImage.gameObject;

        if (holdSkipIconImage == null && holdSkipPromptRoot != null)
            holdSkipIconImage = FindImageInPromptRoot("HoldSkipIcon", "SkipHoldIcon", "DialogSkipIcon", "InputIcon", "Icon");

        if (holdSkipFillImage == null && holdSkipPromptRoot != null)
            holdSkipFillImage = FindImageInPromptRoot("HoldSkipFill", "SkipHoldFill", "DialogSkipFill", "InputFill", "Fill");

        if (holdSkipIconDatabase == null)
            holdSkipIconDatabase = FindFirstObjectByType<InputIconDatabase>(FindObjectsInactive.Include);
    }

    private Image FindImageInPromptRoot(params string[] _names)
    {
        if (holdSkipPromptRoot == null || _names == null)
            return null;

        for (int i = 0; i < _names.Length; i++)
        {
            Transform child = holdSkipPromptRoot.transform.Find(_names[i]);

            if (child != null && child.TryGetComponent(out Image image))
                return image;
        }

        return null;
    }

    private void ConfigureHoldSkipPrompt()
    {
        ResolveHoldSkipPromptReferences();

        if (holdSkipIconImage != null)
            holdSkipIconImage.raycastTarget = false;

        if (holdSkipFillImage == null)
            return;

        if (configureHoldSkipFillImage)
        {
            holdSkipFillImage.type = Image.Type.Filled;
            holdSkipFillImage.fillMethod = holdSkipFillMethod;
            holdSkipFillImage.fillOrigin = holdSkipFillOrigin;
        }

        if (syncHoldSkipFillSpriteWithIcon && holdSkipIconImage != null)
            holdSkipFillImage.sprite = holdSkipIconImage.sprite;

        holdSkipFillImage.color = holdSkipFillColor;
        holdSkipFillImage.raycastTarget = false;
        SetHoldSkipFillAmount(holdSkipDuration > 0f ? holdSkipProgress / holdSkipDuration : 0f);
    }

    private void UpdateHoldSkipIcon()
    {
        ResolveHoldSkipPromptReferences();

        if (holdSkipIconImage == null)
            return;

        if (holdSkipIconImage.GetComponent<InputIconUI>() != null)
        {
            ConfigureHoldSkipPrompt();
            return;
        }

        if (holdSkipIconDatabase == null)
        {
            holdSkipIconImage.enabled = false;
            LogMissingHoldSkipIcon();
            return;
        }

        Sprite icon = holdSkipIconDatabase.GetIcon(InputDeviceDetector.CurrentDeviceType, holdSkipIconAction);

        if (icon == null)
        {
            holdSkipIconImage.enabled = false;
            LogMissingHoldSkipIcon();
            return;
        }

        holdSkipIconImage.sprite = icon;
        holdSkipIconImage.color = Color.white;
        holdSkipIconImage.enabled = true;
        ConfigureHoldSkipPrompt();
    }

    private void SetHoldSkipPromptVisible(bool _visible)
    {
        bool shouldShow = _visible && useHoldSkipPrompt;

        ResolveHoldSkipPromptReferences();

        if (holdSkipPromptRoot != null)
            holdSkipPromptRoot.SetActive(shouldShow);

        if (holdSkipIconImage != null && holdSkipIconImage.gameObject != holdSkipPromptRoot)
            holdSkipIconImage.gameObject.SetActive(shouldShow);

        if (holdSkipFillImage != null && holdSkipFillImage.gameObject != holdSkipPromptRoot)
            holdSkipFillImage.gameObject.SetActive(shouldShow);

        if (!shouldShow)
            ResetHoldSkipProgress();
        else
            UpdateHoldSkipIcon();
    }

    private void ResetHoldSkipProgress()
    {
        holdSkipProgress = 0f;
        SetHoldSkipFillAmount(0f);
    }

    private void SetHoldSkipFillAmount(float _fillAmount)
    {
        if (holdSkipFillImage != null)
            holdSkipFillImage.fillAmount = Mathf.Clamp01(_fillAmount);
    }

    private void LogMissingHoldSkipIcon()
    {
        if (hasLoggedMissingHoldSkipIcon)
            return;

        Debug.LogWarning("[TextCanvaManager] Icone do hold-to-skip nao encontrado. Arraste um InputIconDatabase ou use um InputIconUI na imagem do botao.", this);
        hasLoggedMissingHoldSkipIcon = true;
    }

    private void PushDialogPresentationLocks()
    {
        if (hideHudDuringDialog && dialogHudModalToken == UIModalManager.InvalidToken)
        {
            UIModalRequest request = UIModalRequest.Create(
                this,
                _pauseTime: false,
                _hideHud: true,
                _blockPause: blockPauseDuringDialog,
                _lockCamera: false
            );
            request.extraHudRoots = extraHudRootsToHideDuringDialog;
            dialogHudModalToken = UIModalManager.PushModal(request);
        }

        if (lockInteractionsDuringDialog && !hasInteractionLock)
        {
            PlayerInteract.PushInteractionLock();
            hasInteractionLock = true;
        }
    }

    private void ReleaseDialogPresentationLocks()
    {
        if (dialogHudModalToken != UIModalManager.InvalidToken)
            UIModalManager.PopModal(ref dialogHudModalToken);

        if (hasInteractionLock)
        {
            PlayerInteract.PopInteractionLock();
            hasInteractionLock = false;
        }
    }

    private void NotifyDialogStarted()
    {
        DialogStarted?.Invoke(currentCameraFocusTarget);
    }

    private void NotifyDialogFinished()
    {
        DialogFinished?.Invoke();
        defaultCameraFocusTarget = null;
        currentCameraFocusTarget = null;
    }
}
