using System;

public static class TutorialEvents
{
    public delegate bool MoneyLenderInteractionHandler(MoneyLender _moneyLender, PaymentUI _paymentUI, MoneyLenderUI _moneyLenderUI);

    public static event MoneyLenderInteractionHandler MoneyLenderInteractionRequested;
    public static event Func<bool> BoatEntryBlockRequested;
    public static event Action BoatEntryBlocked;
    public static event Action BoatEntered;
    public static event Action BoatExited;
    public static event Action<FishData, ShipInventory> FishCaught;

    public static bool TryHandleMoneyLenderInteraction(MoneyLender _moneyLender)
    {
        return TryHandleMoneyLenderInteraction(_moneyLender, null, null);
    }

    public static bool TryHandleMoneyLenderInteraction(MoneyLender _moneyLender, MoneyLenderUI _moneyLenderUI)
    {
        return TryHandleMoneyLenderInteraction(_moneyLender, null, _moneyLenderUI);
    }

    public static bool TryHandleMoneyLenderInteraction(MoneyLender _moneyLender, PaymentUI _paymentUI)
    {
        return TryHandleMoneyLenderInteraction(_moneyLender, _paymentUI, null);
    }

    public static bool TryHandleMoneyLenderInteraction(MoneyLender _moneyLender, PaymentUI _paymentUI, MoneyLenderUI _moneyLenderUI)
    {
        if (MoneyLenderInteractionRequested == null)
            return false;

        foreach (MoneyLenderInteractionHandler handler in MoneyLenderInteractionRequested.GetInvocationList())
        {
            if (handler != null && handler.Invoke(_moneyLender, _paymentUI, _moneyLenderUI))
                return true;
        }

        return false;
    }

    public static bool ShouldBlockBoatEntry()
    {
        if (BoatEntryBlockRequested == null)
            return false;

        foreach (Func<bool> handler in BoatEntryBlockRequested.GetInvocationList())
        {
            if (handler != null && handler.Invoke())
                return true;
        }

        return false;
    }

    public static void NotifyBoatEntryBlocked()
    {
        BoatEntryBlocked?.Invoke();
    }

    public static void NotifyBoatEntered()
    {
        BoatEntered?.Invoke();
    }

    public static void NotifyBoatExited()
    {
        BoatExited?.Invoke();
    }

    public static void NotifyFishCaught(FishData _fishData, ShipInventory _shipInventory)
    {
        FishCaught?.Invoke(_fishData, _shipInventory);
    }
}
