using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DayCycle : MonoBehaviour
{
    public event Action<int> DayChanged;
    public event Action ForcedSleepRequested;
    public event Action<bool> VisualModeChanged;

    [Header("Referências")]
    [SerializeField] private Material skyboxMaterial;
    [SerializeField] private Light sun;
    [SerializeField] private Light moon;
    [SerializeField] private TextMeshProUGUI HourText;

    [Header("Tempo")]
    [SerializeField] private float dayDuration = 120f;
    [SerializeField] private float currentTime = 0f;
    [SerializeField] private float Clock;

    [Header("Curvas")]
    [SerializeField] private AnimationCurve sunIntensity;
    [SerializeField] private Gradient skyColor;
    [SerializeField] private Gradient groundColor;

    [Header("Skybox Settings")]
    [SerializeField] private float atmosphereThickness = 1.0f;
    [SerializeField] private float exposure = 1.3f;

    [Header("Dias")]
    [SerializeField] private int currentDay = 1;
    [SerializeField] private int totalDays = 3;
    [SerializeField] private TextMeshProUGUI DayText;
    [SerializeField] private int elapsedDays = 1;

    [Header("HUD Visuals")]
    [SerializeField] private Image dayProgressBackplate;
    [SerializeField] private Image dayProgressFill;
    [SerializeField] private RectTransform dayProgressTrack;
    [SerializeField] private RectTransform dayProgressHandle;
    [SerializeField] private bool configureDayProgressFill = true;
    [SerializeField] private bool resizeDayProgressFillRect = true;
    [SerializeField] private bool keepDayProgressHandleInsideTrack = true;
    [SerializeField] private bool tintDayProgressFillOverTime = true;
    [SerializeField] private Gradient dayProgressFillColorByTime = CreateDefaultDayProgressGradient();
    [SerializeField, Range(0f, 24f)] private float dayVisualStartHour = 6f;
    [SerializeField, Range(0f, 24f)] private float dayVisualEndHour = 18f;
    [SerializeField] private Color dayPrimaryTextColor = new Color(0.16f, 0.35f, 0.85f, 1f);
    [SerializeField] private Color daySecondaryTextColor = Color.black;
    [SerializeField] private Color nightPrimaryTextColor = new Color(1f, 0.82f, 0.2f, 1f);
    [SerializeField] private Color nightSecondaryTextColor = Color.white;

    [Header("Sono Obrigatório")]
    [SerializeField] private bool forceSleepAfterDeadline = true;
    [SerializeField, Range(0f, 24f)] private float bedtimeWarningHour = 0f;
    [SerializeField, Range(0f, 24f)] private float forcedSleepHour = 1f;
    [SerializeField, Range(0f, 24f)] private float wakeUpHour = 6f;
    [SerializeField] private string bedtimeWarningMessage = "Está ficando tarde. Volte para dormir.";
    [SerializeField] private string forcedSleepMessage = "Você apagou de sono e acordou às 6 AM.";

    private bool hasShownBedtimeWarning;
    private bool hasForcedSleepThisNight;
    private bool hasAdvancedDaySinceLastWake;
    private bool isForcedSleepPending;
    private bool isDayVisualMode;
    private bool hasInitializedVisualMode;

    public int CurrentDay => currentDay;
    public int TotalDays => totalDays;
    public int ElapsedDays => elapsedDays;
    public float NormalizedTime => currentTime;
    public bool IsHourTextVisible => HourText != null && HourText.gameObject.activeSelf;
    public bool IsDayTextVisible => DayText != null && DayText.gameObject.activeSelf;
    public bool IsDayVisualMode => isDayVisualMode;
    public Color PrimaryHudTextColor => isDayVisualMode ? dayPrimaryTextColor : nightPrimaryTextColor;
    public Color SecondaryHudTextColor => isDayVisualMode ? daySecondaryTextColor : nightSecondaryTextColor;
    public string PrimaryHudTextColorHex => $"#{ColorUtility.ToHtmlStringRGB(PrimaryHudTextColor)}";
    public string SecondaryHudTextColorHex => $"#{ColorUtility.ToHtmlStringRGB(SecondaryHudTextColor)}";

    private static Gradient CreateDefaultDayProgressGradient()
    {
        Gradient gradient = new();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.04f, 0.08f, 0.2f), 0f),
                new GradientColorKey(new Color(0.04f, 0.08f, 0.2f), 0.2f),
                new GradientColorKey(new Color(1f, 0.86f, 0.18f), 0.3f),
                new GradientColorKey(new Color(1f, 0.86f, 0.18f), 0.62f),
                new GradientColorKey(new Color(1f, 0.46f, 0.12f), 0.76f),
                new GradientColorKey(new Color(0.04f, 0.08f, 0.2f), 0.88f),
                new GradientColorKey(new Color(0.04f, 0.08f, 0.2f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            }
        );

        return gradient;
    }

    void Start()
    {
        ConfigureDayProgressFill();
        UpdateDayUI();
        UpdateTime();
        UpdateDayProgress();
        UpdateHudVisualMode(true);
    }

    void Update()
    {
        float previousTime = currentTime;
        currentTime += Time.deltaTime / dayDuration;
        bool wrappedDay = false;

        while (currentTime >= 1f)
        {
            currentTime -= 1f;
            AdvanceDay(false);
            hasAdvancedDaySinceLastWake = true;
            wrappedDay = true;
        }

        UpdateSleepDeadline(previousTime, currentTime, wrappedDay);
        UpdateSun();
        UpdateSkybox();
        UpdateTime();
        UpdateDayProgress();
        UpdateHudVisualMode(false);
    }

    void UpdateDayUI()
    {
        if (DayText != null)
            DayText.text = $"Dia {currentDay}/{totalDays}";
    }

    public void SetDayCycleHudVisible(bool _visible)
    {
        SetDayCycleHudVisible(_visible, _visible);
    }

    public void SetDayCycleHudVisible(bool _hourVisible, bool _dayVisible)
    {
        if (HourText != null)
            HourText.gameObject.SetActive(_hourVisible);

        if (DayText != null)
            DayText.gameObject.SetActive(_dayVisible);

        if (dayProgressBackplate != null)
            dayProgressBackplate.gameObject.SetActive(_hourVisible || _dayVisible);

        if (dayProgressFill != null)
            dayProgressFill.gameObject.SetActive(_hourVisible || _dayVisible);

        if (dayProgressTrack != null)
            dayProgressTrack.gameObject.SetActive(_hourVisible || _dayVisible);

        if (dayProgressHandle != null)
            dayProgressHandle.gameObject.SetActive(_hourVisible || _dayVisible);
    }

    private void ConfigureDayProgressFill()
    {
        if (dayProgressFill == null || !configureDayProgressFill)
            return;

        if (dayProgressBackplate != null && dayProgressFill == dayProgressBackplate)
        {
            Debug.LogWarning(
                "[DayCycle] Day Progress Fill esta usando a mesma Image do Backplate. " +
                "Use uma Image filha separada para o fill, senao a barra de fundo inteira sera colorida.",
                this
            );
        }

        if (resizeDayProgressFillRect)
        {
            RectTransform fillTransform = dayProgressFill.rectTransform;
            fillTransform.anchorMin = new Vector2(0f, 0f);
            fillTransform.anchorMax = new Vector2(0f, 1f);
            fillTransform.pivot = new Vector2(0f, 0.5f);
            fillTransform.anchoredPosition = Vector2.zero;
            fillTransform.sizeDelta = new Vector2(0f, 0f);
            dayProgressFill.type = Image.Type.Simple;
            return;
        }

        dayProgressFill.type = Image.Type.Filled;
        dayProgressFill.fillMethod = Image.FillMethod.Horizontal;
        dayProgressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
    }

    private void UpdateDayProgress()
    {
        float normalizedTime = Mathf.Clamp01(currentTime);

        if (dayProgressFill != null)
        {
            UpdateDayProgressFillSize(normalizedTime);
            UpdateDayProgressFillColor(normalizedTime);
        }

        UpdateDayProgressHandle(normalizedTime);
    }

    private void UpdateDayProgressFillSize(float _normalizedTime)
    {
        if (!resizeDayProgressFillRect)
        {
            dayProgressFill.fillAmount = _normalizedTime;
            return;
        }

        RectTransform track = GetDayProgressTrack();

        if (track == null)
            return;

        RectTransform fillTransform = dayProgressFill.rectTransform;
        float fillWidth = track.rect.width * Mathf.Clamp01(_normalizedTime);
        fillTransform.anchorMin = new Vector2(0f, fillTransform.anchorMin.y);
        fillTransform.anchorMax = new Vector2(0f, fillTransform.anchorMax.y);
        fillTransform.pivot = new Vector2(0f, fillTransform.pivot.y);
        fillTransform.anchoredPosition = new Vector2(0f, fillTransform.anchoredPosition.y);
        fillTransform.sizeDelta = new Vector2(fillWidth, fillTransform.sizeDelta.y);
    }

    private void UpdateDayProgressFillColor(float _normalizedTime)
    {
        if (!tintDayProgressFillOverTime || dayProgressFillColorByTime == null)
            return;

        dayProgressFill.color = dayProgressFillColorByTime.Evaluate(Mathf.Clamp01(_normalizedTime));
    }

    private void UpdateDayProgressHandle(float _normalizedTime)
    {
        if (dayProgressHandle == null)
            return;

        RectTransform track = GetDayProgressTrack();

        if (track == null)
            return;

        Rect trackRect = track.rect;
        float minX = trackRect.xMin;
        float maxX = trackRect.xMax;

        if (keepDayProgressHandleInsideTrack)
        {
            float halfHandleWidth = dayProgressHandle.rect.width * 0.5f;
            minX += halfHandleWidth;
            maxX -= halfHandleWidth;

            if (minX > maxX)
            {
                minX = trackRect.xMin;
                maxX = trackRect.xMax;
            }
        }

        float localX = Mathf.Lerp(minX, maxX, Mathf.Clamp01(_normalizedTime));
        Vector3 trackWorldPosition = track.TransformPoint(new Vector3(localX, trackRect.center.y, 0f));
        RectTransform handleParent = dayProgressHandle.parent as RectTransform;

        if (handleParent == null)
        {
            dayProgressHandle.position = trackWorldPosition;
            return;
        }

        Vector3 parentLocalPosition = handleParent.InverseTransformPoint(trackWorldPosition);
        Vector3 handleLocalPosition = dayProgressHandle.localPosition;
        handleLocalPosition.x = parentLocalPosition.x;
        handleLocalPosition.y = parentLocalPosition.y;
        dayProgressHandle.localPosition = handleLocalPosition;
    }

    private RectTransform GetDayProgressTrack()
    {
        if (dayProgressTrack != null)
            return dayProgressTrack;

        if (dayProgressBackplate != null)
            return dayProgressBackplate.rectTransform;

        if (dayProgressFill == null)
            return null;

        RectTransform fillParent = dayProgressFill.rectTransform.parent as RectTransform;
        return fillParent != null ? fillParent : dayProgressFill.rectTransform;
    }

    private void UpdateHudVisualMode(bool _forceNotify)
    {
        bool nextDayVisualMode = IsCurrentHourInDayVisualRange();

        if (!_forceNotify && hasInitializedVisualMode && nextDayVisualMode == isDayVisualMode)
            return;

        isDayVisualMode = nextDayVisualMode;
        hasInitializedVisualMode = true;
        ApplyDayCycleTextColors();
        VisualModeChanged?.Invoke(isDayVisualMode);
    }

    private void ApplyDayCycleTextColors()
    {
        if (DayText != null)
            DayText.color = PrimaryHudTextColor;

        if (HourText != null)
            HourText.color = SecondaryHudTextColor;
    }

    private bool IsCurrentHourInDayVisualRange()
    {
        float currentHour = Mathf.Repeat(currentTime * 24f, 24f);
        float startHour = Mathf.Repeat(dayVisualStartHour, 24f);
        float endHour = Mathf.Repeat(dayVisualEndHour, 24f);

        if (Mathf.Approximately(startHour, endHour))
            return true;

        if (startHour < endHour)
            return currentHour >= startHour && currentHour < endHour;

        return currentHour >= startHour || currentHour < endHour;
    }

    void UpdateSun()
    {
        if (sun == null) return;

        float sunAngle = currentTime * 360f - 90f;

        sun.transform.rotation = Quaternion.Euler(sunAngle, 170f, 0);
        sun.intensity = Mathf.Max(0.2f, sunIntensity.Evaluate(currentTime));

        if (moon != null)
        {
            moon.transform.rotation = Quaternion.Euler(sunAngle + 180f, 170f, 0);
            moon.intensity = Mathf.Clamp01(1f - sun.intensity) * 0.3f;
        }
    }

    void UpdateSkybox()
    {
        if (skyboxMaterial == null) return;

        Color sky = skyColor.Evaluate(currentTime);
        skyboxMaterial.SetColor("_SkyTint", sky);

        Color ground = groundColor.Evaluate(currentTime);
        skyboxMaterial.SetColor("_GroundColor", ground);

        float dynamicExposure = Mathf.Lerp(exposure, 2.0f, 1f - sunIntensity.Evaluate(currentTime));
        skyboxMaterial.SetFloat("_Exposure", dynamicExposure);

        skyboxMaterial.SetFloat("_AtmosphereThickness", atmosphereThickness);

        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = sky * 0.3f;

        DynamicGI.UpdateEnvironment();
    }

    void UpdateTime()
    {
        Clock = currentTime * 24f;

        int hours24 = Mathf.FloorToInt(Clock);
        int minutes = Mathf.FloorToInt((Clock % 1f) * 60f);

        string period = hours24 >= 12 ? "PM" : "AM";

        int hours12 = hours24 % 12;
        if (hours12 == 0) hours12 = 12;

        if (HourText != null)
            HourText.text = $"{hours12:00}:00 {period}";
        //HourText.text = $"{hours12:00}:{minutes:00} {period}";
    }

    public void NextDay()
    {
        AdvanceDay(true);
        ResetSleepDeadlineState();
    }

    public void SetCycleState(int _currentDay, int _elapsedDays, float _normalizedTime)
    {
        currentDay = Mathf.Clamp(_currentDay, 1, Mathf.Max(1, totalDays));
        elapsedDays = Mathf.Max(1, _elapsedDays);
        currentTime = Mathf.Repeat(_normalizedTime, 1f);
        Clock = currentTime * 24f;
        ResetSleepDeadlineState();
        UpdateSun();
        UpdateSkybox();
        UpdateTime();
        UpdateDayProgress();
        UpdateHudVisualMode(true);
        UpdateDayUI();
    }

    private void AdvanceDay(bool _resetToMorning)
    {
        currentDay++;
        elapsedDays++;

        if (currentDay > totalDays)
            currentDay = 1;

        if (_resetToMorning)
        {
            SetTimeToHour(wakeUpHour);
        }

        UpdateSun();
        UpdateSkybox();
        UpdateTime();
        UpdateDayProgress();
        UpdateHudVisualMode(true);
        UpdateDayUI();
        DayChanged?.Invoke(elapsedDays);
    }

    private void UpdateSleepDeadline(float _previousTime, float _currentTime, bool _wrappedDay)
    {
        if (!forceSleepAfterDeadline)
            return;

        if (!hasShownBedtimeWarning && HasCrossedHour(_previousTime, _currentTime, _wrappedDay, bedtimeWarningHour))
        {
            hasShownBedtimeWarning = true;

            if (HUDWarningUI.Instance != null)
                HUDWarningUI.Instance.ShowWarning(bedtimeWarningMessage);
        }

        if (!hasForcedSleepThisNight && HasCrossedHour(_previousTime, _currentTime, _wrappedDay, forcedSleepHour))
            ForceSleep();
    }

    private bool HasCrossedHour(float _previousTime, float _currentTime, bool _wrappedDay, float _targetHour)
    {
        float targetNormalized = Mathf.Repeat(_targetHour, 24f) / 24f;

        if (!_wrappedDay)
            return _previousTime < targetNormalized && _currentTime >= targetNormalized;

        return _previousTime < targetNormalized || _currentTime >= targetNormalized;
    }

    private void ForceSleep()
    {
        hasForcedSleepThisNight = true;
        isForcedSleepPending = true;

        if (ForcedSleepRequested != null)
        {
            ForcedSleepRequested.Invoke();
            return;
        }

        CompleteForcedSleepWakeUp();
    }

    public void CompleteForcedSleepWakeUp()
    {
        if (!isForcedSleepPending)
            return;

        if (!hasAdvancedDaySinceLastWake)
            AdvanceDay(false);

        SetTimeToHour(wakeUpHour);
        UpdateSun();
        UpdateSkybox();
        UpdateTime();
        UpdateDayProgress();
        UpdateHudVisualMode(true);
        UpdateDayUI();

        ResetSleepDeadlineState();
        isForcedSleepPending = false;

        if (HUDWarningUI.Instance != null)
            HUDWarningUI.Instance.ShowWarning(forcedSleepMessage);
    }

    private void SetTimeToHour(float _hour)
    {
        float normalizedHour = Mathf.Repeat(_hour, 24f) / 24f;
        currentTime = normalizedHour;
        Clock = Mathf.Repeat(_hour, 24f);
    }

    private void ResetSleepDeadlineState()
    {
        hasShownBedtimeWarning = false;
        hasForcedSleepThisNight = false;
        hasAdvancedDaySinceLastWake = false;
    }
}
