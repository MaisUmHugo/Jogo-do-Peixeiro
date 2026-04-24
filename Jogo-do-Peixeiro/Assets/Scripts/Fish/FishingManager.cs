using System.Collections;
using UnityEngine;

public class FishingManager : MonoBehaviour
{
    public static FishingManager instance;

    [Header("UI")]
    [SerializeField] private FishingResultUI _fishingResultUI;

    [Header("Fishing Settings")]
    [SerializeField] private bool _useSkillCheck = true;
    [SerializeField] private float _baseProgressSpeed = 0.08f;

    [Header("Rarity Progress Multipliers")]
    [SerializeField] private float _rarity1ProgressMultiplier = 1f;
    [SerializeField] private float _rarity2ProgressMultiplier = 0.85f;
    [SerializeField] private float _rarity3ProgressMultiplier = 0.7f;

    [Header("References")]
    [SerializeField] private FishSkillCheck _fishSkillCheck;
    [SerializeField] private FishBiteHandler _fishBiteHandler;
    [SerializeField] private FishDirectionPull _fishDirectionPull;
    [SerializeField] private FishingRod _fishingRod;

    public bool IsFishing { get; private set; }
    public bool HasFishBitten { get; private set; }
    public float ProgressNormalized { get; private set; }

    private ShipInventory _currentShipInventory;
    private FishScriptableObject[] _currentAvailableFish;
    private FishScriptableObject _selectedFishType;
    private FishData _pendingFish;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void Update()
    {
        if (!IsFishing || !HasFishBitten)
            return;

        UpdateFishingProgress();
    }

    public void StartFishing(ShipInventory _shipInventory, FishScriptableObject[] _availableFish)
    {
        if (IsFishing)
            return;

        if (_shipInventory == null)
        {
            ReturnToBoatState();
            return;
        }

        if (_availableFish == null || _availableFish.Length == 0)
        {
            Debug.LogWarning("Nenhum peixe configurado nesse spot.");
            ReturnToBoatState();
            return;
        }

        if (_shipInventory.IsFull)
        {
            Debug.Log("Inventário do barco cheio.");

            if (HUDWarningUI.Instance != null)
                HUDWarningUI.Instance.ShowWarning("Inventário cheio");

            ReturnToBoatState();
            return;
        }

        _currentShipInventory = _shipInventory;
        _currentAvailableFish = _availableFish;

        _selectedFishType = PickRandomFishType();

        if (_selectedFishType == null)
        {
            Debug.LogWarning("Falha ao selecionar um peixe.");
            ClearFishingData();
            ReturnToBoatState();
            return;
        }

        _pendingFish = new FishData(_selectedFishType);

        IsFishing = true;
        HasFishBitten = false;
        ProgressNormalized = 0f;

        if (GameManager.instance != null)
            GameManager.instance.SetState(GameManager.GameState.Fishing);

        StartBiteWaiting();
    }

    private void StartBiteWaiting()
    {
        if (_fishBiteHandler != null)
        {
            _fishBiteHandler.StartWaiting(OnFishBite);
            return;
        }

        OnFishBite();
    }

    private void OnFishBite()
    {
        if (!IsFishing)
            return;

        HasFishBitten = true;

        if (_fishDirectionPull != null)
            _fishDirectionPull.StartPull();

        if (_useSkillCheck && _fishSkillCheck != null)
            _fishSkillCheck.StartSkillCheck(this, _selectedFishType);

        Debug.Log("Peixe mordeu a isca.");
    }

    private void UpdateFishingProgress()
    {
        float progressMultiplier = GetProgressMultiplierByFish();
        float progressDelta = _baseProgressSpeed * progressMultiplier;

        if (_fishDirectionPull != null)
        {
            Vector2 input = Vector2.zero;

            if (InputHandler.instance != null)
                input = InputHandler.instance.moveInput;

            progressDelta += _fishDirectionPull.GetProgressModifier(input, ProgressNormalized);
        }

        ProgressNormalized += progressDelta * Time.deltaTime;
        ProgressNormalized = Mathf.Clamp01(ProgressNormalized);

        if (ProgressNormalized >= 1f)
            CompleteFishing();
    }

