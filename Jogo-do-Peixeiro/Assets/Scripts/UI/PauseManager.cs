using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GameManager;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance;

    [Header("Panels")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private GameObject howToPlayPanel;

    // guarda ação que será executada após confirmação 
    private Action confirmAction;

    // guarda estado antes do pause (OnFoot, OnBoat, etc)
    private GameManager.GameState stateBeforePause;

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
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
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

        // se estiver na confirmação - cancela (volta pro pause)
        if (confirmPanel != null && confirmPanel.activeSelf)
        {
            ConfirmNo();
            return;
        }

        // se estiver no "Como Jogar" - volta pro pause
        if (howToPlayPanel != null && howToPlayPanel.activeSelf)
        {
            CloseHowToPlayFromPause();
            return;
        }

        // toggle pause
        if (GameManager.instance.currentState == GameManager.GameState.Paused)
            ResumeGame();
        else
            PauseGame();
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

        Time.timeScale = 0f;

        // ativa painel de pause e desativa outros
        if (pausePanel != null) pausePanel.SetActive(true);
        if (confirmPanel != null) confirmPanel.SetActive(false);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
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
        Time.timeScale = 1f;

        // volta para estado anterior (OnFoot, OnBoat, etc)
        GameManager.instance.SetState(stateBeforePause);

        // desativa todos os painéis
        if (pausePanel != null) pausePanel.SetActive(false);
        if (confirmPanel != null) confirmPanel.SetActive(false);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);

        // limpa ação pendente
        confirmAction = null;
    }

    public void OpenHowToPlayFromPause()
    {
        // abre painel "como jogar"
        if (pausePanel != null) pausePanel.SetActive(false);
        if (confirmPanel != null) confirmPanel.SetActive(false);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(true);
    }

    public void CloseHowToPlayFromPause()
    {
        // volta para o pause
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
        if (pausePanel != null) pausePanel.SetActive(true);
    }

    public void ShowConfirmation(Action action)
    {
        // guarda ação que será executada ao confirmar
        confirmAction = action;

        // abre painel de confirmação
        if (pausePanel != null) pausePanel.SetActive(false);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(false);
        if (confirmPanel != null) confirmPanel.SetActive(true);
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
    }

    public void OnClickRestart()
    {
        ShowConfirmation(() =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        });
    }

    public void OnClickBackToMenu()
    {
        ShowConfirmation(() =>
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene("Main Menu");
        });
    }
}