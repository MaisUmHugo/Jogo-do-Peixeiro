using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HUDWarningUI : MonoBehaviour
{
    private static HUDWarningUI instance;

    public static HUDWarningUI Instance
    {
        get
        {
            if (instance == null)
                instance = FindFirstObjectByType<HUDWarningUI>(FindObjectsInactive.Include);

            return instance;
        }
        private set => instance = value;
    }

    [Header("References")]
    [SerializeField] private TMP_Text _messageText;
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("Settings")]
    [SerializeField] private float _visibleTime = 1.5f;
    [SerializeField] private float _minimumVisibleTime = 2.5f;
    [SerializeField] private float _visibleTimePerCharacter = 0.025f;
    [SerializeField] private float _maxVisibleTime = 5f;
    [SerializeField] private float _fadeSpeed = 4f;
    [SerializeField] private bool _useUnscaledTime = true;
    [SerializeField] private bool _activateHierarchyWhenShown = true;
    [SerializeField] private bool _ignoreConsecutiveDuplicates = true;

    private readonly Queue<string> _messageQueue = new();
    private Coroutine _messageRoutine;
    private bool _isShowingMessage;
    private bool _hasPersistentMessage;
    private string _currentMessage;
    private string _lastQueuedMessage;
    private string _persistentMessage;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        AutoAssignReferences();
        ClearVisual();
    }

    private void OnEnable()
    {
        if (Instance == null)
            Instance = this;

        AutoAssignReferences();
    }

    private void OnValidate()
    {
        _visibleTime = Mathf.Max(0f, _visibleTime);
        _minimumVisibleTime = Mathf.Max(0f, _minimumVisibleTime);
        _visibleTimePerCharacter = Mathf.Max(0f, _visibleTimePerCharacter);
        _maxVisibleTime = Mathf.Max(_minimumVisibleTime, _maxVisibleTime);
        _fadeSpeed = Mathf.Max(0.01f, _fadeSpeed);
    }

    public void ShowWarning(string _message)
    {
        if (string.IsNullOrWhiteSpace(_message))
            return;

        EnsureCanShow();
        AutoAssignReferences();

        if (_messageText == null)
        {
            Debug.LogWarning("HUDWarningUI sem TMP_Text configurado.");
            return;
        }

        if (ShouldIgnoreDuplicate(_message))
            return;

        _messageQueue.Enqueue(_message);
        _lastQueuedMessage = _message;

        if (!_isShowingMessage)
            _messageRoutine = StartCoroutine(ProcessMessageQueue());
    }

    public void ShowPersistentWarning(string _message)
    {
        if (string.IsNullOrWhiteSpace(_message))
            return;

        EnsureCanShow();
        AutoAssignReferences();

        if (_messageText == null)
        {
            Debug.LogWarning("HUDWarningUI sem TMP_Text configurado.");
            return;
        }

        _hasPersistentMessage = true;
        _persistentMessage = _message;
        _messageQueue.Clear();

        if (_messageRoutine != null)
            StopCoroutine(_messageRoutine);

        _messageRoutine = null;
        _isShowingMessage = false;
        _lastQueuedMessage = null;
        ApplyPersistentWarningVisual();
    }

    public void ClearPersistentWarning(string _message = null)
    {
        if (!_hasPersistentMessage)
            return;

        if (!string.IsNullOrWhiteSpace(_message) && _persistentMessage != _message)
            return;

        string clearedMessage = _persistentMessage;
        _hasPersistentMessage = false;
        _persistentMessage = null;

        if (_currentMessage == clearedMessage)
            _currentMessage = null;

        if (!_isShowingMessage && _messageQueue.Count == 0)
            ClearVisual();
    }

    private IEnumerator ProcessMessageQueue()
    {
        _isShowingMessage = true;

        while (_messageQueue.Count > 0)
        {
            string message = _messageQueue.Dequeue();
            _currentMessage = message;

            yield return ShowMessageRoutine(message);
        }

        _lastQueuedMessage = null;
        _isShowingMessage = false;
        _messageRoutine = null;

        if (_hasPersistentMessage)
        {
            ApplyPersistentWarningVisual();
            yield break;
        }

        _currentMessage = null;
    }

    private IEnumerator ShowMessageRoutine(string _message)
    {
        _messageText.text = _message;
        SetCanvasVisible(true);

        yield return WaitForSeconds(GetVisibleDuration(_message));

        if (_canvasGroup != null)
        {
            while (_canvasGroup.alpha > 0f)
            {
                _canvasGroup.alpha -= _fadeSpeed * GetDeltaTime();
                yield return null;
            }

            _canvasGroup.alpha = 0f;
        }

        _messageText.text = string.Empty;
    }

    private void ClearVisual()
    {
        if (_messageText != null)
            _messageText.text = string.Empty;

        SetCanvasVisible(false);
    }

    public void ClearQueue()
    {
        _messageQueue.Clear();
        _hasPersistentMessage = false;
        _persistentMessage = null;

        if (_messageRoutine != null)
            StopCoroutine(_messageRoutine);

        _messageRoutine = null;
        _isShowingMessage = false;
        _currentMessage = null;
        _lastQueuedMessage = null;

        ClearVisual();
    }

    private bool ShouldIgnoreDuplicate(string _message)
    {
        if (!_ignoreConsecutiveDuplicates)
            return false;

        return _message == _currentMessage ||
               _message == _lastQueuedMessage ||
               (_hasPersistentMessage && _message == _persistentMessage);
    }

    private void EnsureCanShow()
    {
        if (!_activateHierarchyWhenShown)
            return;

        Transform current = transform;

        while (current != null)
        {
            if (!current.gameObject.activeSelf)
                current.gameObject.SetActive(true);

            current = current.parent;
        }

        if (!enabled)
            enabled = true;
    }

    private void AutoAssignReferences()
    {
        if (_messageText == null)
            _messageText = GetComponentInChildren<TMP_Text>(true);

        if (_canvasGroup == null)
            _canvasGroup = GetComponent<CanvasGroup>();

        if (_canvasGroup == null)
            _canvasGroup = GetComponentInChildren<CanvasGroup>(true);

        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
    }

    private void SetCanvasVisible(bool _visible)
    {
        if (_canvasGroup == null)
            return;

        _canvasGroup.alpha = _visible ? 1f : 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
    }

    private void ApplyPersistentWarningVisual()
    {
        if (_messageText == null)
            return;

        _currentMessage = _persistentMessage;
        _messageText.text = _persistentMessage;
        SetCanvasVisible(true);
    }

    private float GetVisibleDuration(string _message)
    {
        float messageDuration = _message.Length * _visibleTimePerCharacter;
        float duration = Mathf.Max(_visibleTime, _minimumVisibleTime, messageDuration);
        return Mathf.Min(duration, _maxVisibleTime);
    }

    private float GetDeltaTime()
    {
        return _useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    private IEnumerator WaitForSeconds(float _duration)
    {
        if (!_useUnscaledTime)
        {
            yield return new WaitForSeconds(_duration);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < _duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }
}
