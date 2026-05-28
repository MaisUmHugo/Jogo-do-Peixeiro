using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

public struct UIModalRequest
{
    public Object owner;
    public bool pauseTime;
    public bool hideHud;
    public bool blockPause;
    public bool lockCamera;
    public GameObject[] extraHudRoots;
    public Action backAction;

    public static UIModalRequest Create(Object _owner, bool _pauseTime, bool _hideHud, bool _blockPause, bool _lockCamera = false, Action _backAction = null)
    {
        return new UIModalRequest
        {
            owner = _owner,
            pauseTime = _pauseTime,
            hideHud = _hideHud,
            blockPause = _blockPause,
            lockCamera = _lockCamera,
            backAction = _backAction
        };
    }
}

public class UIModalManager : MonoBehaviour
{
    public const int InvalidToken = -1;

    private struct ModalEntry
    {
        public int Token;
        public Object Owner;
        public bool PauseTime;
        public bool HideHud;
        public bool BlockPause;
        public bool LockCamera;
        public Action BackAction;
    }

    private struct HiddenObjectState
    {
        public GameObject Root;
        public CanvasGroup CanvasGroup;
        public bool WasActiveSelf;
        public bool WasHiddenWithCanvasGroup;
        public float OriginalAlpha;
        public bool OriginalInteractable;
        public bool OriginalBlocksRaycasts;
    }

    private struct DayCycleHudState
    {
        public DayCycle DayCycle;
        public bool HourVisible;
        public bool DayVisible;
    }

    public static UIModalManager Instance { get; private set; }
    public static bool IsPauseBlocked => blockPauseCount > 0;
    public static bool HasOpenModal => modalEntries.Count > 0;

    [Header("HUD Roots")]
    [SerializeField] private GameObject[] hudRoots;
    [SerializeField] private bool autoResolveHudRoots = true;
    [SerializeField] private bool hideHudWithCanvasGroup = true;
    [SerializeField] private bool hideDayCycleHud = true;

    [Header("Time")]
    [SerializeField] private bool restorePreviousTimeScale = true;

    private static readonly List<ModalEntry> modalEntries = new List<ModalEntry>();
    private static readonly Dictionary<GameObject, HiddenObjectState> hiddenHudStates = new Dictionary<GameObject, HiddenObjectState>();
    private static readonly Dictionary<DayCycle, DayCycleHudState> hiddenDayCycleStates = new Dictionary<DayCycle, DayCycleHudState>();
    private static readonly List<GameObject> hudRootScratch = new List<GameObject>();

    private static int nextToken = 1;
    private static int pauseTimeCount;
    private static int hideHudCount;
    private static int blockPauseCount;
    private static int cameraLockCount;
    private static float previousTimeScale = 1f;
    private static int backHandledFrame = -1;

    public static bool WasBackHandledThisFrame => backHandledFrame == Time.frameCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static int PushModal(UIModalRequest _request)
    {
        int token = nextToken++;

        modalEntries.Add(new ModalEntry
        {
            Token = token,
            Owner = _request.owner,
            PauseTime = _request.pauseTime,
            HideHud = _request.hideHud,
            BlockPause = _request.blockPause,
            LockCamera = _request.lockCamera,
            BackAction = _request.backAction
        });

        if (_request.pauseTime)
            PushTimePause();

        if (_request.blockPause)
            blockPauseCount++;

        if (_request.lockCamera)
        {
            PlayerCamera.PushCameraLock();
            cameraLockCount++;
        }

        if (_request.hideHud)
            PushHudHidden(_request.extraHudRoots, _request.owner);

        return token;
    }

    public static bool TryHandleBack()
    {
        if (WasBackHandledThisFrame)
            return true;

        if (modalEntries.Count == 0)
            return false;

        ModalEntry topEntry = modalEntries[modalEntries.Count - 1];
        MarkBackHandledThisFrame();

        topEntry.BackAction?.Invoke();
        return true;
    }

