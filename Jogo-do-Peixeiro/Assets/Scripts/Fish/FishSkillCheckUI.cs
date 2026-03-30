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
}