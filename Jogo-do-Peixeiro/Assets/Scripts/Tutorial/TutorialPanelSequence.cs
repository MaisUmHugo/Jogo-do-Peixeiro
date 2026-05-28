using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[Serializable]
public class TutorialPanelSlide
{
    public Sprite image;
    public string title;
    [TextArea] public string text;
}

public class TutorialPanelSequence : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject _panelRoot;
    [SerializeField] private Image _slideImage;
    [SerializeField] private TMP_Text _slideTitleText;
    [SerializeField] private TMP_Text _slideText;
    [SerializeField] private Button _continueButton;
    [SerializeField] private TMP_Text _continueButtonText;

    [Header("Slides")]
    [SerializeField] private TutorialPanelSlide[] _slides;

    [Header("Input")]
    [SerializeField] private bool _advanceWithContinueButton = true;
    [SerializeField] private bool _advanceWithInteractInput;
    [SerializeField] private bool _advanceWithAnyButton;
    [SerializeField] private string _continueText = "Continuar";
    [SerializeField] private float _inputDelay;

    [Header("Modal")]
    [SerializeField] private bool _hideHudWhileShowing = true;
    [SerializeField] private bool _blockPauseWhileShowing = true;
    [SerializeField] private bool _lockCameraWhileShowing = true;
    [SerializeField] private bool _lockGameplayWhileShowing = true;
    [SerializeField] private bool _lockInteractionsWhileShowing = true;
    [SerializeField] private bool _hideTutorialObjectiveWhileShowing = true;
    [SerializeField] private bool _keepContinueButtonSelected = true;

    private int _currentSlideIndex;
    private int _firstSlideIndex;
    private int _lastSlideIndex;
    private float _canAdvanceTime;
    private Action _onFinished;
    private bool _isShowing;
    private bool _isContinueButtonVisible;
    private int _modalToken = UIModalManager.InvalidToken;
    private GameManager.GameState _previousGameState;
    private bool _hasGameStateLock;
    private bool _hasInteractionLock;
    private TutorialUI[] _hiddenTutorialUis;
    private bool[] _hiddenTutorialUiWasVisible;

    public bool IsShowing => _isShowing;

    private void OnValidate()
    {
        _inputDelay = Mathf.Max(0f, _inputDelay);
    }

    private void Awake()
    {
        EnsureContinueButtonReady();
        SetContinueButtonVisible(false);
        SetPanelActive(false);
    }

    private void OnEnable()
    {
        EnsureContinueButtonReady();
    }

    private void OnDisable()
    {
        UnsubscribeInput();
        SetContinueButtonVisible(false);
        ReleaseSlideState();
    }

    private void Update()
    {
        if (!_isShowing || !_advanceWithContinueButton || _continueButton == null)
            return;

        if (!_isContinueButtonVisible && Time.unscaledTime >= _canAdvanceTime)
            SetContinueButtonVisible(true);

        KeepContinueButtonSelected();
    }

    public void Show(Action _finishedCallback = null)
    {
        ShowRange(0, _slides != null ? _slides.Length : 0, _finishedCallback);
    }

    public void ShowRange(int _startIndex, int _count, Action _finishedCallback = null)
    {
        if (!enabled)
            enabled = true;

        EnsureContinueButtonReady();
        _onFinished = _finishedCallback;

        if (_slides == null || _slides.Length == 0)
        {
            Finish();
            return;
        }

        _firstSlideIndex = Mathf.Clamp(_startIndex, 0, _slides.Length - 1);
        int slideCount = Mathf.Max(1, _count);
        _lastSlideIndex = Mathf.Clamp(_firstSlideIndex + slideCount - 1, _firstSlideIndex, _slides.Length - 1);

        _isShowing = true;
        _currentSlideIndex = _firstSlideIndex;
        _canAdvanceTime = Time.unscaledTime + _inputDelay;
        SetPanelActive(true);
        PushSlideState();
        RefreshSlide();
        RefreshContinueButtonAvailability();
        SubscribeInput();
    }

    public void Next()
    {
        if (!_isShowing)
            return;

        if (Time.unscaledTime < _canAdvanceTime)
            return;

        if (_currentSlideIndex >= _lastSlideIndex)
        {
            Finish();
            return;
        }

        _currentSlideIndex++;
        _canAdvanceTime = Time.unscaledTime + _inputDelay;
        RefreshSlide();
        RefreshContinueButtonAvailability();
    }

    public void Hide()
    {
        _isShowing = false;
        _onFinished = null;
        UnsubscribeInput();
        SetContinueButtonVisible(false);
        SetPanelActive(false);
        ReleaseSlideState();
    }

    private void Finish()
    {
        Action callback = _onFinished;
        _onFinished = null;
        _isShowing = false;
        UnsubscribeInput();
        SetContinueButtonVisible(false);
        SetPanelActive(false);
        ReleaseSlideState();
        callback?.Invoke();
    }

    private void PushSlideState()
    {
        LockGameplayForSlide();

        if (_modalToken == UIModalManager.InvalidToken &&
            (_hideHudWhileShowing || _blockPauseWhileShowing || _lockCameraWhileShowing))
        {
            UIModalRequest request = UIModalRequest.Create(
                this,
                false,
                _hideHudWhileShowing,
                _blockPauseWhileShowing,
                _lockCameraWhileShowing);

            _modalToken = UIModalManager.PushModal(request);
        }

        if (_lockInteractionsWhileShowing && !_hasInteractionLock)
        {
            PlayerInteract.PushInteractionLock();
            _hasInteractionLock = true;
        }

        HideTutorialObjectivesForSlide();
    }

    private void ReleaseSlideState()
    {
        RestoreTutorialObjectivesAfterSlide();

        if (_hasInteractionLock)
        {
            PlayerInteract.PopInteractionLock();
            _hasInteractionLock = false;
        }

        if (_modalToken != UIModalManager.InvalidToken)
            UIModalManager.PopModal(ref _modalToken);

        RestoreGameplayAfterSlide();
    }

    private void LockGameplayForSlide()
    {
        if (!_lockGameplayWhileShowing || _hasGameStateLock || GameManager.instance == null)
            return;

        _previousGameState = GameManager.instance.currentState;
        _hasGameStateLock = true;
        GameManager.instance.SetState(GameManager.GameState.InUI);
        InputHandler.instance?.ResetGameplayInput();
    }

    private void RestoreGameplayAfterSlide()
    {
        if (!_hasGameStateLock)
            return;

        InputHandler.instance?.ResetGameplayInput();

        if (GameManager.instance != null && GameManager.instance.currentState == GameManager.GameState.InUI)
            GameManager.instance.SetState(_previousGameState);

        _hasGameStateLock = false;
    }

    private void HideTutorialObjectivesForSlide()
    {
        if (!_hideTutorialObjectiveWhileShowing || _hiddenTutorialUis != null)
            return;

        _hiddenTutorialUis = FindObjectsByType<TutorialUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        _hiddenTutorialUiWasVisible = new bool[_hiddenTutorialUis.Length];

        for (int i = 0; i < _hiddenTutorialUis.Length; i++)
        {
            TutorialUI tutorialUi = _hiddenTutorialUis[i];

            if (tutorialUi == null)
                continue;

            _hiddenTutorialUiWasVisible[i] = tutorialUi.IsObjectiveVisible;

            if (_hiddenTutorialUiWasVisible[i])
                tutorialUi.SetObjectiveVisible(false);
        }
    }

    private void RestoreTutorialObjectivesAfterSlide()
    {
        if (_hiddenTutorialUis == null || _hiddenTutorialUiWasVisible == null)
            return;

        int count = Mathf.Min(_hiddenTutorialUis.Length, _hiddenTutorialUiWasVisible.Length);

        for (int i = 0; i < count; i++)
        {
            if (_hiddenTutorialUis[i] != null && _hiddenTutorialUiWasVisible[i])
                _hiddenTutorialUis[i].SetObjectiveVisible(true);
        }

        _hiddenTutorialUis = null;
        _hiddenTutorialUiWasVisible = null;
    }

    private void RefreshSlide()
    {
        TutorialPanelSlide slide = _slides[_currentSlideIndex];

        if (_slideImage != null)
        {
            _slideImage.sprite = slide.image;
            _slideImage.preserveAspect = true;
            _slideImage.gameObject.SetActive(slide.image != null);
        }

        if (_slideTitleText != null)
            _slideTitleText.text = string.IsNullOrWhiteSpace(slide.title) ? "COMO JOGAR?" : slide.title;

        if (_slideText != null)
            _slideText.text = slide.text;
    }

    private void SubscribeInput()
    {
        if (InputHandler.instance == null)
            return;

        if (_advanceWithInteractInput)
        {
            InputHandler.instance.onInteractPressed -= Next;
            InputHandler.instance.onInteractPressed += Next;
        }

        if (_advanceWithAnyButton)
        {
            InputHandler.instance.onAnyButtonPressed -= Next;
            InputHandler.instance.onAnyButtonPressed += Next;
        }
    }

    private void UnsubscribeInput()
    {
        if (InputHandler.instance == null)
            return;

        InputHandler.instance.onInteractPressed -= Next;
        InputHandler.instance.onAnyButtonPressed -= Next;
    }

    private void ResolveContinueButton()
    {
        if (_continueButton != null)
        {
            ResolveContinueButtonText();
            return;
        }

        Transform root = _panelRoot != null ? _panelRoot.transform : transform;
        _continueButton = FindContinueButton(root);
        ResolveContinueButtonText();
    }

    private void ResolveContinueButtonText()
    {
        if (_continueButton == null)
            return;

        if (_continueButtonText == null || !_continueButtonText.transform.IsChildOf(_continueButton.transform))
            _continueButtonText = _continueButton.GetComponentInChildren<TMP_Text>(true);
    }

    private Button FindContinueButton(Transform _root)
    {
        if (_root == null)
            return null;

        Button[] buttons = _root.GetComponentsInChildren<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null)
                continue;

            string buttonName = buttons[i].name.ToLowerInvariant();

            if (buttonName.Contains("continuar") || buttonName.Contains("continue"))
                return buttons[i];
        }

        return buttons.Length > 0 ? buttons[0] : null;
    }

    private void EnsureContinueButtonReady()
    {
        ResolveContinueButton();

        if (_continueButtonText != null)
            _continueButtonText.text = _continueText;

        if (_continueButton == null)
            return;

        _continueButton.onClick.RemoveListener(Next);
        _continueButton.onClick.AddListener(Next);
    }

    private void SetContinueButtonVisible(bool _visible)
    {
        _isContinueButtonVisible = _visible;

        if (_continueButton == null)
            return;

        _continueButton.gameObject.SetActive(_visible);
        _continueButton.interactable = _visible;

        if (_continueButtonText != null)
            _continueButtonText.text = _continueText;

        if (_visible)
            UISelectionHelper.Select(_continueButton, _panelRoot);
    }

    private void RefreshContinueButtonAvailability()
    {
        bool canShowContinueButton = _advanceWithContinueButton && Time.unscaledTime >= _canAdvanceTime;
        SetContinueButtonVisible(canShowContinueButton);
    }

    private void KeepContinueButtonSelected()
    {
        if (!_keepContinueButtonSelected ||
            !_isContinueButtonVisible ||
            !UISelectionHelper.IsUsable(_continueButton))
        {
            return;
        }

        if (UISelectionHelper.CurrentSelectableInScope(_panelRoot) != _continueButton)
            UISelectionHelper.Select(_continueButton, _panelRoot);
    }

    private void SetPanelActive(bool _active)
    {
        if (_active && !gameObject.activeSelf)
            gameObject.SetActive(true);

        if (_panelRoot != null)
        {
            _panelRoot.SetActive(_active);
            return;
        }

        if (gameObject.activeSelf != _active)
            gameObject.SetActive(_active);
    }
}