    public static bool IsTopModal(int _token)
    {
        return _token != InvalidToken &&
               modalEntries.Count > 0 &&
               modalEntries[modalEntries.Count - 1].Token == _token;
    }

    public static void MarkBackHandledThisFrame()
    {
        backHandledFrame = Time.frameCount;
    }

    public static void PopModal(ref int _token)
    {
        if (_token == InvalidToken)
            return;

        PopModal(_token);
        _token = InvalidToken;
    }

    public static void PopModal(int _token)
    {
        int index = modalEntries.FindIndex(entry => entry.Token == _token);

        if (index < 0)
            return;

        ModalEntry entry = modalEntries[index];
        modalEntries.RemoveAt(index);

        if (entry.HideHud)
            PopHudHidden();

        if (entry.LockCamera)
        {
            PlayerCamera.PopCameraLock();
            cameraLockCount = Mathf.Max(0, cameraLockCount - 1);
        }

        if (entry.BlockPause)
            blockPauseCount = Mathf.Max(0, blockPauseCount - 1);

        if (entry.PauseTime)
            PopTimePause();
    }

    public static void PopOwner(Object _owner)
    {
        if (_owner == null)
            return;

        for (int i = modalEntries.Count - 1; i >= 0; i--)
        {
            if (modalEntries[i].Owner == _owner)
                PopModal(modalEntries[i].Token);
        }
    }

    public static void ForceRestoreHudAndClearModalState()
    {
        RestoreHudRoots();
        RestoreDayCycleHud();

        modalEntries.Clear();
        pauseTimeCount = 0;
        hideHudCount = 0;
        blockPauseCount = 0;

        while (cameraLockCount > 0)
        {
            PlayerCamera.PopCameraLock();
            cameraLockCount--;
        }

        Time.timeScale = 1f;
        MarkBackHandledThisFrame();
    }

    private static void PushTimePause()
    {
        if (pauseTimeCount == 0)
        {
            previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
        }

        pauseTimeCount++;
    }

    private static void PopTimePause()
    {
        pauseTimeCount = Mathf.Max(0, pauseTimeCount - 1);

        if (pauseTimeCount > 0)
            return;

        Time.timeScale = ShouldRestorePreviousTimeScale()
            ? previousTimeScale
            : 1f;
    }

    private static bool ShouldRestorePreviousTimeScale()
    {
        return Instance == null || Instance.restorePreviousTimeScale;
    }

    private static void PushHudHidden(GameObject[] _extraHudRoots, Object _owner)
    {
        if (hideHudCount == 0)
        {
            HideHudRoots(_extraHudRoots, _owner);
            HideDayCycleHud();
        }

        hideHudCount++;
    }

    private static void PopHudHidden()
    {
        hideHudCount = Mathf.Max(0, hideHudCount - 1);

        if (hideHudCount > 0)
            return;

        RestoreHudRoots();
        RestoreDayCycleHud();
    }

    private static void HideHudRoots(GameObject[] _extraHudRoots, Object _owner)
    {
        CollectHudRoots(_extraHudRoots);

        for (int i = 0; i < hudRootScratch.Count; i++)
            HideHudRoot(hudRootScratch[i], _owner);
    }

    private static void CollectHudRoots(GameObject[] _extraHudRoots)
    {
        hudRootScratch.Clear();

        if (Instance != null && Instance.hudRoots != null)
            AddRoots(Instance.hudRoots);

        AddRoots(_extraHudRoots);

        if (Instance == null || Instance.autoResolveHudRoots)
        {
            AddComponentRoot<PlayerMoneyHud>();
            AddComponentRoot<ShipInventoryHud>();
            AddComponentRoot<TutorialUI>();
            AddComponentRoot<HUDFishInfoUI>();
            AddComponentRoot<HUDWarningUI>();
        }
    }

