using System.Collections;
using TMPro;
using UnityEngine;

public class HUDFishInfoUI : MonoBehaviour
{
    private static HUDFishInfoUI instance;

    public static HUDFishInfoUI Instance
    {
        get
        {
            if (instance == null)
                instance = FindFirstObjectByType<HUDFishInfoUI>(FindObjectsInactive.Include);

            return instance;
        }
        private set => instance = value;
    }

    [Header("References")]
    [SerializeField] private TMP_Text fishInfoText;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Settings")]
    [SerializeField] private float visibleTime = 1.2f;
    [SerializeField] private float minimumVisibleTime = 2f;
    [SerializeField] private float fadeSpeed = 5f;
    [SerializeField] private bool useUnscaledTime = true;
    [SerializeField] private bool activateHierarchyWhenShown = true;

    private Coroutine messageRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        AutoAssignReferences();
        ClearVisual();
    }

    private void OnEnable()
    {
        if (Instance == null)
            Instance = this;

        AutoAssignReferences();
    }

    private void OnValidate()
    {
        visibleTime = Mathf.Max(0f, visibleTime);
        minimumVisibleTime = Mathf.Max(0f, minimumVisibleTime);
        fadeSpeed = Mathf.Max(0.01f, fadeSpeed);
    }

    public void ShowFishInfo(string _fishName, int _weight)
    {
        string fishName = string.IsNullOrWhiteSpace(_fishName) ? "Peixe" : _fishName;
        ShowMessage($"{fishName} +{_weight}kg");
    }

    public void ShowFishInfo(FishData _fish)
    {
        if (_fish == null || _fish.typeOfFish == null)
            return;

        string fishName = !string.IsNullOrWhiteSpace(_fish.typeOfFish.fishName)
            ? _fish.typeOfFish.fishName
            : _fish.typeOfFish.name;

        ShowFishInfo(fishName, _fish.weight);
    }

    private void ShowMessage(string _message)
    {
        EnsureCanShow();
        AutoAssignReferences();

        if (fishInfoText == null)
        {
            Debug.LogWarning("HUDFishInfoUI sem TMP_Text configurado.");
            return;
        }

        if (messageRoutine != null)
            StopCoroutine(messageRoutine);

        messageRoutine = StartCoroutine(ShowFishInfoRoutine(_message));
    }

    private IEnumerator ShowFishInfoRoutine(string _message)
    {
        fishInfoText.text = _message;
        SetCanvasVisible(true);

        yield return WaitForSeconds(GetVisibleDuration());

        if (canvasGroup != null)
        {
            while (canvasGroup.alpha > 0f)
            {
                canvasGroup.alpha -= fadeSpeed * GetDeltaTime();
                yield return null;
            }

            canvasGroup.alpha = 0f;
        }

        fishInfoText.text = string.Empty;
        messageRoutine = null;
    }

    private void EnsureCanShow()
    {
        if (!activateHierarchyWhenShown)
            return;

        Transform current = transform;

        while (current != null)
        {
            if (!current.gameObject.activeSelf)
                current.gameObject.SetActive(true);

            current = current.parent;
        }

        if (!enabled)
            enabled = true;
    }

    private void AutoAssignReferences()
    {
        if (fishInfoText == null)
            fishInfoText = GetComponentInChildren<TMP_Text>(true);

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = GetComponentInChildren<CanvasGroup>(true);

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private void ClearVisual()
    {
        if (fishInfoText != null)
            fishInfoText.text = string.Empty;

        SetCanvasVisible(false);
    }

    private void SetCanvasVisible(bool _visible)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.alpha = _visible ? 1f : 0f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private float GetVisibleDuration()
    {
        return Mathf.Max(visibleTime, minimumVisibleTime);
    }

    private float GetDeltaTime()
    {
        return useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
    }

    private IEnumerator WaitForSeconds(float _duration)
    {
        if (!useUnscaledTime)
        {
            yield return new WaitForSeconds(_duration);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < _duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }
}
