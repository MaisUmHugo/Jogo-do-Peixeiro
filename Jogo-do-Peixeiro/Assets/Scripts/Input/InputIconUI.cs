using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class InputIconUI : MonoBehaviour
{
    [FormerlySerializedAs("iconDatabase")]
    [SerializeField] private InputIconDatabase _iconDatabase;
    [FormerlySerializedAs("action")]
    [SerializeField] private InputIconAction _action;
    [FormerlySerializedAs("iconImage")]
    [SerializeField] private Image _iconImage;

    private InputDeviceType _lastDeviceType;

    private void OnEnable()
    {
        InputDeviceDetector.DeviceTypeChanged += HandleDeviceTypeChanged;
        UpdateIcon();
    }

    private void OnDisable()
    {
        InputDeviceDetector.DeviceTypeChanged -= HandleDeviceTypeChanged;
    }

    private void HandleDeviceTypeChanged(InputDeviceType _deviceType)
    {
        UpdateIcon();
    }

    private void UpdateIcon()
    {
        if (_iconDatabase == null || _iconImage == null)
            return;

        _lastDeviceType = InputDeviceDetector.CurrentDeviceType;

        Sprite icon = _iconDatabase.GetIcon(_lastDeviceType, _action);

        if (icon == null)
        {
            _iconImage.enabled = false;
            return;
        }

        _iconImage.sprite = icon;
        _iconImage.color = Color.white;
        _iconImage.enabled = true;
    }
}
