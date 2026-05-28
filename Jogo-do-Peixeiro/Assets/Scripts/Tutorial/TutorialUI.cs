using TMPro;
using UnityEngine;

public class TutorialUI : MonoBehaviour
{
    [SerializeField] private GameObject objectiveRoot;
    [SerializeField] private GameObject objectiveTitle;
    [SerializeField] private TMP_Text objectiveText;
    [SerializeField] private TMP_Text objectiveTitleText;
    [SerializeField] private DayCycle dayCycle;
    [SerializeField] private bool useDayCycleTextColors = true;
    [SerializeField] private bool hideObjectiveWhenNoQuestGuidance = true;
    [SerializeField] private bool clearObjectiveWhenAutoHidden = true;

    private bool isDayCycleSubscribed;

    private void OnEnable()
    {
        ResolveReferences();
        SubscribeDayCycleVisuals();
        ApplyObjectiveColors();
        HideObjectiveIfSceneHasNoQuestGuidance();
    }

    private void OnDisable()
    {
        UnsubscribeDayCycleVisuals();
    }

    public void SetObjectiveText(string _text)
    {
        if (objectiveText != null)
        {
            objectiveText.text = _text;
            ApplyObjectiveColors();
        }
    }

    public void SetObjectiveVisible(bool _visible)
    {
        if (objectiveRoot != null)
            objectiveRoot.SetActive(_visible);

        if (objectiveTitle != null && objectiveTitle != objectiveRoot)
            objectiveTitle.SetActive(_visible);

        if (objectiveText != null && objectiveText.gameObject != objectiveRoot)
            objectiveText.gameObject.SetActive(_visible);

        ApplyObjectiveColors();
    }

    public void ClearObjectiveText()
    {
        SetObjectiveText(string.Empty);
    }

    private void ResolveReferences()
    {
        if (objectiveTitleText == null && objectiveTitle != null)
            objectiveTitleText = objectiveTitle.GetComponentInChildren<TMP_Text>(true);

        if (dayCycle == null)
            dayCycle = FindFirstObjectByType<DayCycle>();
    }

    private void HideObjectiveIfSceneHasNoQuestGuidance()
    {
        if (!hideObjectiveWhenNoQuestGuidance)
            return;

        CampaignQuestGuidanceController questGuidance =
            FindFirstObjectByType<CampaignQuestGuidanceController>(FindObjectsInactive.Include);

        if (questGuidance != null)
            return;

        if (clearObjectiveWhenAutoHidden)
            ClearObjectiveText();

        SetObjectiveVisible(false);
    }

    private void SubscribeDayCycleVisuals()
    {
        if (!useDayCycleTextColors || isDayCycleSubscribed)
            return;

        if (dayCycle == null)
            dayCycle = FindFirstObjectByType<DayCycle>();

        if (dayCycle == null)
            return;

        dayCycle.VisualModeChanged += HandleDayCycleVisualModeChanged;
        isDayCycleSubscribed = true;
    }

    private void UnsubscribeDayCycleVisuals()
    {
        if (!isDayCycleSubscribed || dayCycle == null)
            return;

        dayCycle.VisualModeChanged -= HandleDayCycleVisualModeChanged;
        isDayCycleSubscribed = false;
    }

    private void HandleDayCycleVisualModeChanged(bool _isDayVisualMode)
    {
        ApplyObjectiveColors();
    }

    private void ApplyObjectiveColors()
    {
        if (!useDayCycleTextColors || dayCycle == null)
            return;

        if (objectiveTitleText != null)
            objectiveTitleText.color = dayCycle.PrimaryHudTextColor;

        if (objectiveText != null)
            objectiveText.color = dayCycle.SecondaryHudTextColor;
    }
}
