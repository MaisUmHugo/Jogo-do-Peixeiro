using TMPro;
using UnityEngine;

public class MoneyLenderUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text requiredWeightText;
    [SerializeField] private TMP_Text currentWeightText;
    [SerializeField] private TMP_Text statusText;

    private MoneyLender currentMoneyLender;
    private bool isOpen;

    private void Awake()
    {
        CloseImmediate();
    }

    private void Start()
    {
        if (InputHandler.instance != null)
            InputHandler.instance.onPausePressed += HandlePausePressed;
    }

    private void OnDestroy()
    {
        if (InputHandler.instance != null)
            InputHandler.instance.onPausePressed -= HandlePausePressed;
    }

    private void Update()
    {
        if (!isOpen)
            return;

        Refresh();
    }

    public void Open(MoneyLender _moneyLender)
    {
        if (_moneyLender == null)
            return;

        currentMoneyLender = _moneyLender;
        isOpen = true;

        if (panel != null)
            panel.SetActive(true);

        if (statusText != null)
            statusText.text = string.Empty;

        if (GameManager.instance != null)
            GameManager.instance.SetState(GameManager.GameState.InUI);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Refresh();
    }

    public void OnClickDeliverWeight()
    {
        if (currentMoneyLender == null)
            return;

        bool success = currentMoneyLender.TryGetFishWeightPayment();

        if (statusText != null)
            statusText.text = success ? "Pagamento entregue." : "Peso de peixe insuficiente.";

        Refresh();
    }

    public void OnClickClose()
    {
        Close();
    }

    private void HandlePausePressed()
    {
        if (!isOpen)
            return;

        Close();
    }

    private void Refresh()
    {
        if (currentMoneyLender == null)
            return;

        if (requiredWeightText != null)
            requiredWeightText.text = $"Required: {currentMoneyLender.GetCurrentFishWeightPayment()} kg";

        if (currentWeightText != null)
            currentWeightText.text = $"Current: {currentMoneyLender.GetCurrentOwnedWeight()} kg";
    }

    private void Close()
    {
        CloseImmediate();

        if (GameManager.instance != null)
            GameManager.instance.SetState(GameManager.GameState.OnFoot);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void CloseImmediate()
    {
        isOpen = false;
        currentMoneyLender = null;

        if (panel != null)
            panel.SetActive(false);
    }
}