using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum GameOutcomeType
{
    Completion,
    Failure
}

public class GameOutcomePanelUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panel;
    [SerializeField] private bool closeOnAwake = true;

    [Header("Texts")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private string defaultCompletionTitle = "Concluido";
    [SerializeField] private string defaultCompletionMessage = "Objetivo concluido.";
    [SerializeField] private string defaultFailureTitle = "Falha";
    [SerializeField] private string defaultFailureMessage = "O objetivo falhou.";

    [Header("Navigation")]
    [SerializeField] private Selectable firstSelected;

    [Header("Modal")]
    [SerializeField] private bool hideHudWhileShowing = true;
    [SerializeField] private bool blockPauseWhileShowing = true;
    [SerializeField] private bool pauseTimeWhileShowing = true;

    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "Main Menu";

    private int modalToken = UIModalManager.InvalidToken;

    public bool IsShowing { get; private set; }
    public GameOutcomeType CurrentOutcomeType { get; private set; }

    private GameObject PanelObject => panel != null ? panel : gameObject;

    private void Awake()
    {
        ResolveReferences();

        if (closeOnAwake)
            CloseImmediate();
    }

    private void OnDisable()
    {
        UIModalManager.PopModal(ref modalToken);
    }

    public void ShowCompletion()
    {
        ShowCompletion(defaultCompletionTitle, defaultCompletionMessage);
    }

    public void ShowCompletion(string _title, string _message, bool _pauseTime = true)
    {
        Show(GameOutcomeType.Completion, _title, _message, _pauseTime);
    }

    public void ShowFailure()
    {
        ShowFailure(defaultFailureTitle, defaultFailureMessage);
    }

    public void ShowFailure(string _title, string _message, bool _pauseTime = true)
    {
        Show(GameOutcomeType.Failure, _title, _message, _pauseTime);
    }

    public void Show(GameOutcomeType _outcomeType, string _title, string _message, bool _pauseTime = true)
    {
        ResolveReferences();

        CurrentOutcomeType = _outcomeType;
        IsShowing = true;
        PanelObject.SetActive(true);

        if (titleText != null)
            titleText.text = string.IsNullOrWhiteSpace(_title) ? GetDefaultTitle(_outcomeType) : _title;

        if (messageText != null)
            messageText.text = string.IsNullOrWhiteSpace(_message) ? GetDefaultMessage(_outcomeType) : _message;

        PushModalState(_pauseTime);

        if (GameManager.instance != null)
            GameManager.instance.SetState(GameManager.GameState.InUI);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        UISelectionHelper.Select(firstSelected, PanelObject);
    }

    public void Close()
    {
        CloseImmediate();

        if (GameManager.instance != null)
            GameManager.instance.SetState(GameManager.GameState.OnFoot);
    }

    public void RestartScene()
    {
        CloseImmediate();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void GoToMainMenu()
    {
        CloseImmediate();
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void CloseImmediate()
    {
        UISelectionHelper.ClearSelection(PanelObject);
        IsShowing = false;
        UIModalManager.PopModal(ref modalToken);
        PanelObject.SetActive(false);
    }

    private void PushModalState(bool _pauseTime)
    {
        if (modalToken != UIModalManager.InvalidToken)
            return;

        UIModalRequest request = UIModalRequest.Create(
            this,
            pauseTimeWhileShowing && _pauseTime,
            hideHudWhileShowing,
            blockPauseWhileShowing,
            false,
            Close
        );

        modalToken = UIModalManager.PushModal(request);
    }

    private void ResolveReferences()
    {
        if (panel == null)
            panel = gameObject;

        if (titleText == null)
            titleText = FindChildText("TitleText", "TituloText", "OutcomeTitleText", "ResultTitleText");

        if (messageText == null)
            messageText = FindChildText("MessageText", "MensagemText", "OutcomeMessageText", "ResultMessageText");
    }

    private TMP_Text FindChildText(params string[] _names)
    {
        TMP_Text[] texts = GetComponentsInChildren<TMP_Text>(true);

        for (int i = 0; i < texts.Length; i++)
        {
            TMP_Text text = texts[i];

            if (text == null)
                continue;

            for (int j = 0; j < _names.Length; j++)
            {
                if (string.Equals(text.gameObject.name, _names[j], System.StringComparison.OrdinalIgnoreCase))
                    return text;
            }
        }

        return null;
    }

    private string GetDefaultTitle(GameOutcomeType _outcomeType)
    {
        return _outcomeType == GameOutcomeType.Failure
            ? defaultFailureTitle
            : defaultCompletionTitle;
    }

    private string GetDefaultMessage(GameOutcomeType _outcomeType)
    {
        return _outcomeType == GameOutcomeType.Failure
            ? defaultFailureMessage
            : defaultCompletionMessage;
    }
}
