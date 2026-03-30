using UnityEngine;

public class MoneyLenderController : MonoBehaviour, IInteractable
{
    [Header("References")]
    [SerializeField] private MoneyLender moneyLender;
    [SerializeField] private MoneyLenderUI moneyLenderUI;

    public void Interact()
    {
        if (moneyLender == null || moneyLenderUI == null)
            return;

        moneyLenderUI.Open(moneyLender);
        if (TutorialHandler.Instance.isFinishedTalk == false)
        {
            TutorialHandler.Instance.isFinishedTalk = true;
            TutorialHandler.Instance.GoNextObjective();

        }
    }

    public int GetInteractionPriority()
    {
        return 50;
    }
}