    private static void AddRoots(GameObject[] _roots)
    {
        if (_roots == null)
            return;

        for (int i = 0; i < _roots.Length; i++)
            AddRoot(_roots[i]);
    }

    private static void AddComponentRoot<T>() where T : Component
    {
        T[] components = Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != null)
                AddRoot(components[i].gameObject);
        }
    }

    private static void AddRoot(GameObject _root)
    {
        if (_root == null || hudRootScratch.Contains(_root))
            return;

        hudRootScratch.Add(_root);
    }

    private static void HideHudRoot(GameObject _root, Object _owner)
    {
        if (_root == null || hiddenHudStates.ContainsKey(_root) || ShouldSkipOwnerRoot(_root, _owner))
            return;

        HiddenObjectState state = new HiddenObjectState
        {
            Root = _root,
            WasActiveSelf = _root.activeSelf
        };

        if (ShouldHideHudWithCanvasGroup() && _root.activeInHierarchy)
        {
            CanvasGroup canvasGroup = _root.GetComponent<CanvasGroup>();

            if (canvasGroup == null)
                canvasGroup = _root.AddComponent<CanvasGroup>();

            state.CanvasGroup = canvasGroup;
            state.WasHiddenWithCanvasGroup = true;
            state.OriginalAlpha = canvasGroup.alpha;
            state.OriginalInteractable = canvasGroup.interactable;
            state.OriginalBlocksRaycasts = canvasGroup.blocksRaycasts;

            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
        else
        {
            _root.SetActive(false);
        }

        hiddenHudStates[_root] = state;
    }

    private static bool ShouldSkipOwnerRoot(GameObject _root, Object _owner)
    {
        GameObject ownerGameObject = GetOwnerGameObject(_owner);

        if (ownerGameObject == null)
            return false;

        return _root == ownerGameObject || ownerGameObject.transform.IsChildOf(_root.transform);
    }

    private static GameObject GetOwnerGameObject(Object _owner)
    {
        if (_owner is GameObject ownerGameObject)
            return ownerGameObject;

        if (_owner is Component ownerComponent)
            return ownerComponent.gameObject;

        return null;
    }

    private static bool ShouldHideHudWithCanvasGroup()
    {
        return Instance == null || Instance.hideHudWithCanvasGroup;
    }

    private static void RestoreHudRoots()
    {
        foreach (HiddenObjectState state in hiddenHudStates.Values)
        {
            if (state.Root == null)
                continue;

            if (state.WasHiddenWithCanvasGroup && state.CanvasGroup != null)
            {
                state.CanvasGroup.alpha = state.OriginalAlpha;
                state.CanvasGroup.interactable = state.OriginalInteractable;
                state.CanvasGroup.blocksRaycasts = state.OriginalBlocksRaycasts;
                continue;
            }

            state.Root.SetActive(state.WasActiveSelf);
        }

        hiddenHudStates.Clear();
    }

    private static void HideDayCycleHud()
    {
        if (Instance != null && !Instance.hideDayCycleHud)
            return;

        DayCycle[] dayCycles = Object.FindObjectsByType<DayCycle>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        for (int i = 0; i < dayCycles.Length; i++)
        {
            DayCycle dayCycle = dayCycles[i];

            if (dayCycle == null || hiddenDayCycleStates.ContainsKey(dayCycle))
                continue;

            hiddenDayCycleStates[dayCycle] = new DayCycleHudState
            {
                DayCycle = dayCycle,
                HourVisible = dayCycle.IsHourTextVisible,
                DayVisible = dayCycle.IsDayTextVisible
            };

            dayCycle.SetDayCycleHudVisible(false);
        }
    }

    private static void RestoreDayCycleHud()
    {
        foreach (DayCycleHudState state in hiddenDayCycleStates.Values)
        {
            if (state.DayCycle != null)
                state.DayCycle.SetDayCycleHudVisible(state.HourVisible, state.DayVisible);
        }

        hiddenDayCycleStates.Clear();
    }
}
