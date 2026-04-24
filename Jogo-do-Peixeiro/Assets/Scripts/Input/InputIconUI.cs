using UnityEngine;
using UnityEngine.UI;

public class InputIconUI : MonoBehaviour
{
    [SerializeField] private InputIconDatabase _iconDatabase;
    [SerializeField] private InputIconAction _action;
    [SerializeField] private Image _iconImage;

    private InputDeviceType _lastDeviceType;

    private void OnEnable()
    {
        UpdateIcon();
    }

    private void Update()
    {
        if (_lastDeviceType == InputDeviceDetector.CurrentDeviceType)
            return;

        UpdateIcon();
    }

    private void UpdateIcon()
    {
        if (_iconDatabase == null || _iconImage == null)
            return;

        _lastDeviceType = InputDeviceDetector.CurrentDeviceType;
        _iconImage.sprite = _iconDatabase.GetIcon(_lastDeviceType, _action);
    }
}