using UnityEngine;

public class CampaignOutcomeController : MonoBehaviour
{
    #region Fields

    [Header("References")]
    [SerializeField] private CampaignProgressSystem campaignProgress;
    [SerializeField] private GameOutcomePanelUI outcomePanel;
    [SerializeField] private DebtSystem debtSystem;

    [Header("Quest Failure")]
    [SerializeField] private bool showQuestFailurePanel = true;
    [SerializeField] private bool pauseOnQuestFailure = true;
    [SerializeField] private string questFailureTitle = "Quest falhou";
    [SerializeField, TextArea] private string questFailureMessage = "O prazo acabou antes de pagar a meta. Tente novamente ou volte ao menu principal.";

    [Header("Campaign Completion")]
    [SerializeField] private bool showCampaignCompletionPanel = true;
    [SerializeField] private bool pauseOnCampaignCompletion = true;
    [SerializeField] private bool saveGameOnCampaignCompletion = true;
    [SerializeField] private string campaignCompletionTitle = "Divida quitada";
    [SerializeField, TextArea] private string campaignCompletionMessage = "O cobrador pega o dinheiro, sorri e revela as outras dividas acumuladas. Divida atual: R$ {0}. O modo sem fim foi liberado no menu principal.";

    private bool isSubscribed;
    private bool hasShownQuestFailure;
    private bool hasShownCampaignCompletion;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        Subscribe();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    #endregion

    #region Event Handling

    private void HandleQuestAdvanced()
    {
        hasShownQuestFailure = false;
    }

    private void HandleQuestDeadlineExpired()
    {
        if (!showQuestFailurePanel || hasShownQuestFailure || campaignProgress == null)
            return;

        if (campaignProgress.GameMode != GameProgressMode.Campaign || campaignProgress.IsCampaignCompleted)
            return;

        CampaignQuestGuidanceController guidanceController = CampaignQuestGuidanceController.instance;
        bool tutorialGuidanceHandlesFailure = campaignProgress.IsCurrentQuestTutorial &&
                                             guidanceController != null &&
                                             guidanceController.isActiveAndEnabled &&
                                             guidanceController.IsTutorialRunning;

        if (tutorialGuidanceHandlesFailure)
            return;

        hasShownQuestFailure = true;

        if (outcomePanel == null)
        {
            Debug.LogWarning("[CampaignOutcomeController] OutcomePanel nao configurado.", this);
            return;
        }

        outcomePanel.ShowFailure(questFailureTitle, questFailureMessage, pauseOnQuestFailure);
    }

    private void HandleCampaignCompleted()
    {
        if (!showCampaignCompletionPanel || hasShownCampaignCompletion)
            return;

        hasShownCampaignCompletion = true;

        if (saveGameOnCampaignCompletion)
            GameSaveManager.GetOrCreate().SaveGame();

        if (outcomePanel == null)
        {
            Debug.LogWarning("[CampaignOutcomeController] OutcomePanel nao configurado.", this);
            return;
        }

        string message = campaignCompletionMessage.Replace("{0}", GetCurrentDebtValue().ToString());
        outcomePanel.ShowCompletion(campaignCompletionTitle, message, pauseOnCampaignCompletion);
    }

    #endregion

    #region Reference And Subscription Helpers

    private void ResolveReferences()
    {
        if (campaignProgress == null)
            campaignProgress = CampaignProgressSystem.GetOrCreate();

        if (outcomePanel == null)
            outcomePanel = FindFirstObjectByType<GameOutcomePanelUI>(FindObjectsInactive.Include);

        if (debtSystem == null)
            debtSystem = DebtSystem.GetOrCreate();
    }

    private void Subscribe()
    {
        if (isSubscribed || campaignProgress == null)
            return;

        campaignProgress.OnQuestAdvanced += HandleQuestAdvanced;
        campaignProgress.OnQuestDeadlineExpired += HandleQuestDeadlineExpired;
        campaignProgress.OnCampaignCompleted += HandleCampaignCompleted;
        isSubscribed = true;
    }

    private void Unsubscribe()
    {
        if (!isSubscribed || campaignProgress == null)
            return;

        campaignProgress.OnQuestAdvanced -= HandleQuestAdvanced;
        campaignProgress.OnQuestDeadlineExpired -= HandleQuestDeadlineExpired;
        campaignProgress.OnCampaignCompleted -= HandleCampaignCompleted;
        isSubscribed = false;
    }

    private int GetCurrentDebtValue()
    {
        if (debtSystem == null)
            debtSystem = DebtSystem.GetOrCreate();

        return debtSystem != null ? debtSystem.CurrentDebt : 0;
    }

    #endregion
}
