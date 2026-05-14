using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FishSkillCheckUI : MonoBehaviour
{
    public enum TimingPositionMode
    {
        FullArea,
        AlternateHorizontalSides
    }

    [Header("References")]
    [SerializeField] private FishSkillCheck fishSkillCheck;
    [SerializeField] private FishingManager fishingManager;

    [Header("Progress Bar")]
    [SerializeField] private RectTransform progressBarArea;
    [SerializeField] private RectTransform progressBarFill;

    [Header("Timing Bar")]
    [SerializeField] private RectTransform timingBarArea;
    [SerializeField] private RectTransform successZone;
    [SerializeField] private RectTransform indicator;

    [Header("First Skill Check Hint")]
    [SerializeField] private InputIconDatabase _iconDatabase;
    [SerializeField] private Image _skillCheckInteractIcon;
    [SerializeField] private Vector2 _skillCheckIconSize = new Vector2(48f, 48f);
    [SerializeField] private float _skillCheckIconRadiusOffset = 42f;
    [SerializeField] private Vector2 _skillCheckIconOffset;
    [SerializeField] private float _skillCheckIconPulseSpeed = 6f;
    [SerializeField] private float _skillCheckIconPulseScale = 0.12f;

    [Header("Runtime Fallback")]
    [SerializeField] private bool _allowRuntimeFallback;
    [SerializeField] private bool _logMissingReferences = true;

    [Header("Timing Display")]
    [SerializeField] private bool randomizeTimingPosition;
    [SerializeField] private TimingPositionMode timingPositionMode = TimingPositionMode.FullArea;
    [SerializeField] private Vector2 screenPadding = new Vector2(150f, 100f);
    [SerializeField] private float minRandomPositionDistance = 120f;
    [SerializeField] private int skillChecksPerPosition = 1;
    [SerializeField, Range(0f, 0.45f)] private float horizontalCenterDeadZone = 0.18f;

    [Header("Circular Display")]
    [SerializeField] private bool scaleCircularZoneBySize = true;
    [SerializeField] private float minCircularZoneScale = 0.65f;
    [SerializeField] private float maxCircularZoneScale = 1.35f;
    [SerializeField] private RectTransform _successZoneMarker;
    [SerializeField] private RectTransform _perfectZoneMarker;
    [SerializeField] private bool _sizePerfectZoneByThreshold = true;
    [SerializeField] private float _perfectZoneMinWidth = 10f;
    [SerializeField] private float _perfectZoneMaxWidth = 20f;
    [SerializeField] private float _perfectZoneHeight = 42f;
    [SerializeField, Range(0.05f, 0.95f)] private float _perfectZoneMaxSuccessPercent = 0.35f;

    [Header("Feedback Text")]
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private float feedbackDuration = 1.2f;
    [SerializeField] private float feedbackRiseDistance = 60f;
    [SerializeField, Range(0f, 1f)] private float feedbackFadeStartNormalized = 0.45f;
    [SerializeField] private bool feedbackFollowIndicator = true;
    [SerializeField] private Vector2 feedbackOffset = new Vector2(0f, 45f);
    [SerializeField] private bool placeCircularFeedbackOnRing = true;
    [SerializeField] private float circularFeedbackRadiusOffset = 24f;

    [Header("Shake")]
    [SerializeField] private float shakeDuration = 0.18f;
    [SerializeField] private float shakeStrength = 10f;

    private Vector2 timingBarOriginalPosition;
    private Vector2 timingBarCurrentBasePosition;
    private Vector2 lastRandomPosition;
    private Vector2 feedbackOriginalPosition;
    private Coroutine feedbackRoutine;
    private Coroutine shakeRoutine;
    private int skillChecksAtCurrentPosition;
    private int lastHorizontalSide = 1;
    private bool hasRandomTimingPosition;
    private bool wasSkillCheckActive;
    private bool hasLearnedSkillCheckInput;
    private RectTransform skillCheckInteractIconRect;
    private bool hasLoggedMissingSkillCheckIcon;

    private void OnValidate()
    {
        feedbackDuration = Mathf.Max(0.8f, feedbackDuration);
        feedbackRiseDistance = Mathf.Max(0f, feedbackRiseDistance);
        circularFeedbackRadiusOffset = Mathf.Max(0f, circularFeedbackRadiusOffset);
        _skillCheckIconSize.x = Mathf.Max(1f, _skillCheckIconSize.x);
        _skillCheckIconSize.y = Mathf.Max(1f, _skillCheckIconSize.y);
        _skillCheckIconRadiusOffset = Mathf.Max(0f, _skillCheckIconRadiusOffset);
        _skillCheckIconPulseSpeed = Mathf.Max(0f, _skillCheckIconPulseSpeed);
        _skillCheckIconPulseScale = Mathf.Max(0f, _skillCheckIconPulseScale);
        shakeDuration = Mathf.Max(0f, shakeDuration);
        shakeStrength = Mathf.Max(0f, shakeStrength);
        minRandomPositionDistance = Mathf.Max(0f, minRandomPositionDistance);
        skillChecksPerPosition = Mathf.Max(1, skillChecksPerPosition);
        _perfectZoneMinWidth = Mathf.Max(0f, _perfectZoneMinWidth);
        _perfectZoneMaxWidth = Mathf.Max(_perfectZoneMinWidth, _perfectZoneMaxWidth);
        _perfectZoneHeight = Mathf.Max(0f, _perfectZoneHeight);
    }

    private void Awake()
    {
        AutoAssignMissingReferences();
    }

    private void OnEnable()
    {
        InputDeviceDetector.DeviceTypeChanged += HandleDeviceTypeChanged;
        UpdateSkillCheckHintIconSprite();
    }

    private void OnDisable()
    {
        InputDeviceDetector.DeviceTypeChanged -= HandleDeviceTypeChanged;
        SetSkillCheckHintVisible(false);
    }

    private void Start()
    {
        if (timingBarArea != null)
        {
            timingBarOriginalPosition = timingBarArea.anchoredPosition;
            timingBarCurrentBasePosition = timingBarOriginalPosition;
            SetTimingBarVisible(false);
        }

        SetProgressBarVisible(false);

        if (feedbackText != null)
        {
            feedbackOriginalPosition = feedbackText.rectTransform.anchoredPosition;
            feedbackText.gameObject.SetActive(false);
        }

        if (fishSkillCheck != null)
        {
            fishSkillCheck.OnFeedbackTriggered += HandleFeedback;
            fishSkillCheck.OnFailShake += HandleFailShake;
        }

        EnsureSkillCheckHintIcon();
        UpdateSkillCheckHintIconSprite();
        SetSkillCheckHintVisible(false);
    }

    private void AutoAssignMissingReferences()
    {
        if (fishSkillCheck == null)
            fishSkillCheck = GetComponentInParent<FishSkillCheck>();

        if (fishSkillCheck == null)
            fishSkillCheck = FindFirstObjectByType<FishSkillCheck>(FindObjectsInactive.Include);

        if (fishingManager == null)
            fishingManager = FindFirstObjectByType<FishingManager>(FindObjectsInactive.Include);

        if (_iconDatabase == null)
            _iconDatabase = FindFirstObjectByType<InputIconDatabase>(FindObjectsInactive.Include);
    }

    private void OnDestroy()
    {
        if (fishSkillCheck != null)
        {
            fishSkillCheck.OnFeedbackTriggered -= HandleFeedback;
            fishSkillCheck.OnFailShake -= HandleFailShake;
        }
    }

    private void Update()
    {
        UpdateProgressBarVisibility();

        if (fishingManager != null && fishingManager.IsFishing && fishingManager.HasFishBitten)
            UpdateProgressBar();

        UpdateSkillCheckDisplay();
    }

    private void UpdateProgressBarVisibility()
    {
        bool shouldShowProgressBar = fishingManager != null && fishingManager.IsFishing && fishingManager.HasFishBitten;
        SetProgressBarVisible(shouldShowProgressBar);
    }

    private void UpdateSkillCheckDisplay()
    {
        bool isSkillCheckActive = fishSkillCheck != null && fishSkillCheck.IsSkillCheckActive;

        if (isSkillCheckActive != wasSkillCheckActive)
        {
            if (isSkillCheckActive)
            {
                SetTimingBarVisible(true);
                SetTimingContentVisible(true);
                BeginSkillCheckDisplay();
            }
            else
            {
                EndSkillCheckDisplay();
            }

            wasSkillCheckActive = isSkillCheckActive;
        }

        if (isSkillCheckActive)
            UpdateTimingBar();
    }

    private void UpdateProgressBar()
    {
        if (progressBarArea == null || progressBarFill == null)
            return;

        float width = progressBarArea.rect.width;
        float fillWidth = fishingManager.ProgressNormalized * width;

        progressBarFill.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, fillWidth);

        float fillPosX = (fillWidth * 0.5f) - (width * 0.5f);
        progressBarFill.anchoredPosition = new Vector2(fillPosX, progressBarFill.anchoredPosition.y);
    }

    private void SetProgressBarVisible(bool _visible)
    {
        if (progressBarArea != null)
            progressBarArea.gameObject.SetActive(_visible);
    }

    private void UpdateTimingBar()
    {
        if (timingBarArea == null || successZone == null || indicator == null)
            return;

        UpdateCircularSkillCheck();
    }

    private void UpdateCircularSkillCheck()
    {
        float zoneStart = fishSkillCheck.SuccessZoneStartNormalized;
        float zoneEnd = fishSkillCheck.SuccessZoneEndNormalized;
        float zoneSize = Mathf.Clamp01(zoneEnd - zoneStart);
        float zoneCenter = zoneStart + (zoneSize * 0.5f);

        successZone.localRotation = Quaternion.Euler(0f, 0f, -zoneCenter * 360f);
        indicator.localRotation = Quaternion.Euler(0f, 0f, -fishSkillCheck.IndicatorNormalized * 360f);

        if (scaleCircularZoneBySize)
        {
            float zoneScale = Mathf.Lerp(minCircularZoneScale, maxCircularZoneScale, zoneSize);
            successZone.localScale = new Vector3(zoneScale, successZone.localScale.y, successZone.localScale.z);
        }

        UpdatePerfectZoneMarker();
        UpdateSkillCheckHint(zoneCenter);
    }

    private void UpdatePerfectZoneMarker()
    {
        if (_perfectZoneMarker == null || fishSkillCheck == null)
            return;

        float targetWidth = GetPerfectZoneWidth();
        float targetHeight = GetPerfectZoneHeight();

        _perfectZoneMarker.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetWidth);
        _perfectZoneMarker.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
        _perfectZoneMarker.anchoredPosition = Vector2.zero;
    }

    private float GetPerfectZoneWidth()
    {
        RectTransform markerReference = GetSuccessZoneMarkerReference();

        if (_sizePerfectZoneByThreshold && markerReference != null)
        {
            float successWidth = markerReference.rect.width;
            float logicalPerfectPercent = fishSkillCheck.CurrentPerfectThreshold * 2f;
            float widthByThreshold = successWidth * logicalPerfectPercent;
            float maxWidthBySuccessZone = successWidth * _perfectZoneMaxSuccessPercent;
            float maxAllowedWidth = Mathf.Min(_perfectZoneMaxWidth, maxWidthBySuccessZone);
            float minAllowedWidth = Mathf.Min(_perfectZoneMinWidth, maxAllowedWidth);

            return Mathf.Clamp(widthByThreshold, minAllowedWidth, maxAllowedWidth);
        }

        float minThreshold = fishSkillCheck.MinPerfectThreshold;
        float maxThreshold = fishSkillCheck.MaxPerfectThreshold;
        float thresholdRange = maxThreshold - minThreshold;
        float normalizedThreshold = thresholdRange > 0f
            ? Mathf.Clamp01((fishSkillCheck.CurrentPerfectThreshold - minThreshold) / thresholdRange)
            : 1f;

        return Mathf.Lerp(_perfectZoneMinWidth, _perfectZoneMaxWidth, normalizedThreshold);
    }

    private float GetPerfectZoneHeight()
    {
        RectTransform markerReference = GetSuccessZoneMarkerReference();

        if (markerReference == null)
            return _perfectZoneHeight;

        return Mathf.Min(_perfectZoneHeight, markerReference.rect.height);
    }

    private RectTransform GetSuccessZoneMarkerReference()
    {
        if (_successZoneMarker != null)
            return _successZoneMarker;

        return _perfectZoneMarker.parent as RectTransform;
    }

    private void BeginSkillCheckDisplay()
    {
        if (randomizeTimingPosition)
        {
            if (ShouldRandomizeTimingPosition())
                RandomizeTimingBarPosition();
        }
        else
        {
            timingBarCurrentBasePosition = timingBarOriginalPosition;
            skillChecksAtCurrentPosition = 0;
            hasRandomTimingPosition = false;
        }

        if (timingBarArea != null)
            timingBarArea.anchoredPosition = timingBarCurrentBasePosition;

        skillChecksAtCurrentPosition++;
    }

    private void EndSkillCheckDisplay()
    {
        bool shouldKeepVisibleForFeedback = IsFeedbackVisibleInsideTimingArea();

        if (shakeRoutine != null)
        {
            StopCoroutine(shakeRoutine);
            shakeRoutine = null;
        }

        if (timingBarArea != null)
            timingBarArea.anchoredPosition = timingBarCurrentBasePosition;

        if (shouldKeepVisibleForFeedback)
        {
            SetTimingContentVisible(false);
            return;
        }

        HideTimingBar();
    }

    private void SetTimingBarVisible(bool _visible)
    {
        if (timingBarArea != null)
            timingBarArea.gameObject.SetActive(_visible);
    }

    private void SetTimingContentVisible(bool _visible)
    {
        if (timingBarArea == null)
            return;

        Transform feedbackTransform = feedbackText != null ? feedbackText.transform : null;

        for (int i = 0; i < timingBarArea.childCount; i++)
        {
            Transform child = timingBarArea.GetChild(i);

            if (feedbackTransform != null && (child == feedbackTransform || feedbackTransform.IsChildOf(child)))
                continue;

            child.gameObject.SetActive(_visible);
        }
    }

    private void RandomizeTimingBarPosition()
    {
        if (timingBarArea == null)
            return;

        RectTransform parentRect = timingBarArea.parent as RectTransform;

        if (parentRect == null)
            return;

        Rect parentBounds = parentRect.rect;

        float halfWidth = timingBarArea.rect.width * 0.5f;
        float halfHeight = timingBarArea.rect.height * 0.5f;

        float minX = parentBounds.xMin + screenPadding.x + halfWidth;
        float maxX = parentBounds.xMax - screenPadding.x - halfWidth;
        float minY = parentBounds.yMin + screenPadding.y + halfHeight;
        float maxY = parentBounds.yMax - screenPadding.y - halfHeight;

        if (minX > maxX)
            minX = maxX = 0f;

        if (minY > maxY)
            minY = maxY = 0f;

        Vector2 randomPosition = timingPositionMode == TimingPositionMode.AlternateHorizontalSides
            ? GetRandomHorizontalSidePosition(minX, maxX, minY, maxY)
            : timingBarOriginalPosition;

        if (timingPositionMode == TimingPositionMode.FullArea)
        {
            for (int i = 0; i < 8; i++)
            {
                randomPosition = new Vector2(
                    Random.Range(minX, maxX),
                    Random.Range(minY, maxY)
                );

                if (Vector2.Distance(randomPosition, lastRandomPosition) >= minRandomPositionDistance)
                    break;
            }
        }

        timingBarCurrentBasePosition = randomPosition;
        lastRandomPosition = randomPosition;
        hasRandomTimingPosition = true;
        skillChecksAtCurrentPosition = 0;
    }

    private Vector2 GetRandomHorizontalSidePosition(float _minX, float _maxX, float _minY, float _maxY)
    {
        if (_minX >= _maxX)
            return new Vector2(0f, Random.Range(_minY, _maxY));

        int side = -lastHorizontalSide;
        lastHorizontalSide = side;

        float width = _maxX - _minX;
        float deadZoneWidth = width * horizontalCenterDeadZone;
        float leftMaxX = Mathf.Min(-deadZoneWidth, _maxX);
        float rightMinX = Mathf.Max(deadZoneWidth, _minX);

        float x;

        if (side < 0)
        {
            float maxLeft = Mathf.Max(_minX, leftMaxX);
            x = Random.Range(_minX, maxLeft);
        }
        else
        {
            float minRight = Mathf.Min(rightMinX, _maxX);
            x = Random.Range(minRight, _maxX);
        }

        return new Vector2(x, Random.Range(_minY, _maxY));
    }

    private bool ShouldRandomizeTimingPosition()
    {
        return !hasRandomTimingPosition || skillChecksAtCurrentPosition >= skillChecksPerPosition;
    }

    private void HandleFeedback(FishSkillCheck.FeedbackResult resultado)
    {
        if (resultado == FishSkillCheck.FeedbackResult.Good ||
            resultado == FishSkillCheck.FeedbackResult.Great ||
            resultado == FishSkillCheck.FeedbackResult.Perfect)
        {
            hasLearnedSkillCheckInput = true;
            SetSkillCheckHintVisible(false);
        }

        switch (resultado)
        {
            case FishSkillCheck.FeedbackResult.Terrible:
                ShowFeedback("Horrível", new Color(0.7f, 0.1f, 0.1f));
                break;

            case FishSkillCheck.FeedbackResult.Bad:
                ShowFeedback("Ruim", Color.red);
                break;

            case FishSkillCheck.FeedbackResult.Near:
                ShowFeedback("Quase", new Color(1f, 0.5f, 0f));
                break;

            case FishSkillCheck.FeedbackResult.Good:
                ShowFeedback("Bom", Color.white);
                break;

            case FishSkillCheck.FeedbackResult.Great:
                ShowFeedback("Ótimo", Color.yellow);
                break;

            case FishSkillCheck.FeedbackResult.Perfect:
                ShowFeedback("Perfeito", Color.green);
                break;
        }
    }

    private void HandleFailShake()
    {
        if (timingBarArea == null)
            return;

        if (shakeRoutine != null)
            StopCoroutine(shakeRoutine);

        shakeRoutine = StartCoroutine(ShakeTimingBarRoutine());
    }

    private void ShowFeedback(string text, Color color)
    {
        if (feedbackText == null)
            return;

        if (feedbackRoutine != null)
            StopCoroutine(feedbackRoutine);

        feedbackRoutine = StartCoroutine(ShowFeedbackRoutine(text, color));
    }

    private IEnumerator ShowFeedbackRoutine(string text, Color color)
    {
        RectTransform feedbackRect = feedbackText.rectTransform;
        Color feedbackColor = color;
        float elapsed = 0f;

        feedbackText.gameObject.SetActive(true);
        feedbackText.text = text;
        feedbackOriginalPosition = GetFeedbackPosition();
        feedbackRect.anchoredPosition = feedbackOriginalPosition;

        while (elapsed < feedbackDuration)
        {
            elapsed += Time.deltaTime;

            float normalized = Mathf.Clamp01(elapsed / feedbackDuration);
            float riseT = 1f - Mathf.Pow(1f - normalized, 2f);
            float fadeT = Mathf.InverseLerp(feedbackFadeStartNormalized, 1f, normalized);

            feedbackColor.a = Mathf.Lerp(1f, 0f, fadeT);
            feedbackText.color = feedbackColor;
            feedbackRect.anchoredPosition = feedbackOriginalPosition + (Vector2.up * feedbackRiseDistance * riseT);

            yield return null;
        }

        feedbackText.gameObject.SetActive(false);
        feedbackRect.anchoredPosition = feedbackOriginalPosition;
        feedbackColor.a = 1f;
        feedbackText.color = feedbackColor;
        feedbackRoutine = null;

        if (fishSkillCheck == null || !fishSkillCheck.IsSkillCheckActive)
            HideTimingBar();
    }

    private void HideTimingBar()
    {
        timingBarCurrentBasePosition = timingBarOriginalPosition;
        SetTimingContentVisible(true);

        if (timingBarArea != null)
            timingBarArea.anchoredPosition = timingBarOriginalPosition;

        SetTimingBarVisible(false);
    }

    private bool IsFeedbackVisibleInsideTimingArea()
    {
        if (feedbackRoutine == null || feedbackText == null || timingBarArea == null)
            return false;

        return feedbackText.transform.IsChildOf(timingBarArea);
    }

    private IEnumerator ShakeTimingBarRoutine()
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;

            float offsetX = Random.Range(-shakeStrength, shakeStrength);
            float offsetY = Random.Range(-shakeStrength * 0.2f, shakeStrength * 0.2f);

            timingBarArea.anchoredPosition = timingBarCurrentBasePosition + new Vector2(offsetX, offsetY);

            yield return null;
        }

        timingBarArea.anchoredPosition = timingBarCurrentBasePosition;
        shakeRoutine = null;
    }

    private Vector2 GetFeedbackPosition()
    {
        if (!feedbackFollowIndicator || timingBarArea == null || indicator == null)
            return feedbackOriginalPosition;

        if (placeCircularFeedbackOnRing)
            return GetCircularFeedbackPosition();

        return new Vector2(
            indicator.anchoredPosition.x + feedbackOffset.x,
            indicator.anchoredPosition.y + feedbackOffset.y
        );
    }

    private Vector2 GetCircularFeedbackPosition()
    {
        if (fishSkillCheck == null)
            return feedbackOriginalPosition;

        Rect timingRect = timingBarArea.rect;
        float radius = (Mathf.Min(timingRect.width, timingRect.height) * 0.5f) + circularFeedbackRadiusOffset;
        float angleRadians = fishSkillCheck.IndicatorNormalized * Mathf.PI * 2f;
        Vector2 direction = new Vector2(Mathf.Sin(angleRadians), Mathf.Cos(angleRadians));

        return (direction * radius) + feedbackOffset;
    }

    private void HandleDeviceTypeChanged(InputDeviceType _deviceType)
    {
        UpdateSkillCheckHintIconSprite();
    }

    private void UpdateSkillCheckHint(float _zoneCenter)
    {
        if (hasLearnedSkillCheckInput ||
            fishSkillCheck == null ||
            !fishSkillCheck.IsSkillCheckActive ||
            timingBarArea == null)
        {
            SetSkillCheckHintVisible(false);
            return;
        }

        if (!EnsureSkillCheckHintIcon())
            return;

        UpdateSkillCheckHintIconSprite();

        if (_skillCheckInteractIcon.sprite == null)
        {
            SetSkillCheckHintVisible(false);
            return;
        }

        Rect timingRect = timingBarArea.rect;
        float radius = (Mathf.Min(timingRect.width, timingRect.height) * 0.5f) + _skillCheckIconRadiusOffset;
        float angleRadians = _zoneCenter * Mathf.PI * 2f;
        Vector2 direction = new Vector2(Mathf.Sin(angleRadians), Mathf.Cos(angleRadians));
        float pulse = (Mathf.Sin(Time.unscaledTime * _skillCheckIconPulseSpeed) * 0.5f) + 0.5f;

        skillCheckInteractIconRect.anchoredPosition = (direction * radius) + _skillCheckIconOffset;
        skillCheckInteractIconRect.localScale = Vector3.one * (1f + (pulse * _skillCheckIconPulseScale));

        SetSkillCheckHintVisible(true);
    }

    private bool EnsureSkillCheckHintIcon()
    {
        if (_skillCheckInteractIcon != null)
        {
            skillCheckInteractIconRect = _skillCheckInteractIcon.rectTransform;
            return true;
        }

        if (timingBarArea == null)
            return false;

        if (!_allowRuntimeFallback)
        {
            LogMissingReference("SkillCheckInteractHintIcon", ref hasLoggedMissingSkillCheckIcon);
            return false;
        }

        GameObject iconObject = new GameObject("SkillCheckInteractHintIcon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        iconObject.transform.SetParent(timingBarArea, false);

        skillCheckInteractIconRect = iconObject.GetComponent<RectTransform>();
        skillCheckInteractIconRect.anchorMin = new Vector2(0.5f, 0.5f);
        skillCheckInteractIconRect.anchorMax = new Vector2(0.5f, 0.5f);
        skillCheckInteractIconRect.pivot = new Vector2(0.5f, 0.5f);
        skillCheckInteractIconRect.sizeDelta = _skillCheckIconSize;

        _skillCheckInteractIcon = iconObject.GetComponent<Image>();
        _skillCheckInteractIcon.raycastTarget = false;
        _skillCheckInteractIcon.preserveAspect = true;

        return true;
    }

    private void LogMissingReference(string _referenceName, ref bool _hasLogged)
    {
        if (!_logMissingReferences || _hasLogged)
            return;

        Debug.LogWarning($"[FishSkillCheckUI] Falta {_referenceName}. Crie esse Image como filho do timingBarArea ou arraste no Inspector. Ative Allow Runtime Fallback apenas se quiser cria-lo em runtime.", this);
        _hasLogged = true;
    }

    private void UpdateSkillCheckHintIconSprite()
    {
        if (_skillCheckInteractIcon == null || _iconDatabase == null)
            return;

        _skillCheckInteractIcon.sprite = _iconDatabase.GetIcon(InputDeviceDetector.CurrentDeviceType, InputIconAction.SkillCheck);
    }

    private void SetSkillCheckHintVisible(bool _visible)
    {
        if (_skillCheckInteractIcon == null)
            return;

        _skillCheckInteractIcon.enabled = _visible;

        if (_skillCheckInteractIcon.gameObject != gameObject)
            _skillCheckInteractIcon.gameObject.SetActive(_visible);
    }
}
