using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string gameSceneName = "Game";

    [Header("Panels")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject howToPlayPanel;
    [SerializeField] private GameObject confirmPanel;
    [Header("Confirm Text")]
    [SerializeField] private TMP_Text confirmText;

    private string defaultConfirmMessage = "Tem certeza?";

    private Action confirmAction;

    // SPOOKY MODE 👻
    private int spookyClicks = 0;
    private bool waitingForSpookyClicks = false;
    private const int requiredClicks = 10;

    private void Start()
    {
        ShowMenu();
        Time.timeScale = 1f;
        PlayerPrefs.SetInt("SpookyMode", 0);
    }

    // MENU

    public void PlayGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    public void spookymode()
    {
        spookyClicks = 0;
        waitingForSpookyClicks = true;

        confirmText.text = defaultConfirmMessage;

        ShowConfirmation(() =>
        {
            spookyClicks++;

            // adiciona mais um ?
            confirmText.text += "?";

            Debug.Log("Spooky click: " + spookyClicks);

            if (spookyClicks >= requiredClicks)
            {
                ActivateSpookyMode();
            }
            else
            {
                ShowConfirmation(confirmAction);
            }
        });
    }

    private void ActivateSpookyMode()
    {
        waitingForSpookyClicks = false;

        PlayerPrefs.SetInt("SpookyMode", 1);

        confirmText.text = defaultConfirmMessage;

        ShowMenu();
    }

    public void OnClickQuit()
    {
        ShowConfirmation(() =>
        {
            Application.Quit();
            Debug.Log("Saindo do jogo...");
        });
    }

    // PAINÉIS

    public void OpenOptions()
    {
        ShowOnly(optionsPanel);
    }

    public void OpenHowToPlay()
    {
        ShowOnly(howToPlayPanel);
    }

    public void BackToMenu()
    {
        ShowMenu();
    }

    private void ShowMenu()
    {
        ShowOnly(menuPanel);
    }

    private void ShowOnly(GameObject panel)
    {
        if (menuPanel != null) menuPanel.SetActive(panel == menuPanel);
        if (optionsPanel != null) optionsPanel.SetActive(panel == optionsPanel);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(panel == howToPlayPanel);
        if (confirmPanel != null) confirmPanel.SetActive(panel == confirmPanel);
    }

    // CONFIRMAÇÃO

    public void ShowConfirmation(Action action)
    {
        confirmAction = action;
        ShowOnly(confirmPanel);
    }

    public void ConfirmYes()
    {
        confirmAction?.Invoke();
    }

    public void ConfirmNo()
    {
        confirmAction = null;
        waitingForSpookyClicks = false;

        confirmText.text = defaultConfirmMessage;

        ShowMenu();
    }
}