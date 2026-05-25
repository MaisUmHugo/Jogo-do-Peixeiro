using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    #region Fields

    [Header("Scene")]
    [SerializeField] private string gameSceneName = "Game";

    [Header("Music")]
    [SerializeField] private AudioClip menuMusic;
    [SerializeField] private bool playMenuMusicOnStart = true;

    [Header("Menu SFX")]
    [SerializeField, InspectorName("Button Submit SFX")] private AudioClip buttonSubmitSfx;
    [SerializeField, InspectorName("Selection Changed SFX")] private AudioClip selectionChangedSfx;
    [SerializeField, Range(0f, 1f), InspectorName("Button Submit SFX Volume")] private float buttonSubmitSfxVolume = 1f;
    [SerializeField, Range(0f, 1f), InspectorName("Selection Changed SFX Volume")] private float selectionChangedSfxVolume = 0.65f;
    [SerializeField, InspectorName("Auto Bind Button Submit SFX")] private bool autoBindButtonSubmitSfx = true;
    [SerializeField, InspectorName("Play Selection Changed SFX")] private bool playSelectionChangedSfx = true;

    [Header("Panels")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private GameObject modeSelectPanel;
    [SerializeField] private GameObject modeActionPanel;
    [SerializeField] private GameObject savePreviewPanel;
    [SerializeField] private GameObject optionsPanel;
    [SerializeField] private GameObject controlsPanel;
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private GameObject creditsPanel;

    [Header("Mode Select UI")]
    [SerializeField] private Button campaignModeButton;
    [SerializeField] private Button endlessModeButton;
    [SerializeField] private TMP_Text modeSelectDescriptionText;
    [SerializeField] private TMP_Text endlessLockedText;
    [SerializeField] private string endlessLockedMessage = "Complete a campanha para liberar o modo sem fim.";
    [SerializeField, Min(0.1f)] private float modeSelectWarningDuration = 1.8f;
    [SerializeField, Min(0f)] private float modeSelectButtonShakeDuration = 0.2f;
    [SerializeField, Min(0f)] private float modeSelectButtonShakeStrength = 8f;

    [Header("Button Lock Visuals")]
    [SerializeField] private Color lockedButtonColor = new Color(0.42f, 0.42f, 0.42f, 1f);

    [Header("Mode Action UI")]
    [SerializeField] private TMP_Text modeTitleText;
    [SerializeField] private TMP_Text modeDescriptionText;
    [SerializeField] private TMP_Text modeStatusText;
    [SerializeField] private Button modeContinueButton;
    [SerializeField] private Button modeNewGameButton;
    [SerializeField] private string campaignTitle = "Campanha";
    [SerializeField, TextArea] private string campaignDescription = "Comece ou continue a campanha principal.";
    [SerializeField] private string endlessTitle = "Modo sem fim";
    [SerializeField, TextArea] private string endlessDescription = "Jogue livremente depois de concluir a campanha.";

    [Header("Save Preview UI")]
    [SerializeField] private TMP_Text savePreviewModeText;
    [SerializeField] private TMP_Text savePreviewQuestText;
    [SerializeField] private TMP_Text savePreviewDebtText;
    [SerializeField] private TMP_Text savePreviewMoneyText;
    [SerializeField] private TMP_Text savePreviewPlayTimeText;
    [SerializeField] private TMP_Text savePreviewDayText;
    [SerializeField] private TMP_Text savePreviewStatusText;
    [SerializeField] private Button savePreviewContinueButton;

    [Header("Navigation")]
    [SerializeField] private Selectable menuFirstSelected;
    [SerializeField] private Selectable modeSelectFirstSelected;
    [SerializeField] private Selectable modeActionFirstSelected;
    [SerializeField] private Selectable savePreviewFirstSelected;
    [SerializeField] private Selectable optionsFirstSelected;
    [SerializeField] private Selectable controlsFirstSelected;
    [SerializeField] private Selectable creditsFirstSelected;
    [SerializeField] private Selectable confirmFirstSelected;

    [Header("Confirm UI")]
    [SerializeField] private TMP_Text confirmText;

    private readonly string defaultConfirmMessage = "Tem certeza?";

    private Action confirmAction;
    private Selectable menuLastSelected;
    private GameProgressMode selectedMode = GameProgressMode.Campaign;
    private bool hasSelectedMode;
    private GameSaveData cachedSaveData;
    private GameSaveData cachedCampaignSaveData;
    private GameSaveData cachedEndlessSaveData;
    private bool hasCachedSave;
    private bool hasCachedCampaignSave;
    private bool hasCachedEndlessSave;
    private Selectable lastModeSelectSelectable;
    private Coroutine modeSelectWarningCoroutine;
    private Coroutine endlessButtonShakeCoroutine;
    private RectTransform endlessModeButtonRect;
    private Vector2 endlessModeButtonOriginalAnchoredPosition;
    private bool hasEndlessModeButtonOriginalPosition;
    private ColorBlock endlessModeButtonDefaultColors;
    private bool hasEndlessModeButtonDefaultColors;
    private Button[] menuSfxButtons;
    private bool areMenuSfxButtonsBound;
    private GameObject lastSelectedForSfx;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        BindMenuButtonSfx();
    }

    private void Start()
    {
        Time.timeScale = 1f;

        BindMenuButtonSfx();
        PlayMenuMusic();
        RefreshSaveCache();
        ShowMenu();
    }

    private void Update()
    {
        UpdateMenuSelectionSfx();

        if (modeSelectPanel == null || !modeSelectPanel.activeInHierarchy)
            return;

        UpdateModeSelectDescriptionFromSelection(false);
    }

    private void OnDestroy()
    {
        UnbindMenuButtonSfx();
    }

    #endregion

    #region Main Menu Actions

    public void PlayGame()
    {
        if (modeSelectPanel == null && modeActionPanel == null)
        {
            selectedMode = GameProgressMode.Campaign;
            hasSelectedMode = true;
            StartSelectedNewGame();
            return;
        }

        OpenModeSelect();
    }

    public void ContinueGame()
    {
        GameSaveManager.RequestLoadOnNextScene(selectedMode);
        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenModeSelect()
    {
        RefreshSaveCache();
        StoreMenuSelection();
        UpdateModeSelectState();

        if (modeSelectPanel != null)
        {
            ShowOnly(modeSelectPanel);
            UISelectionHelper.Select(modeSelectFirstSelected, modeSelectPanel);
            return;
        }

        OpenCampaignModePanel();
    }

    public void OpenCampaignModePanel()
    {
        OpenModeActionPanel(GameProgressMode.Campaign);
    }

    public void OpenEndlessModePanel()
    {
        if (!IsEndlessUnlocked())
        {
            SelectEndlessModeOption();
            ShowEndlessLockedFeedback();
            return;
        }

        OpenModeActionPanel(GameProgressMode.Endless);
    }

    public void SelectCampaignModeOption()
    {
        SetModeSelectDescription(campaignDescription);
    }

    public void SelectEndlessModeOption()
    {
        SetModeSelectDescription(endlessDescription);
    }

    public void OpenSelectedSavePreview()
    {
        if (!hasSelectedMode)
            selectedMode = GameProgressMode.Campaign;

        if (!CanPreviewSelectedSave(out string reason))
        {
            SetModeStatus(reason);
            return;
        }

        UpdateSavePreview();
        GameObject targetPanel = savePreviewPanel != null ? savePreviewPanel : modeActionPanel;
        ShowOnly(targetPanel);
        UISelectionHelper.Select(savePreviewFirstSelected, targetPanel);
    }

    public void RefreshModeAvailabilityForDebug()
    {
        RefreshSaveCache();

        if (modeSelectPanel != null && modeSelectPanel.activeInHierarchy)
            UpdateModeSelectState();

        if (modeActionPanel != null && modeActionPanel.activeInHierarchy)
            UpdateModeActionPanel();

        if (savePreviewPanel != null && savePreviewPanel.activeInHierarchy)
            UpdateSavePreview();
    }

    public void ContinueSelectedSave()
    {
        if (!CanContinueSelectedMode(out string reason))
        {
            SetSavePreviewStatus(reason);
            return;
        }

        ContinueGame();
    }

    public void StartSelectedNewGame()
    {
        if (!hasSelectedMode)
            selectedMode = GameProgressMode.Campaign;

        RefreshSaveCache();

        if (selectedMode == GameProgressMode.Endless && !IsEndlessUnlocked())
        {
            SetModeStatus(endlessLockedMessage);
            return;
        }

        if (hasCachedSave)
        {
            string modeName = GetModeDisplayName(selectedMode).ToLowerInvariant();
            ShowConfirmation(() => StartSelectedNewGameConfirmed(), $"Iniciar novo jogo no {modeName} vai substituir o save atual. Continuar?");
            return;
        }

        StartSelectedNewGameConfirmed();
    }

    public void BackToModeSelect()
    {
        OpenModeSelect();
    }

    public void BackToModeAction()
    {
        if (!hasSelectedMode)
        {
            OpenModeSelect();
            return;
        }

        OpenModeActionPanel(selectedMode);
    }

    public void OpenOptions()
    {
        StoreMenuSelection();
        ShowOnly(optionsPanel);
        UISelectionHelper.Select(optionsFirstSelected, optionsPanel);
    }

    public void OpenControls()
    {
        StoreMenuSelection();
        ShowOnly(controlsPanel);
        UISelectionHelper.Select(controlsFirstSelected, controlsPanel);
    }

    public void BackToMenu()
    {
        ShowMenu();
    }

    public void OpenCredits()
    {
        StoreMenuSelection();
        ShowOnly(creditsPanel);
        UISelectionHelper.Select(creditsFirstSelected, creditsPanel);
    }

    public void OnClickQuit()
    {
        ShowConfirmation(() =>
        {
            Application.Quit();
            Debug.Log("Saindo do jogo...");
        });
    }

    #endregion

    #region Confirmation

    public void ShowConfirmation(Action action)
    {
        ShowConfirmation(action, defaultConfirmMessage);
    }

    public void ShowConfirmation(Action action, string message)
    {
        StoreMenuSelection();
        confirmAction = action;

        if (confirmText != null)
        {
            confirmText.text = string.IsNullOrWhiteSpace(message) ? defaultConfirmMessage : message;
            confirmText.color = Color.white;
        }

        ShowOnly(confirmPanel);
        UISelectionHelper.Select(confirmFirstSelected, confirmPanel);
    }

    public void ConfirmYes()
    {
        confirmAction?.Invoke();
    }

    public void ConfirmNo()
    {
        confirmAction = null;

        if (confirmText != null)
        {
            confirmText.text = defaultConfirmMessage;
            confirmText.color = Color.white;
        }

        ShowMenu();
    }

    #endregion

    #region Mode And Save Flow

    private void OpenModeActionPanel(GameProgressMode mode)
    {
        selectedMode = mode;
        hasSelectedMode = true;
        RefreshSaveCache();
        UpdateModeActionPanel();
        ShowOnly(modeActionPanel != null ? modeActionPanel : menuPanel);
        UISelectionHelper.Select(modeActionFirstSelected, modeActionPanel);
    }

    private void StartSelectedNewGameConfirmed()
    {
        GameSaveManager saveManager = GameSaveManager.GetOrCreate();
        saveManager.DeleteSave(selectedMode);
        saveManager.ResetTrackedPlayTime();
        GameSaveManager.ClearLoadRequest();

        CampaignProgressSystem campaignProgress = CampaignProgressSystem.GetOrCreate();

        if (selectedMode == GameProgressMode.Endless)
            campaignProgress.StartUnlockedEndlessMode();
        else
            campaignProgress.StartNewCampaign();

        DebtSystem debtSystem = DebtSystem.GetOrCreate();

        if (debtSystem != null)
        {
            if (selectedMode == GameProgressMode.Endless)
                debtSystem.SetDebt(campaignProgress.CampaignCompletionDebtAmount);
            else
                debtSystem.ResetDebt();
        }

        SceneManager.LoadScene(gameSceneName);
    }

    private void UpdateModeSelectState()
    {
        bool endlessUnlocked = IsEndlessUnlocked();

        SetButtonInteractable(campaignModeButton, true);
        SetButtonInteractable(endlessModeButton, true);
        SetButtonLockedVisual(endlessModeButton, !endlessUnlocked, ref endlessModeButtonDefaultColors, ref hasEndlessModeButtonDefaultColors);
        SetModeSelectWarning(string.Empty);

        UpdateModeSelectDescriptionFromSelection(true);
    }

    private void UpdateModeActionPanel()
    {
        bool isEndless = selectedMode == GameProgressMode.Endless;
        bool isAvailable = !isEndless || IsEndlessUnlocked();
        bool canContinue = CanContinueSelectedMode(out string continueReason);

        SetText(modeTitleText, isEndless ? endlessTitle : campaignTitle);
        SetText(modeDescriptionText, isEndless ? endlessDescription : campaignDescription);
        SetModeStatus(isAvailable ? string.Empty : endlessLockedMessage);

        bool canPreviewSave = CanPreviewSelectedSave(out string previewReason);

        SetButtonInteractable(modeContinueButton, isAvailable && canPreviewSave);
        SetButtonInteractable(modeNewGameButton, isAvailable);

        if (isAvailable && !canContinue && modeStatusText != null)
            SetModeStatus(canPreviewSave ? continueReason : previewReason);
    }

    private void UpdateSavePreview()
    {
        bool canContinue = CanContinueSelectedMode(out string reason);
        GameSaveData data = cachedSaveData;

        SetText(savePreviewModeText, data != null ? $"Modo: {GetSaveModeDisplayName(data.gameMode)}" : "Modo: --");
        SetText(savePreviewQuestText, data != null ? GetSaveQuestLine(data) : "Progresso: --");
        SetText(savePreviewDebtText, data != null ? $"Dívida: -R$ {Mathf.Max(0, data.currentDebt)}" : "Dívida: --");
        SetText(savePreviewMoneyText, data != null ? $"Dinheiro: R$ {data.playerMoney:0}" : "Dinheiro: --");
        SetText(savePreviewPlayTimeText, data != null ? $"Tempo jogado: {FormatPlayTime(data.playTimeSeconds)}" : "Tempo jogado: --");
        SetText(savePreviewDayText, data != null && data.dayCycle != null ? $"Dia: {data.dayCycle.currentDay}" : "Dia: --");
        SetSavePreviewStatus(canContinue ? string.Empty : reason);
        SetButtonInteractable(savePreviewContinueButton, canContinue);
    }

    private void RefreshSaveCache()
    {
        GameSaveManager saveManager = GameSaveManager.GetOrCreate();
        hasCachedCampaignSave = saveManager.TryReadSaveData(GameProgressMode.Campaign, out cachedCampaignSaveData);

        if (ShouldRemoveFailedSave(cachedCampaignSaveData))
        {
            saveManager.DeleteSave(GameProgressMode.Campaign);
            cachedCampaignSaveData = null;
            hasCachedCampaignSave = false;
        }

        hasCachedEndlessSave = saveManager.TryReadSaveData(GameProgressMode.Endless, out cachedEndlessSaveData);

        if (ShouldRemoveFailedSave(cachedEndlessSaveData))
        {
            saveManager.DeleteSave(GameProgressMode.Endless);
            cachedEndlessSaveData = null;
            hasCachedEndlessSave = false;
        }

        if (selectedMode == GameProgressMode.Endless)
        {
            cachedSaveData = cachedEndlessSaveData;
            hasCachedSave = hasCachedEndlessSave;
        }
        else
        {
            cachedSaveData = cachedCampaignSaveData;
            hasCachedSave = hasCachedCampaignSave;
        }
    }

    private bool ShouldRemoveFailedSave(GameSaveData data)
    {
        if (data == null || data.campaign == null)
            return false;

        return data.campaign.hasFailedCurrentQuest && !data.campaign.isCampaignCompleted;
    }

    private bool CanPreviewSelectedSave(out string reason)
    {
        reason = string.Empty;

        RefreshSelectedSaveCache();

        if (!hasCachedSave || cachedSaveData == null)
        {
            reason = "Nenhum save encontrado.";
            return false;
        }

        if (selectedMode == GameProgressMode.Campaign && cachedSaveData.gameMode != GameProgressMode.Campaign)
        {
            reason = "O save atual e do modo sem fim.";
            return false;
        }

        if (selectedMode == GameProgressMode.Endless && cachedSaveData.gameMode != GameProgressMode.Endless)
        {
            reason = "Nenhum save do modo sem fim encontrado.";
            return false;
        }

        return true;
    }

    private bool CanContinueSelectedMode(out string reason)
    {
        reason = string.Empty;

        RefreshSelectedSaveCache();

        if (!hasCachedSave || cachedSaveData == null)
        {
            reason = "Nenhum save encontrado.";
            return false;
        }

        CampaignSaveData campaignSave = cachedSaveData.campaign;

        if (selectedMode == GameProgressMode.Campaign)
        {
            if (cachedSaveData.gameMode != GameProgressMode.Campaign)
            {
                reason = "O save atual é do modo sem fim.";
                return false;
            }

            if (campaignSave != null && campaignSave.isCampaignCompleted)
            {
                reason = "Campanha concluída. Inicie uma nova campanha.";
                return false;
            }

            if (campaignSave != null && campaignSave.hasFailedCurrentQuest)
            {
                reason = "Campanha falhou. Inicie uma nova campanha.";
                return false;
            }

            return true;
        }

        if (cachedSaveData.gameMode != GameProgressMode.Endless)
        {
            reason = "Nenhum save do modo sem fim encontrado.";
            return false;
        }

        return true;
    }

    private bool IsEndlessUnlocked()
    {
        if (CampaignProgressSystem.Instance != null && CampaignProgressSystem.Instance.EndlessUnlocked)
            return true;

        if (hasCachedEndlessSave)
            return true;

        CampaignSaveData campaignSave = cachedCampaignSaveData != null ? cachedCampaignSaveData.campaign : null;
        return campaignSave != null && (campaignSave.endlessUnlocked || campaignSave.isCampaignCompleted);
    }

    private void RefreshSelectedSaveCache()
    {
        if (selectedMode == GameProgressMode.Endless)
        {
            cachedSaveData = cachedEndlessSaveData;
            hasCachedSave = hasCachedEndlessSave;
            return;
        }

        cachedSaveData = cachedCampaignSaveData;
        hasCachedSave = hasCachedCampaignSave;
    }

    private void UpdateModeSelectDescriptionFromSelection(bool force)
    {
        Selectable selected = UISelectionHelper.CurrentSelectableInScope(modeSelectPanel);

        if (selected == null && EventSystem.current != null)
            selected = EventSystem.current.currentSelectedGameObject != null
                ? EventSystem.current.currentSelectedGameObject.GetComponent<Selectable>()
                : null;

        if (selected == null)
            selected = modeSelectFirstSelected != null ? modeSelectFirstSelected : campaignModeButton;

        if (!force && selected == lastModeSelectSelectable)
            return;

        lastModeSelectSelectable = selected;

        if (selected == endlessModeButton)
        {
            if (!IsEndlessUnlocked())
            {
                ShowEndlessLockedFeedback();
                return;
            }

            SelectEndlessModeOption();

            return;
        }

        SelectCampaignModeOption();
    }

    private void SetModeSelectDescription(string message)
    {
        SetText(modeSelectDescriptionText, message);
    }

    private void ShowEndlessLockedFeedback()
    {
        SetModeSelectWarning(endlessLockedMessage);
        SetModeSelectDescription(endlessLockedMessage);

        if (modeSelectWarningCoroutine != null)
            StopCoroutine(modeSelectWarningCoroutine);

        modeSelectWarningCoroutine = StartCoroutine(HideModeSelectWarningAfterDelay());

        if (endlessButtonShakeCoroutine != null)
            StopCoroutine(endlessButtonShakeCoroutine);

        endlessButtonShakeCoroutine = StartCoroutine(ShakeEndlessModeButton());
    }

    private void SetModeSelectWarning(string message)
    {
        SetText(endlessLockedText, message, true);
    }

    private IEnumerator HideModeSelectWarningAfterDelay()
    {
        yield return new WaitForSecondsRealtime(modeSelectWarningDuration);
        SetModeSelectWarning(string.Empty);
        modeSelectWarningCoroutine = null;
    }

    private IEnumerator ShakeEndlessModeButton()
    {
        CacheEndlessModeButtonRect();

        if (endlessModeButtonRect == null || modeSelectButtonShakeDuration <= 0f || modeSelectButtonShakeStrength <= 0f)
        {
            endlessButtonShakeCoroutine = null;
            yield break;
        }

        float timer = 0f;

        while (timer < modeSelectButtonShakeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float offset = UnityEngine.Random.Range(-1f, 1f) * modeSelectButtonShakeStrength;
            endlessModeButtonRect.anchoredPosition = endlessModeButtonOriginalAnchoredPosition + new Vector2(offset, 0f);
            yield return null;
        }

        endlessModeButtonRect.anchoredPosition = endlessModeButtonOriginalAnchoredPosition;
        endlessButtonShakeCoroutine = null;
    }

    private void CacheEndlessModeButtonRect()
    {
        if (hasEndlessModeButtonOriginalPosition)
            return;

        endlessModeButtonRect = endlessModeButton != null ? endlessModeButton.GetComponent<RectTransform>() : null;

        if (endlessModeButtonRect == null)
            return;

        endlessModeButtonOriginalAnchoredPosition = endlessModeButtonRect.anchoredPosition;
        hasEndlessModeButtonOriginalPosition = true;
    }

    private string GetSaveQuestLine(GameSaveData data)
    {
        if (data == null)
            return "Progresso: --";

        if (data.gameMode == GameProgressMode.Endless)
            return "Progresso: Modo livre";

        CampaignSaveData campaignSave = data.campaign;

        if (campaignSave == null)
            return "Progresso: Campanha";

        if (campaignSave.isCampaignCompleted)
            return "Progresso: Campanha concluída";

        if (campaignSave.hasFailedCurrentQuest)
            return $"Progresso: Quest {campaignSave.currentQuestIndex} falhou";

        return $"Progresso: Quest {campaignSave.currentQuestIndex}/{campaignSave.maxQuestCount}";
    }

    private string GetModeDisplayName(GameProgressMode mode)
    {
        return mode == GameProgressMode.Endless ? "Modo sem fim" : "Campanha";
    }

    private string GetSaveModeDisplayName(GameProgressMode mode)
    {
        return mode == GameProgressMode.Endless ? "Sem fim" : "Campanha";
    }

    private string FormatPlayTime(float seconds)
    {
        if (seconds <= 0f)
            return "--";

        TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);

        if (timeSpan.TotalHours >= 1d)
            return $"{(int)timeSpan.TotalHours:00}:{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";

        return $"{timeSpan.Minutes:00}:{timeSpan.Seconds:00}";
    }

    #endregion

    #region Panel Helpers

    private void PlayMenuMusic()
    {
        if (!playMenuMusicOnStart || AudioManager.Instance == null)
            return;

        AudioManager.Instance.PlayMusic(menuMusic);
    }

    private void BindMenuButtonSfx()
    {
        if (areMenuSfxButtonsBound || !autoBindButtonSubmitSfx)
            return;

        menuSfxButtons = GetComponentsInChildren<Button>(true);

        if (menuSfxButtons == null)
            return;

        for (int i = 0; i < menuSfxButtons.Length; i++)
        {
            if (menuSfxButtons[i] != null)
                menuSfxButtons[i].onClick.AddListener(PlayButtonSubmitSfx);
        }

        areMenuSfxButtonsBound = true;
    }

    private void UnbindMenuButtonSfx()
    {
        if (!areMenuSfxButtonsBound || menuSfxButtons == null)
            return;

        for (int i = 0; i < menuSfxButtons.Length; i++)
        {
            if (menuSfxButtons[i] != null)
                menuSfxButtons[i].onClick.RemoveListener(PlayButtonSubmitSfx);
        }

        areMenuSfxButtonsBound = false;
    }

    private void PlayButtonSubmitSfx()
    {
        PlayMenuSfx(buttonSubmitSfx, buttonSubmitSfxVolume);
    }

    private void UpdateMenuSelectionSfx()
    {
        if (!playSelectionChangedSfx || selectionChangedSfx == null || EventSystem.current == null)
            return;

        GameObject selected = EventSystem.current.currentSelectedGameObject;

        if (selected == lastSelectedForSfx)
            return;

        if (lastSelectedForSfx == null)
        {
            lastSelectedForSfx = selected;
            return;
        }

        lastSelectedForSfx = selected;

        if (!IsMenuSelectable(selected))
            return;

        PlayMenuSfx(selectionChangedSfx, selectionChangedSfxVolume);
    }

    private bool IsMenuSelectable(GameObject selected)
    {
        if (selected == null)
            return false;

        Transform selectedTransform = selected.transform;

        return IsChildOf(selectedTransform, transform) ||
               IsChildOf(selectedTransform, menuPanel) ||
               IsChildOf(selectedTransform, modeSelectPanel) ||
               IsChildOf(selectedTransform, modeActionPanel) ||
               IsChildOf(selectedTransform, savePreviewPanel) ||
               IsChildOf(selectedTransform, optionsPanel) ||
               IsChildOf(selectedTransform, controlsPanel) ||
               IsChildOf(selectedTransform, confirmPanel) ||
               IsChildOf(selectedTransform, creditsPanel);
    }

    private bool IsChildOf(Transform child, Component parent)
    {
        return parent != null && child != null && child.IsChildOf(parent.transform);
    }

    private bool IsChildOf(Transform child, GameObject parent)
    {
        return parent != null && child != null && child.IsChildOf(parent.transform);
    }

    private void PlayMenuSfx(AudioClip clip, float volume)
    {
        if (AudioManager.Instance == null || clip == null)
            return;

        AudioManager.Instance.PlaySfx(clip, volume);
    }

    private void ShowMenu()
    {
        RefreshSaveCache();
        ShowOnly(menuPanel);
        SelectMenuPanel();
    }

    private void ShowOnly(GameObject panel)
    {
        SetObjectActive(menuPanel, panel == menuPanel);
        SetObjectActive(modeSelectPanel, panel == modeSelectPanel);
        SetObjectActive(modeActionPanel, panel == modeActionPanel);
        SetObjectActive(savePreviewPanel, panel == savePreviewPanel);
        SetObjectActive(optionsPanel, panel == optionsPanel);
        SetObjectActive(controlsPanel, panel == controlsPanel);
        SetObjectActive(confirmPanel, panel == confirmPanel);
        SetObjectActive(creditsPanel, panel == creditsPanel);
    }

    private void SelectMenuPanel()
    {
        Selectable target = menuLastSelected != null ? menuLastSelected : menuFirstSelected;
        UISelectionHelper.Select(target, menuPanel);
    }

    private void StoreMenuSelection()
    {
        Selectable current = UISelectionHelper.CurrentSelectableInScope(menuPanel);

        if (current != null)
            menuLastSelected = current;
    }

    private void SetModeStatus(string message)
    {
        SetText(modeStatusText, message, true);
    }

    private void SetSavePreviewStatus(string message)
    {
        SetText(savePreviewStatusText, message, true);
    }

    private void SetText(TMP_Text text, string value, bool hideWhenEmpty = false)
    {
        if (text == null)
            return;

        bool hasValue = !string.IsNullOrWhiteSpace(value);
        text.text = hasValue ? value : string.Empty;

        if (hideWhenEmpty)
            text.gameObject.SetActive(hasValue);
    }

    private void SetButtonInteractable(Button button, bool interactable)
    {
        if (button == null)
            return;

        ApplyDisabledButtonColor(button);
        button.interactable = interactable;
    }

    private void SetButtonLockedVisual(Button button, bool locked, ref ColorBlock defaultColors, ref bool hasDefaultColors)
    {
        if (button == null)
            return;

        if (!hasDefaultColors)
        {
            defaultColors = button.colors;
            hasDefaultColors = true;
        }

        if (!locked)
        {
            button.colors = defaultColors;
            return;
        }

        ColorBlock colors = defaultColors;
        colors.normalColor = lockedButtonColor;
        colors.highlightedColor = ScaleColor(lockedButtonColor, 1.08f);
        colors.selectedColor = ScaleColor(lockedButtonColor, 1.08f);
        colors.pressedColor = ScaleColor(lockedButtonColor, 0.85f);
        colors.disabledColor = lockedButtonColor;
        button.colors = colors;
    }

    private void ApplyDisabledButtonColor(Button button)
    {
        ColorBlock colors = button.colors;
        colors.disabledColor = lockedButtonColor;
        button.colors = colors;
    }

    private Color ScaleColor(Color color, float multiplier)
    {
        return new Color(
            Mathf.Clamp01(color.r * multiplier),
            Mathf.Clamp01(color.g * multiplier),
            Mathf.Clamp01(color.b * multiplier),
            color.a
        );
    }

    private void SetObjectActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }

    #endregion
}
