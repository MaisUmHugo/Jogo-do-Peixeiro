using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerMoneyHud : MonoBehaviour
{
    [SerializeField] private TMP_Text moneyText;
    [SerializeField] private TMP_Text debtText;
    [SerializeField] private TMP_Text campaignText;
    [SerializeField] private PlayerMoneyManager playerMoneyManager;
    [SerializeField] private DebtSystem debtSystem;
    [SerializeField] private CampaignProgressSystem campaignProgress;
    [SerializeField] private bool showDebtWithMoneyWhenMissingText = true;
    [SerializeField] private bool showCampaignHud = true;
    [SerializeField] private bool autoCreateCampaignHud = true;
    [SerializeField] private string debtColor = "#D94A4A";
    [SerializeField] private string paidDebtColor = "#6CCB6C";
    [SerializeField] private string warningColor = "#F2C94C";

    private float currentMoney;
    private int currentDebt;
    private bool isCampaignSubscribed;

    private void OnEnable()
    {
        PlayerMoneyManager.OnMoneyChangeEvent += UpdateMoneyText;
        DebtSystem.OnDebtChangedEvent += UpdateDebtText;

        ResolveReferences();
        SubscribeCampaignProgress();
        EnsureCampaignHud();

        currentMoney = playerMoneyManager != null ? playerMoneyManager.PlayerMoney : 0f;
        currentDebt = debtSystem != null ? debtSystem.CurrentDebt : 0;

        RefreshHudText();
    }

    private void OnDisable()
    {
        PlayerMoneyManager.OnMoneyChangeEvent -= UpdateMoneyText;
        DebtSystem.OnDebtChangedEvent -= UpdateDebtText;
        UnsubscribeCampaignProgress();
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

        if (campaignText != null)
        {
            campaignText.gameObject.SetActive(showCampaignHud);
            campaignText.text = GetCampaignLine();
        }
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

        if (campaignProgress == null)
            campaignProgress = CampaignProgressSystem.GetOrCreate();
    }

    private void SubscribeCampaignProgress()
    {
        if (isCampaignSubscribed || campaignProgress == null)
            return;

        campaignProgress.OnProgressChanged += RefreshHudText;
        campaignProgress.OnQuestAdvanced += RefreshHudText;
        campaignProgress.OnQuestDeadlineExpired += RefreshHudText;
        campaignProgress.OnCampaignCompleted += RefreshHudText;
        isCampaignSubscribed = true;
    }

    private void UnsubscribeCampaignProgress()
    {
        if (!isCampaignSubscribed || campaignProgress == null)
            return;

        campaignProgress.OnProgressChanged -= RefreshHudText;
        campaignProgress.OnQuestAdvanced -= RefreshHudText;
        campaignProgress.OnQuestDeadlineExpired -= RefreshHudText;
        campaignProgress.OnCampaignCompleted -= RefreshHudText;
        isCampaignSubscribed = false;
    }

    private void EnsureCampaignHud()
    {
        if (!showCampaignHud || !autoCreateCampaignHud || campaignText != null)
            return;

        GameObject panelObject = new GameObject("CampaignHUD", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panelObject.transform.SetParent(transform, false);

        RectTransform panelRect = panelObject.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(24f, -24f);
        panelRect.sizeDelta = new Vector2(520f, 138f);

        Image panelImage = panelObject.GetComponent<Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.55f);
        panelImage.raycastTarget = false;

        GameObject textObject = new GameObject("CampaignText", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panelObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(16f, 10f);
        textRect.offsetMax = new Vector2(-16f, -10f);

        campaignText = textObject.GetComponent<TMP_Text>();
        campaignText.fontSize = 26f;
        campaignText.enableAutoSizing = true;
        campaignText.fontSizeMin = 18f;
        campaignText.fontSizeMax = 26f;
        campaignText.alignment = TextAlignmentOptions.Left;
        campaignText.color = Color.white;
        campaignText.raycastTarget = false;
    }

    private string GetCampaignLine()
    {
        if (!showCampaignHud || campaignProgress == null)
            return string.Empty;

        if (campaignProgress.GameMode == GameProgressMode.Endless)
            return "Modo livre";

        if (campaignProgress.IsCampaignCompleted)
            return $"Campanha concluida\n<color={debtColor}>Nova divida: -R$ {currentDebt}</color>\nModo livre liberado";

        if (campaignProgress.HasFailedCurrentQuest)
            return $"Quest {campaignProgress.CurrentQuestIndex}/{campaignProgress.MaxQuestCount}\n<color={debtColor}>Prazo encerrado</color>\nVolte ao menu ou reinicie.";

        string deadlineColor = campaignProgress.DaysRemainingInCurrentQuest <= 1 ? warningColor : "#FFFFFF";
        string deadline = $"Prazo: <color={deadlineColor}>{campaignProgress.DaysRemainingInCurrentQuest} dia(s)</color>";

        if (campaignProgress.CurrentQuestRequiresSpecialDelivery)
        {
            string fishName = campaignProgress.SpecialDeliveryFish != null
                ? campaignProgress.SpecialDeliveryFish.fishName
                : "peixe especial";

            return $"Quest {campaignProgress.CurrentQuestIndex}/{campaignProgress.MaxQuestCount}: entrega especial\n{deadline}\nEntregue {campaignProgress.SpecialDeliveryQuantity}x {fishName}";
        }

        return $"Quest {campaignProgress.CurrentQuestIndex}/{campaignProgress.MaxQuestCount}: {campaignProgress.CurrentQuestName}\n{deadline}\nMeta: R$ {campaignProgress.QuestDebtPaidAmount}/{campaignProgress.QuestDebtPaymentTarget}";
    }
}
