using System.Collections;
using TMPro;
using UnityEngine;

public class HUDFishInfoUI : MonoBehaviour
{
    public static HUDFishInfoUI Instance { get; private set; }

    [Header("References")]
    [SerializeField] private TMP_Text fishInfoText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Settings")]
    [SerializeField] private float visibleTime = 1.2f;
    [SerializeField] private float fadeSpeed = 10f;

    private Coroutine messageRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (fishInfoText != null)
            fishInfoText.text = string.Empty;

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    public void ShowFishInfo(string _fishName, int _weight)
    {
        if (messageRoutine != null)
            StopCoroutine(messageRoutine);

        messageRoutine = StartCoroutine(ShowFishInfoRoutine($"{_fishName} +{_weight}kg"));
    }

    private IEnumerator ShowFishInfoRoutine(string _message)
    {
        if (fishInfoText != null)
            fishInfoText.text = _message;

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

        if (fishInfoText != null)
            fishInfoText.text = string.Empty;
    }
}