    public void AddSkillCheckProgressBonus(float _bonus)
    {
        ProgressNormalized += _bonus;
        ProgressNormalized = Mathf.Clamp01(ProgressNormalized);

        if (ProgressNormalized >= 1f)
            CompleteFishing();
    }

    public void ApplySkillCheckPenalty(float _penalty)
    {
        ProgressNormalized -= _penalty;
        ProgressNormalized = Mathf.Clamp01(ProgressNormalized);
    }

    public void OnSkillCheckSuccessTick(FishSkillCheck.FeedbackResult _result)
    {
        if (_fishingRod != null)
            _fishingRod.PlaySuccessSplash(_result);
    }

    public void OnSkillCheckFail()
    {
        Debug.Log("Falhou na pescaria.");
        EndFishing(false);
    }

    private void CompleteFishing()
    {
        GivePendingFish();
        EndFishing(true);
    }

    private FishScriptableObject PickRandomFishType()
    {
        if (_currentAvailableFish == null || _currentAvailableFish.Length == 0)
            return null;

        int randomIndex = Random.Range(0, _currentAvailableFish.Length);
        return _currentAvailableFish[randomIndex];
    }

    private float GetProgressMultiplierByFish()
    {
        if (_selectedFishType == null)
            return 1f;

        switch (_selectedFishType.rarity)
        {
            case 1:
                return _rarity1ProgressMultiplier;

            case 2:
                return _rarity2ProgressMultiplier;

            case 3:
                return _rarity3ProgressMultiplier;

            default:
                return 1f;
        }
    }

    private void GivePendingFish()
    {
        if (_currentShipInventory == null || _pendingFish == null)
            return;

        bool addedSuccessfully = _currentShipInventory.TryAddFish(_pendingFish);

        if (addedSuccessfully)
        {
            Debug.Log($"Peixe capturado: {_pendingFish.typeOfFish.fishName} - {_pendingFish.weight}kg");

            if (HUDFishInfoUI.Instance != null)
            {
                HUDFishInfoUI.Instance.ShowFishInfo(
                    _pendingFish.typeOfFish.fishName,
                    _pendingFish.weight
                );
            }

            if (_fishingResultUI != null)
                _fishingResultUI.ShowCatchResult(_pendingFish);

            UpdateTutorialAfterFishCaught();
        }
        else
        {
            Debug.Log("Inventário cheio.");

            if (HUDWarningUI.Instance != null)
                HUDWarningUI.Instance.ShowWarning("Sem espaço para mais peixes");
        }

        _pendingFish = null;
    }

    private void UpdateTutorialAfterFishCaught()
    {
        if (TutorialHandler.Instance == null || _currentShipInventory == null)
            return;

        float currentWeight = _currentShipInventory.GetCurrentWeight();
        Debug.Log($"Peso atual após capturar peixe: {currentWeight}");

        if (currentWeight >= 10f)
        {
            TutorialHandler.Instance.isFinishedFishing = true;
            TutorialHandler.Instance.GoNextObjective();
        }
        else
        {
            TutorialHandler.Instance.AttFishWeightTutorialText();
        }
    }

    private void EndFishing(bool _success)
    {
        IsFishing = false;
        HasFishBitten = false;
        ProgressNormalized = 0f;

        if (_fishBiteHandler != null)
            _fishBiteHandler.StopWaiting();

        if (_fishDirectionPull != null)
            _fishDirectionPull.StopPull();

        if (_fishSkillCheck != null)
            _fishSkillCheck.StopSkillCheck();

        if (GameManager.instance != null)
            GameManager.instance.SetState(GameManager.GameState.OnBoat);

        ClearFishingData();
    }

    private void ClearFishingData()
    {
        _currentShipInventory = null;
        _currentAvailableFish = null;
        _selectedFishType = null;
        _pendingFish = null;
    }

    private void ReturnToBoatState()
    {
        if (GameManager.instance != null)
            GameManager.instance.SetState(GameManager.GameState.OnBoat);
    }
}