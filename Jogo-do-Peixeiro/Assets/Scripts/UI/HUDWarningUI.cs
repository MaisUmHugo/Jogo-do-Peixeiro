using System.Collections;
using TMPro;
using UnityEngine;

public class HUDWarningUI : MonoBehaviour
{
    public static HUDWarningUI Instance { get; private set; }

    [Header("References")]
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Settings")]
    [SerializeField] private float visibleTime = 1.5f;
    [SerializeField] private float fadeSpeed = 8f;

    private Coroutine messageRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (messageText != null)
            messageText.text = string.Empty;

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    public void ShowWarning(string _message)
    {
        if (messageRoutine != null)
            StopCoroutine(messageRoutine);

        messageRoutine = StartCoroutine(ShowMessageRoutine(_message));
    }

    private IEnumerator ShowMessageRoutine(string _message)
    {
        if (messageText != null)
            messageText.text = _message;

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        yield return new WaitForSeconds(visibleTime);

        if (canvasGroup != null)
        {
            while (canvasGroup.alpha > 0f)
            {
                canvasGroup.alpha -= fadeSpeed * Time.deltaTime;
                yield return null;
            }

            canvasGroup.alpha = 0f;
        }

        if (messageText != null)
            messageText.text = string.Empty;
    }
}