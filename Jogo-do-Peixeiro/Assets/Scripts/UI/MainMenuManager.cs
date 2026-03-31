using System;
using System.Collections;
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
    [SerializeField] private GameObject creditsPanel;

    [Header("Confirm UI")]
    [SerializeField] private TMP_Text confirmText;
    [SerializeField] private Transform yesButton;
    [SerializeField] private Transform noButton;

    [Header("Spooky Button")]
    [SerializeField] private GameObject spookyButton;

    [Header("Shake Settings")]
    [SerializeField] private float shakeIntensity = 4f;
    [SerializeField] private float shakeDuration = 0.15f;

    private string defaultConfirmMessage = "Tem certeza?";

    private Action confirmAction;

    // SPOOKY MODE 👻
    private int spookyClicks = 0;
    private const int requiredClicks = 10;

    private Vector3 originalTextPos;
    private Vector3 yesOriginalPos;
    private Vector3 noOriginalPos;

    private void Start()
    {
        ShowMenu();

        Time.timeScale = 1f;

        // sempre começa desativado
        PlayerPrefs.SetInt("SpookyMode", 0);

        if (spookyButton != null)
            spookyButton.SetActive(true);

        originalTextPos = confirmText.transform.localPosition;

        if (yesButton != null)
            yesOriginalPos = yesButton.localPosition;

        if (noButton != null)
            noOriginalPos = noButton.localPosition;
    }

    // BOTÃO PLAY
    public void PlayGame()
    {
        SceneManager.LoadScene(gameSceneName);
    }

    // BOTÃO SPOOKY 👻
    public void spookymode()
    {
        spookyClicks = 0;

        confirmText.text = defaultConfirmMessage;
        confirmText.color = Color.white;

        ResetButtonPositions();

        ShowConfirmation(() =>
        {
            spookyClicks++;

            // adiciona mais ?
            confirmText.text += "?";

            // vai ficando vermelho
            float t = (float)spookyClicks / requiredClicks;
            confirmText.color = Color.Lerp(Color.white, Color.red, t);

            // tremer texto
            StartCoroutine(ShakeText());

            // troca posição dos botões no final
            if (spookyClicks == requiredClicks - 1)
            {
                SwapButtons();
            }

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

    // ATIVA O SPOOKY MODE
    private void ActivateSpookyMode()
    {
        PlayerPrefs.SetInt("SpookyMode", 1);

        confirmText.text = defaultConfirmMessage;
        confirmText.color = Color.white;

        ResetButtonPositions();

        // esconde botão spooky
        if (spookyButton != null)
            spookyButton.SetActive(false);

        ShowMenu();

        Debug.Log("👻 SPOOKY MODE ATIVADO");
    }

    // BOTÃO SAIR
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

    public void OpenCredits()
    {
        ShowOnly(creditsPanel);
    }

    private void ShowOnly(GameObject panel)
    {
        if (menuPanel != null) menuPanel.SetActive(panel == menuPanel);
        if (optionsPanel != null) optionsPanel.SetActive(panel == optionsPanel);
        if (howToPlayPanel != null) howToPlayPanel.SetActive(panel == howToPlayPanel);
        if (confirmPanel != null) confirmPanel.SetActive(panel == confirmPanel);
        if (creditsPanel != null) creditsPanel.SetActive(panel == creditsPanel);
    }

    // CONFIRMAR
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

        confirmText.text = defaultConfirmMessage;
        confirmText.color = Color.white;

        ResetButtonPositions();

        ShowMenu();
    }

    // EFEITO TREMER TEXTO
    private IEnumerator ShakeText()
    {
        float timer = 0f;

        while (timer < shakeDuration)
        {
            timer += Time.unscaledDeltaTime;

            Vector3 offset =
                new Vector3(
                    UnityEngine.Random.Range(-1f, 1f),
                    UnityEngine.Random.Range(-1f, 1f),
                    0f
                ) * shakeIntensity;

            confirmText.transform.localPosition =
                originalTextPos + offset;

            yield return null;
        }

        confirmText.transform.localPosition = originalTextPos;
    }

    // TROCAR POSIÇÃO DOS BOTÕES
    private void SwapButtons()
    {
        if (yesButton == null || noButton == null)
            return;

        yesButton.localPosition = noOriginalPos;
        noButton.localPosition = yesOriginalPos;
    }

    private void ResetButtonPositions()
    {
        if (yesButton != null)
            yesButton.localPosition = yesOriginalPos;

        if (noButton != null)
            noButton.localPosition = noOriginalPos;
    }
}