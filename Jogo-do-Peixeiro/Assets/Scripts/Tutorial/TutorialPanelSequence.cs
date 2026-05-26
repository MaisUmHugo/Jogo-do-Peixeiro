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
    [SerializeField] private float _inputDelay = 2f;

    private int _currentSlideIndex;
    private int _firstSlideIndex;
    private int _lastSlideIndex;
    private float _canAdvanceTime;
    private Action _onFinished;
    private bool _isShowing;
    private bool _isContinueButtonVisible;

    public bool IsShowing => _isShowing;

    private void OnValidate()
    {
        _inputDelay = Mathf.Max(0f, _inputDelay);
    }

    private void Awake()
    {
        ResolveContinueButton();
        ConfigureContinueButton();
        SetPanelActive(false);
    }

    private void OnDisable()
    {
        UnsubscribeInput();
        SetContinueButtonVisible(false);
    }

    private void Update()
    {
        if (!_isShowing || !_advanceWithContinueButton || _continueButton == null)
            return;

        if (!_isContinueButtonVisible && Time.unscaledTime >= _canAdvanceTime)
            SetContinueButtonVisible(true);
    }

    public void Show(Action _finishedCallback = null)
    {
        ShowRange(0, _slides != null ? _slides.Length : 0, _finishedCallback);
    }

    public void ShowRange(int _startIndex, int _count, Action _finishedCallback = null)
    {
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
        SetContinueButtonVisible(false);
        RefreshSlide();
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
        SetContinueButtonVisible(false);
        RefreshSlide();
    }

    public void Hide()
    {
        _isShowing = false;
        _onFinished = null;
        UnsubscribeInput();
        SetContinueButtonVisible(false);
        SetPanelActive(false);
    }

    private void Finish()
    {
        Action callback = _onFinished;
        _onFinished = null;
        _isShowing = false;
        UnsubscribeInput();
        SetContinueButtonVisible(false);
        SetPanelActive(false);
        callback?.Invoke();
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
            return;

        Transform root = _panelRoot != null ? _panelRoot.transform : transform;
        _continueButton = FindContinueButton(root);

        if (_continueButton != null && _continueButtonText == null)
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

    private void ConfigureContinueButton()
    {
        if (_continueButtonText != null)
            _continueButtonText.text = _continueText;

        if (_continueButton == null)
            return;

        _continueButton.onClick.RemoveListener(Next);
        _continueButton.onClick.AddListener(Next);
        SetContinueButtonVisible(false);
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

    private void SetPanelActive(bool _active)
    {
        if (_panelRoot != null)
            _panelRoot.SetActive(_active);
        else
            gameObject.SetActive(_active);
    }
}
