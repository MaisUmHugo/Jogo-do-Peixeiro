using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuManager : MonoBehaviour
{
    [Header("Scene")]
    [SerializeField] private string gameSceneName = "Game";

    [Header("Panels")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject howToPlayPanel;
    [SerializeField] private GameObject confirmPanel;

    private Action confirmAction;

    private void Start()
    {
        ShowMenu();
        Time.timeScale = 1f;
    }

    // MENU

    public void PlayGame()
    {
        SceneManager.LoadScene(gameSceneName);
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
        confirmAction = null;
    }

    public void ConfirmNo()
    {
        confirmAction = null;
        ShowMenu();
    }
}