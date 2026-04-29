using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InvertoryManager : MonoBehaviour
{
    public static InvertoryManager Instance { get; private set; }

    [SerializeField] private ShipInventory shipInventory;
    [SerializeField] private GameObject inventoryRoot;
    [SerializeField] private TMP_Text inventoryText;
    [SerializeField] private TMP_Text kilogramText;
    [SerializeField] private bool closeOnAwake = true;

    private CanvasGroup inventoryCanvasGroup;
    private Coroutine inputSubscriptionRoutine;
    private bool isInventoryVisible = true;
    private bool isShipInventorySubscribed;
    private bool isInputSubscribed;
    private GameManager.GameState stateBeforeInventory = GameManager.GameState.OnFoot;
    private bool hasStoredStateBeforeInventory;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitializeReferences();

        if (closeOnAwake)
            SetInventoryVisible(false);
    }

    private void OnEnable()
    {
        InitializeReferences();
        TrySubscribeInput();

        if (!isInputSubscribed)
            inputSubscriptionRoutine = StartCoroutine(WaitForInputHandler());
    }

    private void OnDisable()
    {
        if (inputSubscriptionRoutine != null)
        {
            StopCoroutine(inputSubscriptionRoutine);
            inputSubscriptionRoutine = null;
        }

        UnsubscribeInput();
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;

        if (shipInventory != null && isShipInventorySubscribed)
            shipInventory.OnFishListChange -= OnNewFishList;
    }

    private void InitializeReferences()
    {
        if (inventoryRoot == null)
            inventoryRoot = gameObject;

        if (inventoryRoot == gameObject && inventoryCanvasGroup == null)
        {
            inventoryCanvasGroup = inventoryRoot.GetComponent<CanvasGroup>();

            if (inventoryCanvasGroup == null)
                inventoryCanvasGroup = inventoryRoot.AddComponent<CanvasGroup>();
        }

        if (shipInventory == null)
            shipInventory = FindFirstObjectByType<ShipInventory>(FindObjectsInactive.Include);

        if (shipInventory != null && !isShipInventorySubscribed)
        {
            shipInventory.OnFishListChange += OnNewFishList;
            isShipInventorySubscribed = true;
        }
    }

    private void TrySubscribeInput()
    {
        if (isInputSubscribed || InputHandler.instance == null)
            return;

        InputHandler.instance.onInventoryPressed += ToggleInventory;
        isInputSubscribed = true;
    }

    private IEnumerator WaitForInputHandler()
    {
        while (!isInputSubscribed)
        {
            TrySubscribeInput();

            if (isInputSubscribed)
                break;

            yield return null;
        }

        inputSubscriptionRoutine = null;
    }

    private void UnsubscribeInput()
    {
        if (!isInputSubscribed)
            return;

        if (InputHandler.instance != null)
            InputHandler.instance.onInventoryPressed -= ToggleInventory;

        isInputSubscribed = false;
    }

    private void OnNewFishList(List<FishData> _ownedFishes, float _fishweight)
    {
        if (inventoryText != null)
        {
            inventoryText.text = "";

            foreach (FishData fish in _ownedFishes)
            {
                inventoryText.text += $"{fish.typeOfFish.fishName}, peso: {fish.weight} \n \n";
            }
        }

        if (kilogramText != null)
            kilogramText.text = $"kilos de peixe: {_fishweight} Kg";

    }

    public void ToggleInventory()
    {
        InitializeReferences();

        if (IsInventoryVisible())
        {
            CloseInventory();
            return;
        }

        OpenInventory();
    }

    public void OpenInventory()
    {
        if (!CanOpenInventory())
            return;

        StoreGameStateBeforeInventory();
        SetInventoryVisible(true);
    }

    public void CloseInventory()
    {
        bool wasVisible = IsInventoryVisible();
        SetInventoryVisible(false);

        if (wasVisible)
            RestoreGameStateAfterInventory();
    }

    public bool TryCloseInventory()
    {
        if (!IsInventoryVisible())
            return false;

        CloseInventory();
        return true;
    }

    public static bool TryCloseOpenInventory()
    {
        if (Instance == null)
            Instance = FindFirstObjectByType<InvertoryManager>(FindObjectsInactive.Include);

        return Instance != null && Instance.TryCloseInventory();
    }

    private void SetInventoryVisible(bool _visible)
    {
        if (inventoryRoot == null)
            return;

        isInventoryVisible = _visible;

        if (inventoryRoot == gameObject)
        {
            if (inventoryCanvasGroup == null)
                inventoryCanvasGroup = inventoryRoot.GetComponent<CanvasGroup>();

            if (inventoryCanvasGroup == null)
                inventoryCanvasGroup = inventoryRoot.AddComponent<CanvasGroup>();

            inventoryCanvasGroup.alpha = _visible ? 1f : 0f;
            inventoryCanvasGroup.interactable = _visible;
            inventoryCanvasGroup.blocksRaycasts = _visible;
            return;
        }

        inventoryRoot.SetActive(_visible);
    }

    private bool IsInventoryVisible()
    {
        if (inventoryRoot == null)
            return false;

        if (inventoryRoot == gameObject)
            return isInventoryVisible;

        return inventoryRoot.activeSelf;
    }

    private bool CanOpenInventory()
    {
        if (GameManager.instance == null)
            return true;

        return GameManager.instance.currentState == GameManager.GameState.OnFoot ||
               GameManager.instance.currentState == GameManager.GameState.OnBoat;
    }

    private void StoreGameStateBeforeInventory()
    {
        if (GameManager.instance == null)
            return;

        stateBeforeInventory = GameManager.instance.currentState;
        hasStoredStateBeforeInventory = true;
        GameManager.instance.SetState(GameManager.GameState.InUI);
    }

    private void RestoreGameStateAfterInventory()
    {
        if (!hasStoredStateBeforeInventory || GameManager.instance == null)
            return;

        if (GameManager.instance.currentState == GameManager.GameState.InUI)
            GameManager.instance.SetState(stateBeforeInventory);

        hasStoredStateBeforeInventory = false;
    }
}
