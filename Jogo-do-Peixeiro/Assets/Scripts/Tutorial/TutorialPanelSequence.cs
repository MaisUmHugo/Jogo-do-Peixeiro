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

    [Header("Slides")]
    [SerializeField] private TutorialPanelSlide[] _slides;

    [Header("Input")]
    [SerializeField] private bool _advanceWithAnyButton = true;
    [SerializeField] private float _inputDelay = 0.15f;

    private int _currentSlideIndex;
    private float _canAdvanceTime;
    private Action _onFinished;
    private bool _isShowing;

    public bool IsShowing => _isShowing;

    private void Awake()
    {
        SetPanelActive(false);
    }

    private void OnDisable()
    {
        UnsubscribeInput();
    }

    public void Show(Action _finishedCallback = null)
    {
        _onFinished = _finishedCallback;

        if (_slides == null || _slides.Length == 0)
        {
            Finish();
            return;
        }

        _isShowing = true;
        _currentSlideIndex = 0;
        _canAdvanceTime = Time.unscaledTime + _inputDelay;
        SetPanelActive(true);
        RefreshSlide();
        SubscribeInput();
    }

    public void Next()
    {
        if (!_isShowing)
            return;

        if (Time.unscaledTime < _canAdvanceTime)
            return;

        if (_currentSlideIndex >= _slides.Length - 1)
        {
            Finish();
            return;
        }

        _currentSlideIndex++;
        _canAdvanceTime = Time.unscaledTime + _inputDelay;
        RefreshSlide();
    }

    public void Hide()
    {
        _isShowing = false;
        _onFinished = null;
        UnsubscribeInput();
        SetPanelActive(false);
    }

    private void Finish()
    {
        Action callback = _onFinished;
        _onFinished = null;
        _isShowing = false;
        UnsubscribeInput();
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
        if (!_advanceWithAnyButton || InputHandler.instance == null)
            return;

        InputHandler.instance.onAnyButtonPressed -= Next;
        InputHandler.instance.onAnyButtonPressed += Next;
    }

    private void UnsubscribeInput()
    {
        if (InputHandler.instance == null)
            return;

        InputHandler.instance.onAnyButtonPressed -= Next;
    }

    private void SetPanelActive(bool _active)
    {
        if (_panelRoot != null)
            _panelRoot.SetActive(_active);
        else
            gameObject.SetActive(_active);
    }
}
