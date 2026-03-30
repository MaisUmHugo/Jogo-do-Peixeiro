using System.Collections;
using TMPro;
using UnityEngine;

public class FishSkillCheckUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FishSkillCheck fishSkillCheck;

    [Header("Progress Bar")]
    [SerializeField] private RectTransform progressBarArea;
    [SerializeField] private RectTransform progressBarFill;

    [Header("Timing Bar")]
    [SerializeField] private RectTransform timingBarArea;
    [SerializeField] private RectTransform successZone;
    [SerializeField] private RectTransform indicator;

    [Header("Feedback Text")]
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private float feedbackDuration = 0.45f;

    [Header("Shake")]
    [SerializeField] private float shakeDuration = 0.18f;
    [SerializeField] private float shakeStrength = 10f;

    private Vector2 timingBarOriginalPosition;
    private Coroutine feedbackRoutine;
    private Coroutine shakeRoutine;

    private void Start()
    {
        if (timingBarArea != null)
            timingBarOriginalPosition = timingBarArea.anchoredPosition;

        if (feedbackText != null)
            feedbackText.gameObject.SetActive(false);

        if (fishSkillCheck != null)
        {
            fishSkillCheck.OnFeedbackTriggered += HandleFeedback;
            fishSkillCheck.OnFailShake += HandleFailShake;
        }
    }

    private void OnDestroy()
    {
        if (fishSkillCheck != null)
        {
            fishSkillCheck.OnFeedbackTriggered -= HandleFeedback;
            fishSkillCheck.OnFailShake -= HandleFailShake;
        }
    }

    private void Update()
    {
        if (fishSkillCheck == null)
            return;

        UpdateProgressBar();
        UpdateTimingBar();
    }

    private void UpdateProgressBar()
    {
        if (progressBarArea == null || progressBarFill == null)
            return;

        float width = progressBarArea.rect.width;
        float fillWidth = fishSkillCheck.ProgressNormalized * width;

        progressBarFill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, fillWidth);

        float fillPosX = (fillWidth * 0.5f) - (width * 0.5f);
        progressBarFill.anchoredPosition = new Vector2(fillPosX, progressBarFill.anchoredPosition.y);
    }

    private void UpdateTimingBar()
    {
        if (timingBarArea == null || successZone == null || indicator == null)
            return;

        float width = timingBarArea.rect.width;

        float zoneStart = fishSkillCheck.SuccessZoneStartNormalized;
        float zoneEnd = fishSkillCheck.SuccessZoneEndNormalized;

        float zoneWidth = (zoneEnd - zoneStart) * width;
        float zoneCenterX = ((zoneStart + zoneEnd) * 0.5f - 0.5f) * width;

        successZone.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, zoneWidth);
        successZone.anchoredPosition = new Vector2(zoneCenterX, successZone.anchoredPosition.y);

        float indicatorX = (fishSkillCheck.IndicatorNormalized - 0.5f) * width;
        indicator.anchoredPosition = new Vector2(indicatorX, indicator.anchoredPosition.y);
    }

    private void HandleFeedback(FishSkillCheck.FeedbackResult resultado)
    {
        switch (resultado)
        {
            case FishSkillCheck.FeedbackResult.Terrible:
                ShowFeedback("Horrível", new Color(0.7f, 0.1f, 0.1f));
                break;

            case FishSkillCheck.FeedbackResult.Bad:
                ShowFeedback("Ruim", Color.red);
                break;

            case FishSkillCheck.FeedbackResult.Near:
                ShowFeedback("Quase", new Color(1f, 0.5f, 0f));
                break;

            case FishSkillCheck.FeedbackResult.Good:
                ShowFeedback("Bom", Color.white);
                break;

            case FishSkillCheck.FeedbackResult.Great:
                ShowFeedback("Ótimo", Color.yellow);
                break;

            case FishSkillCheck.FeedbackResult.Perfect:
                ShowFeedback("Perfeito", Color.green);
                break;
        }
    }

    private void HandleFailShake()
    {
        if (timingBarArea == null)
            return;

        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(ShakeTimingBarRoutine());
    }

    private void ShowFeedback(string text, Color color)
    {
        if (feedbackText == null)
            return;

        if (feedbackRoutine != null)
            StopCoroutine(feedbackRoutine);

        feedbackRoutine = StartCoroutine(ShowFeedbackRoutine(text, color));
    }

    private IEnumerator ShowFeedbackRoutine(string text, Color color)
    {
        feedbackText.gameObject.SetActive(true);
        feedbackText.text = text;
        feedbackText.color = color;

        yield return new WaitForSeconds(feedbackDuration);

        feedbackText.gameObject.SetActive(false);
    }

    private IEnumerator ShakeTimingBarRoutine()
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;

            float offsetX = Random.Range(-shakeStrength, shakeStrength);
            float offsetY = Random.Range(-shakeStrength * 0.2f, shakeStrength * 0.2f);

            timingBarArea.anchoredPosition = timingBarOriginalPosition + new Vector2(offsetX, offsetY);

            yield return null;
        }

        timingBarArea.anchoredPosition = timingBarOriginalPosition;
    }
}