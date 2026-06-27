using System;
using System.Collections.Generic;

public enum GameProgressMode
{
    Campaign,
    Endless
}

[Serializable]
public class GameSaveData
{
    public int version = 1;
    public GameProgressMode gameMode = GameProgressMode.Campaign;
    public float playTimeSeconds;
    public float playerMoney;
    public int currentDebt;
    public int moneyLenderTimesPaid;
    public int moneyLenderDebtPaymentPaidAmount;
    public DockUpgradeSaveData upgrades = new DockUpgradeSaveData();
    public DayCycleSaveData dayCycle = new DayCycleSaveData();
    public CampaignSaveData campaign = new CampaignSaveData();
    public List<SavedFishData> shipFish = new List<SavedFishData>();
    public List<SavedFishCaptureData> fishCaptureHistory = new List<SavedFishCaptureData>();
    public string equippedBaitId;
    public List<SavedBaitData> baits = new List<SavedBaitData>();
}

[Serializable]
public class DockUpgradeSaveData
{
    public int capacityLevel;
    public int boatSpeedLevel;
    public int rodLevel;
    public bool hasFireproofBoatUpgrade;
}

[Serializable]
public class DayCycleSaveData
{
    public int currentDay = 1;
    public int elapsedDays = 1;
    public float normalizedTime;
}

[Serializable]
public class CampaignSaveData
{
    public GameProgressMode gameMode = GameProgressMode.Campaign;
    public int currentQuestIndex = 1;
    public int maxQuestCount = 5;
    public int questDurationDays = 3;
    public int daysElapsedInCurrentQuest;
    public int questDebtPaymentTarget = 50;
    public int questDebtPaidAmount;
    public bool hasFailedCurrentQuest;
    public bool hasUnlockedFreePlay;
    public bool isCampaignCompleted;
    public bool endlessUnlocked;
    public int campaignCompletionDebtAmount = 100000000;
    public bool isSpecialMoneyLenderDeliveryActive;
    public string specialDeliveryFishId;
    public int specialDeliveryQuantity;
    public int specialDeliveryRequiredWeight;
    public int specialDeliveryDebtReduction;
    public int endlessDeliveriesWithoutSpecialRequest;
    public bool tutorialStateSaved;
    public bool tutorialRunTutorial = true;
    public int tutorialStep;
    public bool tutorialFinished;
    public bool tutorialFailed;
    public bool tutorialHasAcceptedRequest;
    public bool tutorialHasSoldFishToDockOwner;
    public bool tutorialHasPlayedMoneyLenderIntroCutscene;
    public bool tutorialHasPlayedBoatBeforeIntroEdgeDialog;
    public bool tutorialHasCompletedOpeningIntroFlow;
    public bool tutorialDockOwnerFirstDialogPlayed;
    public string tutorialRequestedFishId;
    public int tutorialRequestedQuantity;
    public int tutorialRequestedTotalWeight;
    public int tutorialStartDay = 1;
}

[Serializable]
public class SavedFishData
{
    public string fishId;
    public int weight;
}

[Serializable]
public class SavedFishCaptureData
{
    public string fishId;
    public int captureCount;
}

[Serializable]
public class SavedBaitData
{
    public string baitId;
    public int quantity;
}
