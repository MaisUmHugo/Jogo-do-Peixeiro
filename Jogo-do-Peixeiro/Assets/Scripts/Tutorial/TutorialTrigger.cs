using UnityEngine;

public class TutorialTrigger : MonoBehaviour
{
    public enum TriggerType
    {
        MoneyLenderCabin,
        FishingSpot,
        Dock
    }

    [SerializeField] private TriggerType triggerType;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (CampaignQuestGuidanceController.instance == null)
            return;

        switch (triggerType)
        {
            case TriggerType.MoneyLenderCabin:
                CampaignQuestGuidanceController.instance.NotifyReachedMoneyLenderCabin();
                break;

            case TriggerType.FishingSpot:
                CampaignQuestGuidanceController.instance.NotifyReachedFishingSpot();
                break;

            case TriggerType.Dock:
                CampaignQuestGuidanceController.instance.NotifyReturnedToDock();
                break;
        }
    }
}