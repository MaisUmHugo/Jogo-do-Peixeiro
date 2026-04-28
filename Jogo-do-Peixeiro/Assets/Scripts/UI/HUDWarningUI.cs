using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HUDWarningUI : MonoBehaviour
{
    public static HUDWarningUI Instance { get; private set; }

    [Header("References")]
    [SerializeField] private TMP_Text _messageText;
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("Settings")]
    [SerializeField] private float _visibleTime = 1.5f;
    [SerializeField] private float _fadeSpeed = 8f;

    private readonly Queue<string> _messageQueue = new();
    private Coroutine _messageRoutine;
    private bool _isShowingMessage;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ClearVisual();
    }

    public void ShowWarning(string _message)
    {
        if (string.IsNullOrWhiteSpace(_message))
            return;

        _messageQueue.Enqueue(_message);

        if (!_isShowingMessage)
            _messageRoutine = StartCoroutine(ProcessMessageQueue());
    }

    private IEnumerator ProcessMessageQueue()
    {
        _isShowingMessage = true;

        while (_messageQueue.Count > 0)
        {
            string message = _messageQueue.Dequeue();

            yield return ShowMessageRoutine(message);
        }

        _isShowingMessage = false;
        _messageRoutine = null;
    }

    private IEnumerator ShowMessageRoutine(string _message)
    {
        if (_messageText != null)
            _messageText.text = _message;

        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(_visibleTime);

        if (_canvasGroup != null)
        {
            while (_canvasGroup.alpha > 0f)
            {
                _canvasGroup.alpha -= _fadeSpeed * Time.deltaTime;
                yield return null;
            }

            _canvasGroup.alpha = 0f;
        }

        if (_messageText != null)
            _messageText.text = string.Empty;
    }

    private void ClearVisual()
    {
        if (_messageText != null)
            _messageText.text = string.Empty;

        if (_canvasGroup != null)
            _canvasGroup.alpha = 0f;
    }

    public void ClearQueue()
    {
        _messageQueue.Clear();

        if (_messageRoutine != null)
            StopCoroutine(_messageRoutine);

        _messageRoutine = null;
        _isShowingMessage = false;

        ClearVisual();
    }
}