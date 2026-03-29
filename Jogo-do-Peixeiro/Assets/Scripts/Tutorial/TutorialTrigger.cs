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

        if (TutorialController.instance == null)
            return;

        switch (triggerType)
        {
            case TriggerType.MoneyLenderCabin:
                TutorialController.instance.NotifyReachedMoneyLenderCabin();
                break;

            case TriggerType.FishingSpot:
                TutorialController.instance.NotifyReachedFishingSpot();
                break;

            case TriggerType.Dock:
                TutorialController.instance.NotifyReturnedToDock();
                break;
        }
    }
}