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
    #region Fields

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

    [Header("Button Groups")]
    [SerializeField] private GameObject completionButtonsGroup;
    [SerializeField] private GameObject failureButtonsGroup;

    [Header("Buttons")]
    [SerializeField] private Button retryButton;
    [SerializeField] private Button completionMainMenuButton;
    [SerializeField] private Button failureMainMenuButton;

    [Header("Navigation")]
    [SerializeField] private Selectable firstSelected;
    [SerializeField] private Selectable completionFirstSelected;
    [SerializeField] private Selectable failureFirstSelected;

    [Header("Modal")]
    [SerializeField] private bool hideHudWhileShowing = true;
    [SerializeField] private bool blockPauseWhileShowing = true;
    [SerializeField] private bool pauseTimeWhileShowing = true;

    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "Main Menu";

    private int modalToken = UIModalManager.InvalidToken;
    private bool areButtonsBound;

    public bool IsShowing { get; private set; }
    public GameOutcomeType CurrentOutcomeType { get; private set; }

    private GameObject PanelObject => panel != null ? panel : gameObject;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        ResolveReferences();
        BindButtons();

        if (closeOnAwake)
            CloseImmediate();
    }

    private void OnEnable()
    {
        ResolveReferences();
        BindButtons();
    }

    private void OnDisable()
    {
        UnbindButtons();
        UIModalManager.PopModal(ref modalToken);
    }

    #endregion

    #region Public UI Actions

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

        SetButtonGroups(_outcomeType);
        PushModalState(_pauseTime);

        if (GameManager.instance != null)
            GameManager.instance.SetState(GameManager.GameState.InUI);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        UISelectionHelper.Select(GetFirstSelected(_outcomeType), PanelObject);
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

    #endregion

    #region Panel State

    private void CloseImmediate()
    {
        UISelectionHelper.ClearSelection(PanelObject);
        IsShowing = false;
        UIModalManager.PopModal(ref modalToken);
        PanelObject.SetActive(false);
    }

    private void BindButtons()
    {
        if (areButtonsBound)
            return;

        if (retryButton != null)
            retryButton.onClick.AddListener(RestartScene);

        if (completionMainMenuButton != null)
            completionMainMenuButton.onClick.AddListener(GoToMainMenu);

        if (failureMainMenuButton != null)
            failureMainMenuButton.onClick.AddListener(GoToMainMenu);

        areButtonsBound = true;
    }

    private void UnbindButtons()
    {
        if (!areButtonsBound)
            return;

        if (retryButton != null)
            retryButton.onClick.RemoveListener(RestartScene);

        if (completionMainMenuButton != null)
            completionMainMenuButton.onClick.RemoveListener(GoToMainMenu);

        if (failureMainMenuButton != null)
            failureMainMenuButton.onClick.RemoveListener(GoToMainMenu);

        areButtonsBound = false;
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

    private void SetButtonGroups(GameOutcomeType _outcomeType)
    {
        SetObjectActive(completionButtonsGroup, _outcomeType == GameOutcomeType.Completion);
        SetObjectActive(failureButtonsGroup, _outcomeType == GameOutcomeType.Failure);
    }

    private Selectable GetFirstSelected(GameOutcomeType _outcomeType)
    {
        Selectable modeSelected = _outcomeType == GameOutcomeType.Failure
            ? failureFirstSelected
            : completionFirstSelected;

        if (UISelectionHelper.IsUsable(modeSelected))
            return modeSelected;

        if (UISelectionHelper.IsUsable(firstSelected))
            return firstSelected;

        if (_outcomeType == GameOutcomeType.Failure && UISelectionHelper.IsUsable(retryButton))
            return retryButton;

        if (_outcomeType == GameOutcomeType.Completion && UISelectionHelper.IsUsable(completionMainMenuButton))
            return completionMainMenuButton;

        if (_outcomeType == GameOutcomeType.Failure && UISelectionHelper.IsUsable(failureMainMenuButton))
            return failureMainMenuButton;

        if (UISelectionHelper.IsUsable(completionMainMenuButton))
            return completionMainMenuButton;

        if (UISelectionHelper.IsUsable(failureMainMenuButton))
            return failureMainMenuButton;

        return PanelObject.GetComponentInChildren<Selectable>(true);
    }

    #endregion

    #region Reference Resolution

    private void ResolveReferences()
    {
        if (panel == null)
            panel = gameObject;

        if (titleText == null)
            titleText = FindChildText("TitleText", "TituloText", "OutcomeTitleText", "ResultTitleText");

        if (messageText == null)
            messageText = FindChildText("MessageText", "MensagemText", "OutcomeMessageText", "ResultMessageText");

        if (completionButtonsGroup == null)
            completionButtonsGroup = FindChildGameObject("CompletionButtonsGroup", "CompleteButtonsGroup", "ConclusionButtonsGroup");

        if (failureButtonsGroup == null)
            failureButtonsGroup = FindChildGameObject("FailureButtonsGroup", "FailButtonsGroup", "FailedButtonsGroup");

        if (retryButton == null)
            retryButton = FindChildButton("RetryButton", "RestartButton", "TentarNovamenteButton");

        if (completionMainMenuButton == null)
            completionMainMenuButton = FindChildButton("CompletionMainMenuButton", "CompletionMenuButton", "MainMenuButton", "MenuButton", "MenuPrincipalButton");

        if (failureMainMenuButton == null)
            failureMainMenuButton = FindChildButton("FailureMainMenuButton", "FailureMenuButton", "FailedMainMenuButton");

        if (failureMainMenuButton == null && failureButtonsGroup != null)
            failureMainMenuButton = FindChildButtonInRoot(failureButtonsGroup, "MainMenuButton", "MenuButton", "MenuPrincipalButton");
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

    private Button FindChildButton(params string[] _names)
    {
        Button[] buttons = GetComponentsInChildren<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];

            if (button == null)
                continue;

            for (int j = 0; j < _names.Length; j++)
            {
                if (string.Equals(button.gameObject.name, _names[j], System.StringComparison.OrdinalIgnoreCase))
                    return button;
            }
        }

        return null;
    }

    private Button FindChildButtonInRoot(GameObject _root, params string[] _names)
    {
        if (_root == null)
            return null;

        Button[] buttons = _root.GetComponentsInChildren<Button>(true);

        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];

            if (button == null)
                continue;

            for (int j = 0; j < _names.Length; j++)
            {
                if (string.Equals(button.gameObject.name, _names[j], System.StringComparison.OrdinalIgnoreCase))
                    return button;
            }
        }

        return null;
    }

    private GameObject FindChildGameObject(params string[] _names)
    {
        Transform[] children = GetComponentsInChildren<Transform>(true);

        for (int i = 0; i < children.Length; i++)
        {
            Transform child = children[i];

            if (child == null)
                continue;

            for (int j = 0; j < _names.Length; j++)
            {
                if (string.Equals(child.gameObject.name, _names[j], System.StringComparison.OrdinalIgnoreCase))
                    return child.gameObject;
            }
        }

        return null;
    }

    private void SetObjectActive(GameObject _target, bool _active)
    {
        if (_target != null)
            _target.SetActive(_active);
    }

    #endregion

    #region Text Helpers

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

    #endregion
}
