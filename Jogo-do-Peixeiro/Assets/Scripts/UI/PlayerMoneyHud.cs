using TMPro;
using UnityEngine;

public class PlayerMoneyHud : MonoBehaviour
{
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text debtText;
    [SerializeField] private PlayerMoneyManager playerMoneyManager;
    [SerializeField] private DebtSystem debtSystem;
    [SerializeField] private bool showDebtWithMoneyWhenMissingText = true;
    [SerializeField] private string debtColor = "#D94A4A";
    [SerializeField] private string paidDebtColor = "#6CCB6C";

    private float currentMoney;
    private int currentDebt;

    private void OnEnable()
    {
        PlayerMoneyManager.OnMoneyChangeEvent += UpdateMoneyText;
        DebtSystem.OnDebtChangedEvent += UpdateDebtText;

        ResolveReferences();

        currentMoney = playerMoneyManager != null ? playerMoneyManager.PlayerMoney : 0f;
        currentDebt = debtSystem != null ? debtSystem.CurrentDebt : 0;

        RefreshHudText();
    }

    private void OnDisable()
    {
        PlayerMoneyManager.OnMoneyChangeEvent -= UpdateMoneyText;
        DebtSystem.OnDebtChangedEvent -= UpdateDebtText;
    }

    private void UpdateMoneyText(float _money) 
    {
        currentMoney = _money;
        RefreshHudText();
    }

    private void UpdateDebtText(int _currentDebt, int _changeAmount)
    {
        currentDebt = _currentDebt;
        RefreshHudText();
    }

    private void RefreshHudText()
    {
        string moneyLine = $"R$: {currentMoney:0}";
        string debtLine = GetDebtLine();

        if (moneyText != null)
        {
            moneyText.text = debtText == null && showDebtWithMoneyWhenMissingText
                ? $"{moneyLine}\n{debtLine}"
                : moneyLine;
        }

        if (debtText != null)
            debtText.text = debtLine;
    }

    private string GetDebtLine()
    {
        if (currentDebt <= 0)
            return $"<color={paidDebtColor}>Divida: R$ 0</color>";

        return $"<color={debtColor}>Divida: -R$ {currentDebt}</color>";
    }

    private void ResolveReferences()
    {
        if (playerMoneyManager == null)
            playerMoneyManager = FindFirstObjectByType<PlayerMoneyManager>();

        if (debtSystem == null)
            debtSystem = DebtSystem.GetOrCreate();
    }
}
