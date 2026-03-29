using UnityEngine;

public class FishSkillCheckUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private FishSkillCheck fishSkillCheck;
    [SerializeField] private RectTransform barArea;
    [SerializeField] private RectTransform successZone;
    [SerializeField] private RectTransform indicator;

    private void Update()
    {
        if (fishSkillCheck == null || barArea == null || successZone == null || indicator == null)
            return;

        UpdateSuccessZone();
        UpdateIndicator();
    }

    private void UpdateSuccessZone()
    {
        float barWidth = barArea.rect.width;

        float zoneStart = fishSkillCheck.SuccessZoneStartNormalized;
        float zoneEnd = fishSkillCheck.SuccessZoneEndNormalized;

        float zoneWidth = (zoneEnd - zoneStart) * barWidth;
        float zoneCenterX = ((zoneStart + zoneEnd) * 0.5f - 0.5f) * barWidth;

        successZone.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, zoneWidth);
        successZone.anchoredPosition = new Vector2(zoneCenterX, successZone.anchoredPosition.y);
    }

    private void UpdateIndicator()
    {
        float barWidth = barArea.rect.width;

        float indicatorX = (fishSkillCheck.IndicatorNormalized - 0.5f) * barWidth;

        indicator.anchoredPosition = new Vector2(indicatorX, indicator.anchoredPosition.y);
    }
}