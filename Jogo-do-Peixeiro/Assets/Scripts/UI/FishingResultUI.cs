using UnityEngine;
using UnityEngine.Serialization;

public class FishingResultUI : MonoBehaviour
{
    [FormerlySerializedAs("fishResultPanelUI")]
    [SerializeField] private FishResultUI _fishResultUI;

    [FormerlySerializedAs("panel")]
    [SerializeField] private GameObject _legacyPanel;

    private void Awake()
    {
        ResolveReferences();
    }

    public void ShowCatchResult(FishData _fish)
    {
        ResolveReferences();

        if (_fishResultUI != null)
            _fishResultUI.ShowCatchResult(_fish);
    }

    public void ShowCatchResult(FishData _fish, bool _isNewFish)
    {
        ResolveReferences();

        if (_fishResultUI != null)
            _fishResultUI.ShowCatchResult(_fish, _isNewFish);
    }

    private void ResolveReferences()
    {
        if (_fishResultUI != null)
            return;

        if (_legacyPanel != null)
            _fishResultUI = _legacyPanel.GetComponent<FishResultUI>();

        if (_fishResultUI == null)
            _fishResultUI = FindFirstObjectByType<FishResultUI>(FindObjectsInactive.Include);
    }
}
