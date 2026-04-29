using UnityEngine;

public class MoneyLenderController : MonoBehaviour, IInteractable
{
    [Header("References")]
    [SerializeField] private MoneyLender moneyLender;
    [SerializeField] private PaymentUI paymentUI;
    [SerializeField] private MoneyLenderUI moneyLenderUI;

    public void Interact()
    {
        if (moneyLender == null)
            return;

        if (TutorialEvents.TryHandleMoneyLenderInteraction(moneyLender, paymentUI, moneyLenderUI))
            return;

        if (paymentUI != null)
        {
            paymentUI.Open(moneyLender);
            return;
        }

        if (moneyLenderUI != null)
            moneyLenderUI.Open(moneyLender);
    }

    public int GetInteractionPriority()
    {
        return 50;
    }
}
