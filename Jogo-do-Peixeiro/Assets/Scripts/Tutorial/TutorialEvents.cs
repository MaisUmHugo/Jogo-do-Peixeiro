using System;

public static class TutorialEvents
{
    public static event Func<MoneyLender, bool> MoneyLenderInteractionRequested;
    public static event Func<bool> BoatEntryBlockRequested;
    public static event Action BoatEntryBlocked;
    public static event Action BoatEntered;
    public static event Action<FishData, ShipInventory> FishCaught;

    public static bool TryHandleMoneyLenderInteraction(MoneyLender _moneyLender)
    {
        if (MoneyLenderInteractionRequested == null)
            return false;

        foreach (Func<MoneyLender, bool> handler in MoneyLenderInteractionRequested.GetInvocationList())
        {
            if (handler != null && handler.Invoke(_moneyLender))
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

    public static void NotifyFishCaught(FishData _fishData, ShipInventory _shipInventory)
    {
        FishCaught?.Invoke(_fishData, _shipInventory);
    }
}
