using System.Collections;
using TMPro;
using UnityEngine;

public class ForcedSleepController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private DayCycle _dayCycle;
    [SerializeField] private Transform _playerRoot;
    [SerializeField] private CharacterController _characterController;
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private PlayerMove _playerMove;
    [SerializeField] private PlayerInteract _playerInteract;
    [SerializeField] private BoatController _boatController;
    [SerializeField] private ShipInventory _shipInventory;

    [Header("Sleep")]
    [SerializeField] private Transform _forcedSleepPoint;
    [SerializeField] private float _verticalOffset = 0.35f;
    [SerializeField, Min(0)] private int _settleFixedFrames = 2;
    [SerializeField] private GameObject[] _panelsToClose;

    [Header("Boat Reset")]
    [SerializeField] private bool _returnBoatToParkPointOnForcedSleep = true;
    [SerializeField] private Transform _boatParkPoint;

    [Header("Forced Sleep Cargo Loss")]
    [SerializeField] private bool _loseFishOnForcedSleep = true;
    [SerializeField, Range(0f, 1f)] private float _forcedSleepFishLossRatio = 0.25f;

    [Header("Fade Transition")]
    [SerializeField] private CanvasGroup _fadeCanvasGroup;
    [SerializeField] private TMP_Text _fadeText;
    [SerializeField] private string _forcedSleepText = "Voce apagou de sono...";
    [SerializeField] private string _regularSleepText = "Dormindo...";
    [SerializeField] private float _fadeInDuration = 0.6f;
    [SerializeField] private float _fadeHoldDuration = 0.8f;
    [SerializeField] private float _fadeOutDuration = 0.6f;

    private bool _isRunning;

    private void Awake()
    {
        SetFadeAlpha(0f);
        SetFadeTextVisible(false);
    }

    private void OnEnable()
    {
        ResolveReferences();

        if (_dayCycle != null)
            _dayCycle.ForcedSleepRequested += StartForcedSleep;
    }

    private void OnDisable()
    {
        if (_dayCycle != null)
            _dayCycle.ForcedSleepRequested -= StartForcedSleep;
    }

    private void StartForcedSleep()
    {
        if (_isRunning)
            return;

        StartCoroutine(ForcedSleepRoutine());
    }

    public bool StartRegularSleep(DayCycle _targetDayCycle = null)
    {
        if (_isRunning)
            return false;

        if (_targetDayCycle != null)
            _dayCycle = _targetDayCycle;

        StartCoroutine(RegularSleepRoutine());
        return true;
    }

    private IEnumerator RegularSleepRoutine()
    {
        _isRunning = true;
        ResolveReferences();

        SetGameState(GameManager.GameState.InUI);
        ClosePanels();
        ResetInput();
        PrepareFadeText(_regularSleepText);
        yield return FadeTo(1f, _fadeInDuration);

        if (_dayCycle != null)
            _dayCycle.NextDay();

        RegisterCurrentPositionAsSafeRespawn();
        ResetInput();
        yield return new WaitForSecondsRealtime(_fadeHoldDuration);
        yield return FadeTo(0f, _fadeOutDuration);
        SetFadeTextVisible(false);

        SetGameState(GameManager.GameState.OnFoot);
        _isRunning = false;
    }

    private IEnumerator ForcedSleepRoutine()
    {
        _isRunning = true;
        ResolveReferences();

        SetGameState(GameManager.GameState.InUI);
        ClosePanels();
        ResetInput();
        PrepareFadeText(_forcedSleepText);
        yield return FadeTo(1f, _fadeInDuration);
        CompleteDayCycleSleep();

        if (FishingManager.instance != null && FishingManager.instance.IsFishing)
            FishingManager.instance.CancelFishing();

        if (_boatController != null && _boatController.IsPlayerOnBoat())
        {
            _boatController.ExitBoatForTeleport();
            ResolvePlayerReferences(true);
        }

        ReturnBoatToParkPoint();
        ApplyForcedSleepFishLoss();

        SetPlayerControllerEnabled(false);
        SetCharacterControllerEnabled(false);
        ResetPlayerMotion();

        yield return new WaitForFixedUpdate();

        TeleportPlayer();
        Physics.SyncTransforms();

        for (int i = 0; i < _settleFixedFrames; i++)
        {
            ResetPlayerMotion();
            yield return new WaitForFixedUpdate();
        }

        ResetPlayerMotion();
        SetCharacterControllerEnabled(true);
        SetPlayerControllerEnabled(true);
        ResetInput();
        Physics.SyncTransforms();
        RegisterCurrentPositionAsSafeRespawn();

        if (_playerInteract != null)
            _playerInteract.RefreshInteractablesAfterTeleport();

        yield return new WaitForSecondsRealtime(_fadeHoldDuration);
        yield return FadeTo(0f, _fadeOutDuration);
        SetFadeTextVisible(false);

        SetGameState(GameManager.GameState.OnFoot);
        _isRunning = false;
    }

    private void CompleteDayCycleSleep()
    {
        if (_dayCycle != null)
            _dayCycle.CompleteForcedSleepWakeUp();
    }

    private void TeleportPlayer()
    {
        if (_playerRoot == null || _forcedSleepPoint == null)
            return;

        _playerRoot.position = _forcedSleepPoint.position + Vector3.up * _verticalOffset;
        _playerRoot.rotation = _forcedSleepPoint.rotation;
    }

    private void RegisterCurrentPositionAsSafeRespawn()
    {
        if (_playerMove != null && _playerRoot != null)
            _playerMove.SetSafeRespawnPosition(_playerRoot.position);
    }

    private void ResolveReferences()
    {
        if (_dayCycle == null)
            _dayCycle = FindFirstObjectByType<DayCycle>(FindObjectsInactive.Include);

        ResolvePlayerReferences(false);

        if (_boatController == null)
            _boatController = FindFirstObjectByType<BoatController>(FindObjectsInactive.Include);

        if (_boatParkPoint == null)
            _boatParkPoint = FindBoatParkPoint();

        if (_shipInventory == null)
            _shipInventory = FindFirstObjectByType<ShipInventory>(FindObjectsInactive.Include);
    }

    private Transform FindBoatParkPoint()
    {
        Dock[] docks = FindObjectsByType<Dock>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Dock dock in docks)
        {
            if (dock == null)
                continue;

            Transform parkPoint = dock.BoatParkPoint;

            if (parkPoint != null)
                return parkPoint;
        }

        return FindTransformByName("boatParkPoint", "BoatParkPoint");
    }

    private Transform FindTransformByName(string _primaryName, string _fallbackName)
    {
        Transform[] transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (Transform foundTransform in transforms)
        {
            if (foundTransform.name == _primaryName || foundTransform.name == _fallbackName)
                return foundTransform;
        }

        return null;
    }

    private void ReturnBoatToParkPoint()
    {
        if (!_returnBoatToParkPointOnForcedSleep || _boatController == null || _boatParkPoint == null)
            return;

        _boatController.ReturnToParkPoint(_boatParkPoint);
        Physics.SyncTransforms();
    }

    private void ApplyForcedSleepFishLoss()
    {
        if (!_loseFishOnForcedSleep || _shipInventory == null)
            return;

        float currentWeight = _shipInventory.GetCurrentWeight();
        float targetWeightLoss = currentWeight * Mathf.Clamp01(_forcedSleepFishLossRatio);

        if (targetWeightLoss <= 0f)
            return;

        int removedCount = _shipInventory.RemoveFishUntilWeightLost(targetWeightLoss, out float removedWeight);

        if (removedCount > 0)
            Debug.Log($"Sono forcado removeu {removedCount} peixe(s), {removedWeight:0.#}kg de carga.");
    }

    private void ResolvePlayerReferences(bool _forceRefresh)
    {
        if (_forceRefresh)
        {
            _playerController = null;
            _playerMove = null;
            _characterController = null;
            _playerInteract = null;
        }

        if (_playerRoot == null)
        {
            PlayerController foundPlayerController = FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);

            if (foundPlayerController != null)
                _playerRoot = foundPlayerController.transform;
        }

        if (_playerRoot == null)
        {
            PlayerMove foundPlayerMove = FindFirstObjectByType<PlayerMove>(FindObjectsInactive.Include);

            if (foundPlayerMove != null)
                _playerRoot = foundPlayerMove.transform;
        }

        if (_playerRoot == null)
            return;

        if (_playerController == null)
            _playerController = _playerRoot.GetComponent<PlayerController>();

        if (_playerMove == null)
            _playerMove = _playerRoot.GetComponent<PlayerMove>();

        if (_characterController == null)
            _characterController = _playerRoot.GetComponent<CharacterController>();

        if (_playerInteract == null)
            _playerInteract = _playerRoot.GetComponentInChildren<PlayerInteract>(true);
    }

    private void ClosePanels()
    {
        if (_panelsToClose == null)
            return;

        foreach (GameObject panel in _panelsToClose)
        {
            if (panel != null)
                panel.SetActive(false);
        }
    }

    private void ResetInput()
    {
        if (InputHandler.instance != null)
            InputHandler.instance.ResetGameplayInput();
    }

    private void ResetPlayerMotion()
    {
        if (_playerMove != null)
            _playerMove.ResetMovementState();

        if (_playerRoot == null)
            return;

        Rigidbody[] rigidbodies = _playerRoot.GetComponentsInChildren<Rigidbody>(true);

        foreach (Rigidbody body in rigidbodies)
        {
            if (body == null)
                continue;

            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.Sleep();
        }
    }

    private void SetPlayerControllerEnabled(bool _enabled)
    {
        if (_playerController != null)
            _playerController.enabled = _enabled;
    }

    private void SetCharacterControllerEnabled(bool _enabled)
    {
        if (_characterController != null)
            _characterController.enabled = _enabled;
    }

    private void SetGameState(GameManager.GameState _state)
    {
        if (GameManager.instance != null)
            GameManager.instance.SetState(_state);
    }

    private void PrepareFadeText(string _text)
    {
        if (_fadeText != null)
        {
            _fadeText.text = _text;
            SetFadeTextVisible(true);
        }
    }

    private IEnumerator FadeTo(float _targetAlpha, float _duration)
    {
        if (_fadeCanvasGroup == null)
            yield break;

        _fadeCanvasGroup.gameObject.SetActive(true);
        _fadeCanvasGroup.blocksRaycasts = true;
        _fadeCanvasGroup.interactable = true;

        float startAlpha = _fadeCanvasGroup.alpha;
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, _duration);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetFadeAlpha(Mathf.Lerp(startAlpha, _targetAlpha, t));
            yield return null;
        }

        SetFadeAlpha(_targetAlpha);

        if (Mathf.Approximately(_targetAlpha, 0f))
        {
            _fadeCanvasGroup.blocksRaycasts = false;
            _fadeCanvasGroup.interactable = false;
        }
    }

    private void SetFadeAlpha(float _alpha)
    {
        if (_fadeCanvasGroup == null)
            return;

        _fadeCanvasGroup.alpha = _alpha;
        _fadeCanvasGroup.blocksRaycasts = _alpha > 0.01f;
        _fadeCanvasGroup.interactable = _alpha > 0.01f;
    }

    private void SetFadeTextVisible(bool _visible)
    {
        if (_fadeText != null)
            _fadeText.gameObject.SetActive(_visible);
    }
}
