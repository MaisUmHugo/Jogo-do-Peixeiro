using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSaveManager : MonoBehaviour
{
    public static GameSaveManager Instance { get; private set; }

    [Header("Save Files")]
    [SerializeField] private string saveFileName = "savegame.json";
    [SerializeField] private string campaignSaveFileName = "campaign_save.json";
    [SerializeField] private string endlessSaveFileName = "endless_save.json";
    [SerializeField] private bool saveOnApplicationQuit = true;

    private const string LoadSaveOnNextSceneKey = "LoadSaveOnNextScene";
    private const string LoadSaveModeOnNextSceneKey = "LoadSaveModeOnNextScene";
    private float loadedPlayTimeSeconds;
    private float sessionStartRealtime;

    public string SavePath => GetSavePath(GetCurrentModeForSave());
    public bool HasSave => HasSaveForMode(GameProgressMode.Campaign) || HasSaveForMode(GameProgressMode.Endless);

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
        PlayerPrefs.DeleteKey(LoadSaveModeOnNextSceneKey);
        PlayerPrefs.Save();
    }

    public static void RequestLoadOnNextScene(GameProgressMode mode)
    {
        PlayerPrefs.SetInt(LoadSaveOnNextSceneKey, 1);
        PlayerPrefs.SetInt(LoadSaveModeOnNextSceneKey, (int)mode);
        PlayerPrefs.Save();
    }

    public static void ClearLoadRequest()
    {
        PlayerPrefs.DeleteKey(LoadSaveOnNextSceneKey);
        PlayerPrefs.DeleteKey(LoadSaveModeOnNextSceneKey);
        PlayerPrefs.Save();
    }

    public static void SaveCurrentGame()
    {
        GetOrCreate()?.SaveGame();
    }

    public static void SaveCurrentGameAndRequestLoadOnNextScene()
    {
        GameSaveManager saveManager = GetOrCreate();
        GameProgressMode mode = saveManager != null
            ? saveManager.GetCurrentModeForSave()
            : ResolveCurrentModeForLoadRequest();

        saveManager?.SaveGame();
        RequestLoadOnNextScene(mode);
    }

    private static GameProgressMode ResolveCurrentModeForLoadRequest()
    {
        CampaignProgressSystem campaignProgress = CampaignProgressSystem.Instance;
        return campaignProgress != null ? campaignProgress.GameMode : GameProgressMode.Campaign;
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
        ResetSessionTimer();
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
        if (saveOnApplicationQuit && CanSaveCurrentScene())
            SaveGame();
    }

    public bool LoadRequestedSaveIfNeeded()
    {
        if (PlayerPrefs.GetInt(LoadSaveOnNextSceneKey, 0) != 1)
            return false;

        bool hasRequestedMode = PlayerPrefs.HasKey(LoadSaveModeOnNextSceneKey);
        GameProgressMode requestedMode = (GameProgressMode)PlayerPrefs.GetInt(
            LoadSaveModeOnNextSceneKey,
            (int)GameProgressMode.Campaign
        );

        ClearLoadRequest();

        if (hasRequestedMode)
            return LoadGame(requestedMode);

        return LoadGame();
    }

    [ContextMenu("Save Game")]
    public void SaveGame()
    {
        if (!CanSaveCurrentScene())
            return;

        GameSaveData data = CaptureCurrentGame();
        string savePath = GetSavePath(data.gameMode);
        string json = JsonUtility.ToJson(data, true);
        string directory = Path.GetDirectoryName(savePath);

        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        File.WriteAllText(savePath, json);
        loadedPlayTimeSeconds = data.playTimeSeconds;
        ResetSessionTimer();
        Debug.Log($"Jogo salvo em: {savePath}");
    }

    [ContextMenu("Load Game")]
    public bool LoadGame()
    {
        if (!TryReadSaveData(out GameSaveData data))
            return false;

        ApplySaveData(data);
        Debug.Log("Jogo carregado.");
        return true;
    }

    public bool LoadGame(GameProgressMode mode)
    {
        if (!TryReadSaveData(mode, out GameSaveData data))
            return false;

        ApplySaveData(data);
        Debug.Log($"Jogo carregado: {mode}");
        return true;
    }

    public bool TryReadSaveData(out GameSaveData data)
    {
        if (TryReadSaveData(GameProgressMode.Campaign, out data))
            return true;

        if (TryReadSaveData(GameProgressMode.Endless, out data))
            return true;

        data = null;
        Debug.Log("Nenhum save encontrado.");
        return false;
    }

    public bool TryReadSaveData(GameProgressMode mode, out GameSaveData data)
    {
        string savePath = GetSavePath(mode);

        if (TryReadSaveDataFromPath(savePath, out data))
            return true;

        if (mode == GameProgressMode.Campaign && TryReadSaveDataFromPath(GetLegacySavePath(), out data))
            return true;

        data = null;
        return false;
    }

    public bool HasSaveForMode(GameProgressMode mode)
    {
        if (File.Exists(GetSavePath(mode)))
            return true;

        return mode == GameProgressMode.Campaign && File.Exists(GetLegacySavePath());
    }

    private bool TryReadSaveDataFromPath(string path, out GameSaveData data)
    {
        data = null;

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return false;
        }

        string json = File.ReadAllText(path);
        data = JsonUtility.FromJson<GameSaveData>(json);

        if (data != null)
            return true;

            Debug.LogWarning($"Save inválido: {path}");
        return false;
    }

    [ContextMenu("Delete Save")]
    public void DeleteSave()
    {
        bool deletedAny = DeleteSaveFile(GetSavePath(GameProgressMode.Campaign));
        deletedAny |= DeleteSaveFile(GetSavePath(GameProgressMode.Endless));
        deletedAny |= DeleteSaveFile(GetLegacySavePath());

        if (deletedAny)
        {
            ResetTrackedPlayTime();
            Debug.Log("Saves removidos.");
        }
    }

    public void DeleteSave(GameProgressMode mode)
    {
        DeleteSave(mode, true);
    }

    public void DeleteSave(GameProgressMode mode, bool resetTrackedPlayTime)
    {
        bool deletedAny = DeleteSaveFile(GetSavePath(mode));

        if (mode == GameProgressMode.Campaign)
            deletedAny |= DeleteSaveFile(GetLegacySavePath());

        if (deletedAny)
        {
            if (resetTrackedPlayTime)
                ResetTrackedPlayTime();

            Debug.Log($"Save removido: {mode}");
        }
    }

    public void ResetTrackedPlayTime()
    {
        loadedPlayTimeSeconds = 0f;
        ResetSessionTimer();
    }

    public GameSaveData CaptureCurrentGame()
    {
        GameSaveData data = new GameSaveData();

        PlayerMoneyManager moneyManager = FindFirstObjectByType<PlayerMoneyManager>();
        DebtSystem debtSystem = DebtSystem.GetOrCreate();
        MoneyLender moneyLender = FindFirstObjectByType<MoneyLender>();
        DockUpgradeSystem dockUpgradeSystem = FindFirstObjectByType<DockUpgradeSystem>(FindObjectsInactive.Include);
        DayCycle dayCycle = FindFirstObjectByType<DayCycle>();
        CampaignProgressSystem campaignProgress = CampaignProgressSystem.GetOrCreate();
        ShipInventory shipInventory = FindFirstObjectByType<ShipInventory>();
        BaitInventory baitInventory = FindFirstObjectByType<BaitInventory>(FindObjectsInactive.Include);

        data.playTimeSeconds = GetCurrentPlayTimeSeconds();
        data.playerMoney = moneyManager != null ? moneyManager.PlayerMoney : 0f;
        data.currentDebt = debtSystem != null ? debtSystem.CurrentDebt : 0;
        data.moneyLenderTimesPaid = moneyLender != null ? moneyLender.GetTimesPaid() : 0;
        data.moneyLenderDebtPaymentPaidAmount = moneyLender != null ? moneyLender.GetCurrentDebtPaymentPaidAmount() : 0;
        data.gameMode = campaignProgress != null ? campaignProgress.GameMode : GameProgressMode.Campaign;
        data.upgrades = CaptureUpgradeData(dockUpgradeSystem);
        data.dayCycle = CaptureDayCycleData(dayCycle);
        data.campaign = campaignProgress != null ? campaignProgress.CaptureSaveData() : new CampaignSaveData();
        data.shipFish = CaptureShipFishData(shipInventory);
        data.fishCaptureHistory = FishCaptureHistory.CaptureSaveData();
        data.equippedBaitId = baitInventory != null && baitInventory.EquippedBait != null ? baitInventory.EquippedBait.SaveId : string.Empty;
        data.baits = CaptureBaitData(baitInventory);

        return data;
    }

    public void ApplySaveData(GameSaveData _data)
    {
        if (_data == null)
            return;

        PlayerMoneyManager moneyManager = FindFirstObjectByType<PlayerMoneyManager>();
        DebtSystem debtSystem = DebtSystem.GetOrCreate();
        MoneyLender moneyLender = FindFirstObjectByType<MoneyLender>();
        DayCycle dayCycle = FindFirstObjectByType<DayCycle>();
        CampaignProgressSystem campaignProgress = CampaignProgressSystem.GetOrCreate();
        ShipInventory shipInventory = FindFirstObjectByType<ShipInventory>();
        BaitInventory baitInventory = FindFirstObjectByType<BaitInventory>(FindObjectsInactive.Include);

        if (moneyManager != null)
            moneyManager.SetMoney(_data.playerMoney);

        if (debtSystem != null)
            debtSystem.SetDebt(_data.currentDebt);

        if (moneyLender != null)
            moneyLender.SetPaymentCycle(_data.moneyLenderTimesPaid, _data.moneyLenderDebtPaymentPaidAmount);

        if (_data.upgrades != null)
        {
            DockUpgradeSystem.SetSharedUpgradeState(
                _data.upgrades.capacityLevel,
                _data.upgrades.boatSpeedLevel,
                _data.upgrades.rodLevel,
                _data.upgrades.hasFireproofBoatUpgrade
            );
        }

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

        FishCaptureHistory.ApplySaveData(_data.fishCaptureHistory);

        if (baitInventory != null)
            baitInventory.ReplaceBaits(RestoreBaitData(_data.baits), BaitSaveResolver.FindBaitById(_data.equippedBaitId));

        loadedPlayTimeSeconds = Mathf.Max(0f, _data.playTimeSeconds);
        ResetSessionTimer();
    }

    private float GetCurrentPlayTimeSeconds()
    {
        return Mathf.Max(0f, loadedPlayTimeSeconds + Time.realtimeSinceStartup - sessionStartRealtime);
    }

    private void ResetSessionTimer()
    {
        sessionStartRealtime = Time.realtimeSinceStartup;
    }

    private bool CanSaveCurrentScene()
    {
        return FindFirstObjectByType<GameManager>() != null &&
               FindFirstObjectByType<DayCycle>() != null &&
               FindFirstObjectByType<PlayerMoneyManager>() != null;
    }

    private GameProgressMode GetCurrentModeForSave()
    {
        CampaignProgressSystem campaignProgress = CampaignProgressSystem.Instance;
        return campaignProgress != null ? campaignProgress.GameMode : GameProgressMode.Campaign;
    }

    private string GetSavePath(GameProgressMode mode)
    {
        return Path.Combine(Application.persistentDataPath, GetSaveFileName(mode));
    }

    private string GetLegacySavePath()
    {
        return Path.Combine(Application.persistentDataPath, saveFileName);
    }

    private bool DeleteSaveFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return false;

        File.Delete(path);
        return true;
    }

    private string GetSaveFileName(GameProgressMode mode)
    {
        string fileName = mode == GameProgressMode.Endless ? endlessSaveFileName : campaignSaveFileName;

        if (!string.IsNullOrWhiteSpace(fileName))
            return fileName;

        return string.IsNullOrWhiteSpace(saveFileName) ? "savegame.json" : saveFileName;
    }

    private DockUpgradeSaveData CaptureUpgradeData(DockUpgradeSystem _dockUpgradeSystem)
    {
        if (DockUpgradeSystem.TryGetSharedUpgradeState(
                out int sharedCapacityLevel,
                out int sharedBoatSpeedLevel,
                out int sharedRodLevel,
                out bool sharedHasFireproofBoatUpgrade))
        {
            return new DockUpgradeSaveData
            {
                capacityLevel = sharedCapacityLevel,
                boatSpeedLevel = sharedBoatSpeedLevel,
                rodLevel = sharedRodLevel,
                hasFireproofBoatUpgrade = sharedHasFireproofBoatUpgrade
            };
        }

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
                Debug.LogWarning($"Peixe salvo não encontrado: {savedFish.fishId}");
                continue;
            }

            restoredFish.Add(new FishData(fishType, savedFish.weight));
        }

        return restoredFish;
    }

    private List<SavedBaitData> CaptureBaitData(BaitInventory _baitInventory)
    {
        List<SavedBaitData> savedBaits = new List<SavedBaitData>();

        if (_baitInventory == null)
            return savedBaits;

        foreach (BaitStack stack in _baitInventory.BaitStacks)
        {
            if (stack == null || stack.bait == null || stack.quantity <= 0)
                continue;

            savedBaits.Add(new SavedBaitData
            {
                baitId = stack.bait.SaveId,
                quantity = stack.quantity
            });
        }

        return savedBaits;
    }

    private List<BaitStack> RestoreBaitData(List<SavedBaitData> _savedBaits)
    {
        List<BaitStack> restoredBaits = new List<BaitStack>();

        if (_savedBaits == null)
            return restoredBaits;

        foreach (SavedBaitData savedBait in _savedBaits)
        {
            if (savedBait == null || savedBait.quantity <= 0)
                continue;

            BaitData bait = BaitSaveResolver.FindBaitById(savedBait.baitId);

            if (bait == null)
            {
                Debug.LogWarning($"Isca salva não encontrada: {savedBait.baitId}");
                continue;
            }

            restoredBaits.Add(new BaitStack(bait, savedBait.quantity));
        }

        return restoredBaits;
    }

    private void HandleSceneLoaded(Scene _scene, LoadSceneMode _mode)
    {
        LoadRequestedSaveIfNeeded();
    }
}
