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
    }

    public int GetInteractionPriority()
    {
        return 50;
    }
}