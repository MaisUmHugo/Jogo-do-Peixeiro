using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static GameManager;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance;

    [Header("Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private GameObject controlsPanel;

    [Header("Pause Visibility")]
    [SerializeField] private GameObject[] panelsToHideWhilePaused;
    [SerializeField] private bool restoreHiddenPanelsOnResume = true;
    [SerializeField] private bool hidePanelsWithCanvasGroup = true;
    [SerializeField] private bool hideHudWithModalManager = true;
    [SerializeField] private bool pauseTimeWithModalManager = true;

    [Header("Navigation")]
    [SerializeField] private Selectable pauseFirstSelected;
    [SerializeField] private Selectable confirmFirstSelected;
    [SerializeField] private Selectable controlsFirstSelected;

    // guarda ação que será executada após confirmação 
    private Action confirmAction;

    // guarda estado antes do pause (OnFoot, OnBoat, etc)
    private GameManager.GameState stateBeforePause;
    private HiddenPanelState[] hiddenPanelStates;
    private Selectable pauseLastSelected;
    private int pauseModalToken = UIModalManager.InvalidToken;

    private struct HiddenPanelState
    {
        public GameObject Panel;
        public CanvasGroup CanvasGroup;
        public bool WasActiveSelf;
        public bool WasHiddenWithCanvasGroup;
        public float OriginalAlpha;
        public bool OriginalInteractable;
        public bool OriginalBlocksRaycasts;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        // conecta no evento de pause do InputHandler
        if (InputHandler.instance != null)
            InputHandler.instance.onPausePressed += HandlePause;

        // garante que todos os painéis começam desligados
        if (pausePanel != null) pausePanel.SetActive(false);
        if (confirmPanel != null) confirmPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        // remove inscrição do evento (evita bugs/memory leak)
        if (InputHandler.instance != null)
            InputHandler.instance.onPausePressed -= HandlePause;
    }

    private void HandlePause()
    {
        // evita null reference
        if (GameManager.instance == null)
            return;

        if (GameManager.instance.currentState == GameManager.GameState.Paused)
        {
            HandlePausePanelBack();
            return;
        }

        if (GameManager.instance.currentState != GameManager.GameState.Paused &&
            InvertoryManager.TryHandleOpenInventoryPauseInput())
        {
            return;
        }

        if (UIModalManager.TryHandleBack())
            return;

        if (InputHandler.instance != null && InputHandler.instance.WasBackInputStartedOverUi)
            return;

        if (GameManager.instance.currentState != GameManager.GameState.Paused &&
            UIModalManager.IsPauseBlocked)
        {
            return;
        }

        // se estiver na confirmação - cancela (volta pro pause)
        if (confirmPanel != null && confirmPanel.activeSelf)
        {
            ConfirmNo();
            return;
        }

        if (controlsPanel != null && controlsPanel.activeSelf)
        {
            CloseControlsFromPause();
            return;
        }

        // toggle pause
        if (GameManager.instance.currentState == GameManager.GameState.Paused)
            ResumeGame();
        else
            PauseGame();
    }

    private void HandlePausePanelBack()
    {
        if (confirmPanel != null && confirmPanel.activeSelf)
        {
            ConfirmNo();
            return;
        }

        if (controlsPanel != null && controlsPanel.activeSelf)
        {
            CloseControlsFromPause();
            return;
        }

        ResumeGame();
    }

    public void PauseGame()
    {
        if (GameManager.instance == null)
            return;

        // evita pausar duas vezes
        if (GameManager.instance.currentState == GameManager.GameState.Paused)
            return;

        // salva estado atual antes de pausar
        stateBeforePause = GameManager.instance.currentState;

        GameManager.instance.SetState(GameManager.GameState.Paused);

        if (pauseTimeWithModalManager || hideHudWithModalManager)
        {
            UIModalRequest modalRequest = UIModalRequest.Create(
                this,
                pauseTimeWithModalManager,
                hideHudWithModalManager,
                false
            );

            pauseModalToken = UIModalManager.PushModal(modalRequest);
        }
        else
        {
            Time.timeScale = 0f;
        }

        HideConfiguredPanelsForPause();

        // ativa painel de pause e desativa outros
        if (pausePanel != null) pausePanel.SetActive(true);
        if (confirmPanel != null) confirmPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);

        SelectPausePanel();
    }

    // função para os botões abaixo
    public void ResumeGame()
    {
        if (GameManager.instance == null)
            return;

        // só volta se estiver pausado
        if (GameManager.instance.currentState != GameManager.GameState.Paused)
            return;

        // destrava o tempo
        if (pauseModalToken != UIModalManager.InvalidToken)
            UIModalManager.PopModal(ref pauseModalToken);
        else
            Time.timeScale = 1f;

        // volta para estado anterior (OnFoot, OnBoat, etc)
        GameManager.instance.SetState(stateBeforePause);

        RestoreConfiguredPanelsAfterPause();

        // desativa todos os painéis
        if (pausePanel != null) pausePanel.SetActive(false);
        if (confirmPanel != null) confirmPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);

        // limpa ação pendente
        ClearPauseSelection();
        pauseLastSelected = null;
        confirmAction = null;
    }

    public void OpenControlsFromPause()
    {
        StorePauseSelection();

        if (pausePanel != null) pausePanel.SetActive(false);
        if (confirmPanel != null) confirmPanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(true);

        SelectControlsPanel();
    }

    public void CloseControlsFromPause()
    {
        // volta para o pause
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);

        SelectPausePanel();
    }

    public void ShowConfirmation(Action action)
    {
        // guarda ação que será executada ao confirmar
        confirmAction = action;
        StorePauseSelection();

        // abre painel de confirmação
        if (pausePanel != null) pausePanel.SetActive(false);
        if (controlsPanel != null) controlsPanel.SetActive(false);
        if (confirmPanel != null) confirmPanel.SetActive(true);

        SelectConfirmPanel();
    }

    public void ConfirmYes()
    {
        confirmAction?.Invoke();

        // limpa referência
        confirmAction = null;
    }

    public void ConfirmNo()
    {
        if (confirmPanel != null) confirmPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);

        confirmAction = null;

        SelectPausePanel();
    }

    public void OnClickRestart()
    {
        ShowConfirmation(() =>
        {
            if (pauseModalToken != UIModalManager.InvalidToken)
                UIModalManager.PopModal(ref pauseModalToken);

            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        });
    }

    public void OnClickBackToMenu()
    {
        ShowConfirmation(() =>
        {
            if (pauseModalToken != UIModalManager.InvalidToken)
                UIModalManager.PopModal(ref pauseModalToken);

            Time.timeScale = 1f;
            SceneManager.LoadScene("Main Menu");
        });
    }

    private void HideConfiguredPanelsForPause()
    {
        if (panelsToHideWhilePaused == null || panelsToHideWhilePaused.Length == 0)
            return;

        hiddenPanelStates = new HiddenPanelState[panelsToHideWhilePaused.Length];

        for (int i = 0; i < panelsToHideWhilePaused.Length; i++)
        {
            GameObject panel = panelsToHideWhilePaused[i];

            if (panel == null || IsPausePanelOrParent(panel))
                continue;

            HiddenPanelState state = new HiddenPanelState
            {
                Panel = panel,
                WasActiveSelf = panel.activeSelf
            };

            if (hidePanelsWithCanvasGroup && panel.activeInHierarchy)
            {
                CanvasGroup canvasGroup = panel.GetComponent<CanvasGroup>();

                if (canvasGroup == null)
                    canvasGroup = panel.AddComponent<CanvasGroup>();

                state.CanvasGroup = canvasGroup;
                state.WasHiddenWithCanvasGroup = true;
                state.OriginalAlpha = canvasGroup.alpha;
                state.OriginalInteractable = canvasGroup.interactable;
                state.OriginalBlocksRaycasts = canvasGroup.blocksRaycasts;

                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
            else
            {
                panel.SetActive(false);
            }

            hiddenPanelStates[i] = state;
        }
    }

    private void RestoreConfiguredPanelsAfterPause()
    {
        if (!restoreHiddenPanelsOnResume)
            return;

        if (hiddenPanelStates == null)
            return;

        for (int i = 0; i < hiddenPanelStates.Length; i++)
        {
            HiddenPanelState state = hiddenPanelStates[i];
            GameObject panel = state.Panel;

            if (panel == null || IsPausePanelOrParent(panel))
                continue;

            if (state.WasHiddenWithCanvasGroup && state.CanvasGroup != null)
            {
                state.CanvasGroup.alpha = state.OriginalAlpha;
                state.CanvasGroup.interactable = state.OriginalInteractable;
                state.CanvasGroup.blocksRaycasts = state.OriginalBlocksRaycasts;
                continue;
            }

            panel.SetActive(state.WasActiveSelf);
        }

        hiddenPanelStates = null;
    }

    private bool IsPausePanelOrParent(GameObject panel)
    {
        return IsSameOrParent(panel, pausePanel) ||
               IsSameOrParent(panel, confirmPanel) ||
               IsSameOrParent(panel, controlsPanel);
    }

    private bool IsSameOrParent(GameObject candidate, GameObject child)
    {
        if (candidate == null || child == null)
            return false;

        return candidate == child || child.transform.IsChildOf(candidate.transform);
    }

    private void SelectPausePanel()
    {
        Selectable target = pauseLastSelected != null ? pauseLastSelected : pauseFirstSelected;
        UISelectionHelper.Select(target, pausePanel);
    }

    private void SelectConfirmPanel()
    {
        UISelectionHelper.Select(confirmFirstSelected, confirmPanel);
    }

    private void SelectControlsPanel()
    {
        UISelectionHelper.Select(controlsFirstSelected, controlsPanel);
    }

    private void ClearPauseSelection()
    {
        UISelectionHelper.ClearSelection(pausePanel);
        UISelectionHelper.ClearSelection(confirmPanel);
        UISelectionHelper.ClearSelection(controlsPanel);
    }

    private void StorePauseSelection()
    {
        Selectable current = UISelectionHelper.CurrentSelectableInScope(pausePanel);

        if (current != null)
            pauseLastSelected = current;
    }
}
