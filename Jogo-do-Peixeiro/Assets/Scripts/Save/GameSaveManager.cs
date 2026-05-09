using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSaveManager : MonoBehaviour
{
    public static GameSaveManager Instance { get; private set; }

    [SerializeField] private string saveFileName = "savegame.json";
    [SerializeField] private bool saveOnApplicationQuit = true;

    private const string LoadSaveOnNextSceneKey = "LoadSaveOnNextScene";

    public string SavePath => Path.Combine(Application.persistentDataPath, saveFileName);
    public bool HasSave => File.Exists(SavePath);

    public static GameSaveManager GetOrCreate()
    {
        if (Instance != null)
            return Instance;

        Instance = FindFirstObjectByType<GameSaveManager>();

        if (Instance != null)
            return Instance;

        GameObject saveManagerObject = new GameObject("GameSaveManager");
        Instance = saveManagerObject.AddComponent<GameSaveManager>();
        return Instance;
    }

    public static void RequestLoadOnNextScene()
    {
        PlayerPrefs.SetInt(LoadSaveOnNextSceneKey, 1);
        PlayerPrefs.Save();
    }

    public static void ClearLoadRequest()
    {
        PlayerPrefs.DeleteKey(LoadSaveOnNextSceneKey);
        PlayerPrefs.Save();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void OnApplicationQuit()
    {
        if (saveOnApplicationQuit)
            SaveGame();
    }

    public bool LoadRequestedSaveIfNeeded()
    {
        if (PlayerPrefs.GetInt(LoadSaveOnNextSceneKey, 0) != 1)
            return false;

        ClearLoadRequest();
        return LoadGame();
    }

    [ContextMenu("Save Game")]
    public void SaveGame()
    {
        GameSaveData data = CaptureCurrentGame();
        string json = JsonUtility.ToJson(data, true);
        string directory = Path.GetDirectoryName(SavePath);

        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(SavePath, json);
        Debug.Log($"Jogo salvo em: {SavePath}");
    }

    [ContextMenu("Load Game")]
    public bool LoadGame()
    {
        if (!HasSave)
        {
            Debug.Log("Nenhum save encontrado.");
            return false;
        }

        string json = File.ReadAllText(SavePath);
        GameSaveData data = JsonUtility.FromJson<GameSaveData>(json);

        if (data == null)
        {
            Debug.LogWarning("Save invalido.");
            return false;
        }

        ApplySaveData(data);
        Debug.Log($"Jogo carregado de: {SavePath}");
        return true;
    }

    [ContextMenu("Delete Save")]
    public void DeleteSave()
    {
        if (!HasSave)
            return;

        File.Delete(SavePath);
        Debug.Log("Save removido.");
    }

    public GameSaveData CaptureCurrentGame()
    {
        GameSaveData data = new GameSaveData();

        PlayerMoneyManager moneyManager = FindFirstObjectByType<PlayerMoneyManager>();
        DebtSystem debtSystem = DebtSystem.GetOrCreate();
        MoneyLender moneyLender = FindFirstObjectByType<MoneyLender>();
        DockUpgradeSystem dockUpgradeSystem = FindFirstObjectByType<DockUpgradeSystem>();
        DayCycle dayCycle = FindFirstObjectByType<DayCycle>();
        CampaignProgressSystem campaignProgress = CampaignProgressSystem.GetOrCreate();
        ShipInventory shipInventory = FindFirstObjectByType<ShipInventory>();

        data.playerMoney = moneyManager != null ? moneyManager.PlayerMoney : 0f;
        data.currentDebt = debtSystem != null ? debtSystem.CurrentDebt : 0;
        data.moneyLenderTimesPaid = moneyLender != null ? moneyLender.GetTimesPaid() : 0;
        data.moneyLenderDebtPaymentPaidAmount = moneyLender != null ? moneyLender.GetCurrentDebtPaymentPaidAmount() : 0;
        data.gameMode = campaignProgress != null ? campaignProgress.GameMode : GameProgressMode.Campaign;
        data.upgrades = CaptureUpgradeData(dockUpgradeSystem);
        data.dayCycle = CaptureDayCycleData(dayCycle);
        data.campaign = campaignProgress != null ? campaignProgress.CaptureSaveData() : new CampaignSaveData();
        data.shipFish = CaptureShipFishData(shipInventory);

        return data;
    }

    public void ApplySaveData(GameSaveData _data)
    {
        if (_data == null)
            return;

        PlayerMoneyManager moneyManager = FindFirstObjectByType<PlayerMoneyManager>();
        DebtSystem debtSystem = DebtSystem.GetOrCreate();
        MoneyLender moneyLender = FindFirstObjectByType<MoneyLender>();
        DockUpgradeSystem dockUpgradeSystem = FindFirstObjectByType<DockUpgradeSystem>();
        DayCycle dayCycle = FindFirstObjectByType<DayCycle>();
        CampaignProgressSystem campaignProgress = CampaignProgressSystem.GetOrCreate();
        ShipInventory shipInventory = FindFirstObjectByType<ShipInventory>();

        if (moneyManager != null)
            moneyManager.SetMoney(_data.playerMoney);

        if (debtSystem != null)
            debtSystem.SetDebt(_data.currentDebt);

        if (moneyLender != null)
            moneyLender.SetPaymentCycle(_data.moneyLenderTimesPaid, _data.moneyLenderDebtPaymentPaidAmount);

        if (dockUpgradeSystem != null && _data.upgrades != null)
            dockUpgradeSystem.SetUpgradeState(
                _data.upgrades.capacityLevel,
                _data.upgrades.boatSpeedLevel,
                _data.upgrades.rodLevel,
                _data.upgrades.hasFireproofBoatUpgrade
            );

        if (dayCycle != null && _data.dayCycle != null)
            dayCycle.SetCycleState(
                _data.dayCycle.currentDay,
                _data.dayCycle.elapsedDays,
                _data.dayCycle.normalizedTime
            );

        if (campaignProgress != null && _data.campaign != null)
            campaignProgress.ApplySaveData(_data.campaign);

        if (shipInventory != null)
            shipInventory.ReplaceFish(RestoreShipFishData(_data.shipFish));
    }

    private DockUpgradeSaveData CaptureUpgradeData(DockUpgradeSystem _dockUpgradeSystem)
    {
        if (_dockUpgradeSystem == null)
            return new DockUpgradeSaveData();

        return new DockUpgradeSaveData
        {
            capacityLevel = _dockUpgradeSystem.CapacityLevel,
            boatSpeedLevel = _dockUpgradeSystem.BoatSpeedLevel,
            rodLevel = _dockUpgradeSystem.RodLevel,
            hasFireproofBoatUpgrade = _dockUpgradeSystem.HasFireproofBoatUpgrade
        };
    }

    private DayCycleSaveData CaptureDayCycleData(DayCycle _dayCycle)
    {
        if (_dayCycle == null)
            return new DayCycleSaveData();

        return new DayCycleSaveData
        {
            currentDay = _dayCycle.CurrentDay,
            elapsedDays = _dayCycle.ElapsedDays,
            normalizedTime = _dayCycle.NormalizedTime
        };
    }

    private List<SavedFishData> CaptureShipFishData(ShipInventory _shipInventory)
    {
        List<SavedFishData> savedFish = new List<SavedFishData>();

        if (_shipInventory == null)
            return savedFish;

        foreach (FishData fish in _shipInventory.OwnedFish)
        {
            if (fish == null || fish.typeOfFish == null)
                continue;

            savedFish.Add(new SavedFishData
            {
                fishId = fish.typeOfFish.SaveId,
                weight = fish.weight
            });
        }

        return savedFish;
    }

    private List<FishData> RestoreShipFishData(List<SavedFishData> _savedFish)
    {
        List<FishData> restoredFish = new List<FishData>();

        if (_savedFish == null)
            return restoredFish;

        foreach (SavedFishData savedFish in _savedFish)
        {
            if (savedFish == null)
                continue;

            FishScriptableObject fishType = FishSaveResolver.FindFishById(savedFish.fishId);

            if (fishType == null)
            {
                Debug.LogWarning($"Peixe salvo nao encontrado: {savedFish.fishId}");
                continue;
            }

            restoredFish.Add(new FishData(fishType, savedFish.weight));
        }

        return restoredFish;
    }

    private void HandleSceneLoaded(Scene _scene, LoadSceneMode _mode)
    {
        LoadRequestedSaveIfNeeded();
    }
}
