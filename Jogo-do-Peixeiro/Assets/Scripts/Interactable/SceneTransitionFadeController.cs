using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneTransitionFadeController : MonoBehaviour
{
    private const string FadeControllerName = "SceneTransitionFadeController";
    private const int FadeSortingOrder = 32000;

    private static SceneTransitionFadeController instance;

    private CanvasGroup canvasGroup;
    private Coroutine fadeRoutine;
    private bool fadeInRequestedOnNextScene;
    private float requestedFadeInDuration = 0.55f;
    private float requestedFadeInDelay = 0.15f;

    public static IEnumerator FadeOut(float _duration)
    {
        SceneTransitionFadeController controller = GetOrCreate();
        controller.StopActiveFade();
        yield return controller.FadeTo(1f, _duration);
    }

    public static void RequestFadeInOnNextScene(float _duration, float _delay)
    {
        SceneTransitionFadeController controller = GetOrCreate();
        controller.fadeInRequestedOnNextScene = true;
        controller.requestedFadeInDuration = Mathf.Max(0f, _duration);
        controller.requestedFadeInDelay = Mathf.Max(0f, _delay);
    }

    public static void FadeIn(float _duration, float _delay = 0f)
    {
        SceneTransitionFadeController controller = GetOrCreate();
        controller.StartFadeIn(_duration, _delay);
    }

    public static IEnumerator FadeInAndWait(float _duration, float _delay = 0f)
    {
        SceneTransitionFadeController controller = GetOrCreate();
        controller.StopActiveFade();

        if (_delay > 0f)
            yield return new WaitForSecondsRealtime(_delay);

        yield return controller.FadeTo(0f, _duration);
    }

    public static void SetBlackImmediate()
    {
        SceneTransitionFadeController controller = GetOrCreate();
        controller.StopActiveFade();
        controller.SetAlpha(1f);
    }

    private static SceneTransitionFadeController GetOrCreate()
    {
        if (instance != null)
            return instance;

        instance = FindFirstObjectByType<SceneTransitionFadeController>(FindObjectsInactive.Include);

        if (instance != null)
        {
            instance.EnsureOverlay();
            return instance;
        }

        GameObject fadeObject = new GameObject(FadeControllerName);
        DontDestroyOnLoad(fadeObject);
        instance = fadeObject.AddComponent<SceneTransitionFadeController>();
        instance.EnsureOverlay();
        return instance;
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureOverlay();
        SetAlpha(0f);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(Scene _scene, LoadSceneMode _mode)
    {
        if (!fadeInRequestedOnNextScene)
            return;

        fadeInRequestedOnNextScene = false;
        StartFadeIn(requestedFadeInDuration, requestedFadeInDelay);
    }

    private void StartFadeIn(float _duration, float _delay)
    {
        StopActiveFade();
        fadeRoutine = StartCoroutine(FadeInRoutine(_duration, _delay));
    }

    private IEnumerator FadeInRoutine(float _duration, float _delay)
    {
        if (_delay > 0f)
            yield return new WaitForSecondsRealtime(_delay);

        yield return FadeTo(0f, _duration);
        fadeRoutine = null;
    }

    private IEnumerator FadeTo(float _targetAlpha, float _duration)
    {
        EnsureOverlay();

        float startAlpha = canvasGroup.alpha;
        float duration = Mathf.Max(0f, _duration);

        SetRaycastBlocking(true);

        if (duration <= 0f)
        {
            SetAlpha(_targetAlpha);
            yield break;
        }

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetAlpha(Mathf.Lerp(startAlpha, _targetAlpha, t));
            yield return null;
        }

        SetAlpha(_targetAlpha);
    }

    private void StopActiveFade()
    {
        if (fadeRoutine == null)
            return;

        StopCoroutine(fadeRoutine);
        fadeRoutine = null;
    }

    private void EnsureOverlay()
    {
        if (canvasGroup != null)
            return;

        Canvas canvas = GetComponent<Canvas>();

        if (canvas == null)
            canvas = gameObject.AddComponent<Canvas>();

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = FadeSortingOrder;

        CanvasScaler scaler = GetComponent<CanvasScaler>();

        if (scaler == null)
            scaler = gameObject.AddComponent<CanvasScaler>();

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (transform.childCount == 0)
            CreateFadeImage();
    }

    private void CreateFadeImage()
    {
        GameObject imageObject = new GameObject("BlackFadeImage", typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(transform, false);

        RectTransform rectTransform = imageObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;

        Image image = imageObject.GetComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = false;
    }

    private void SetAlpha(float _alpha)
    {
        EnsureOverlay();

        float alpha = Mathf.Clamp01(_alpha);
        canvasGroup.alpha = alpha;
        SetRaycastBlocking(alpha > 0.01f);
    }

    private void SetRaycastBlocking(bool _blocking)
    {
        if (canvasGroup == null)
            return;

        canvasGroup.blocksRaycasts = _blocking;
        canvasGroup.interactable = _blocking;
    }
}
