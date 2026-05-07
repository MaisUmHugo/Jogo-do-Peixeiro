using TMPro;
using UnityEngine;

public class MoneyLenderUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text requiredWeightText;
    [SerializeField] private TMP_Text currentWeightText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private PlayerMoneyManager playerMoneyManager;

    [Header("Audio")]
    [SerializeField] private AudioClip doorOpenSfx;
    [SerializeField] private AudioClip doorCloseSfx;
    [SerializeField, Range(0f, 1f)] private float doorOpenSfxVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float doorCloseSfxVolume = 1f;

    private MoneyLender currentMoneyLender;
    private TutorialController tutorialController;
    private bool isOpen;
    private bool isTutorialPayment;

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
        isTutorialPayment = false;
        tutorialController = null;
        OpenInternal(_moneyLender);
    }

    public void OpenForTutorial(MoneyLender _moneyLender, TutorialController _tutorialController)
    {
        if (_tutorialController == null)
            return;

        isTutorialPayment = true;
        tutorialController = _tutorialController;
        OpenInternal(_moneyLender);
    }

    private void OpenInternal(MoneyLender _moneyLender)
    {
        if (_moneyLender == null)
            return;

        bool wasOpen = isOpen;

        currentMoneyLender = _moneyLender;

        if (playerMoneyManager == null)
            playerMoneyManager = FindFirstObjectByType<PlayerMoneyManager>();

        isOpen = true;

        if (panel != null)
            panel.SetActive(true);

        if (statusText != null)
            statusText.text = string.Empty;

        if (GameManager.instance != null)
            GameManager.instance.SetState(GameManager.GameState.InUI);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (!wasOpen)
            PlayDoorSfx(doorOpenSfx, doorOpenSfxVolume);

        Refresh();
    }

    public void OnClickDeliverWeight()
    {
        if (currentMoneyLender == null)
            return;

        if (isTutorialPayment && tutorialController != null)
        {
            bool tutorialSuccess = tutorialController.TryDeliverRequestedFishFromUI(currentMoneyLender, this);

            if (statusText != null && !tutorialSuccess)
                statusText.text = "Pedido incompleto.";

            if (!tutorialSuccess)
                Refresh();

            return;
        }

        bool success = currentMoneyLender.TryPayDebt(out int paidAmount, out MoneyLender.DebtPaymentResult paymentResult);

        if (statusText != null)
            statusText.text = GetDebtPaymentStatusText(success, paidAmount, paymentResult);

        if (paymentResult == MoneyLender.DebtPaymentResult.Completed ||
            paymentResult == MoneyLender.DebtPaymentResult.PaidOff)
        {
            Close();
            return;
        }

        Refresh();
    }

    private string GetDebtPaymentStatusText(bool _success, int _paidAmount, MoneyLender.DebtPaymentResult _paymentResult)
    {
        if (!_success)
            return "Dinheiro insuficiente.";

        return _paymentResult switch
        {
            MoneyLender.DebtPaymentResult.Partial => $"Pagamento parcial: R$ {_paidAmount}.",
            MoneyLender.DebtPaymentResult.Completed => "Divida reduzida.",
            MoneyLender.DebtPaymentResult.PaidOff => "Divida quitada.",
            _ => "Dinheiro insuficiente."
        };
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

        if (isTutorialPayment && tutorialController != null)
        {
            if (requiredWeightText != null)
                requiredWeightText.text = $"Pedido: {tutorialController.RequestedQuantity}x {tutorialController.RequestedFishName} + {tutorialController.RequestedTotalWeight} kg";

            if (currentWeightText != null)
                currentWeightText.text = $"No barco: {tutorialController.OwnedRequestedFishCount}/{tutorialController.RequestedQuantity} peixes | {tutorialController.CurrentOwnedFishWeight:0}/{tutorialController.RequestedTotalWeight} kg";

            return;
        }

        if (playerMoneyManager == null)
            playerMoneyManager = FindFirstObjectByType<PlayerMoneyManager>();

        int debtBalance = currentMoneyLender.GetCurrentDebtBalance();
        int debtPayment = currentMoneyLender.GetCurrentPayableDebtPayment();
        string debtValue = debtBalance > 0 ? $"-R$ {debtBalance}" : "R$ 0";

        if (requiredWeightText != null)
            requiredWeightText.text = $"Divida: {debtValue} | Pagamento: R$ {debtPayment}";

        if (currentWeightText != null)
            currentWeightText.text = $"Dinheiro: R$ {(playerMoneyManager != null ? playerMoneyManager.PlayerMoney : 0f):0}";
    }

    private void Close()
    {
        if (isOpen)
            PlayDoorSfx(doorCloseSfx, doorCloseSfxVolume);

        CloseImmediate();

        if (GameManager.instance != null)
            GameManager.instance.SetState(GameManager.GameState.OnFoot);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void CloseForTutorialFinish()
    {
        if (isOpen)
            PlayDoorSfx(doorCloseSfx, doorCloseSfxVolume);

        CloseImmediate();

        if (GameManager.instance != null)
            GameManager.instance.SetState(GameManager.GameState.InUI);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void CloseImmediate()
    {
        isOpen = false;
        currentMoneyLender = null;

        if (panel != null)
            panel.SetActive(false);
    }

    private void PlayDoorSfx(AudioClip _clip, float _volume)
    {
        if (AudioManager.Instance == null || _clip == null)
            return;

        AudioManager.Instance.PlaySfx(_clip, _volume);
    }
}
