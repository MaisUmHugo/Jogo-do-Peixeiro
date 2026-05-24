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
    private bool _hasLoggedMissingReferences;
    private bool _hasLoggedMissingIcon;

    private void OnEnable()
    {
        InputDeviceDetector.DeviceTypeChanged += HandleDeviceTypeChanged;
        ResolveMissingReferences();
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
        ResolveMissingReferences();

        if (_iconDatabase == null || _iconImage == null)
        {
            LogMissingReferences();
            return;
        }

        _lastDeviceType = InputDeviceDetector.CurrentDeviceType;

        Sprite icon = _iconDatabase.GetIcon(_lastDeviceType, _action);

        if (icon == null)
        {
            _iconImage.enabled = false;
            LogMissingIcon();
            return;
        }

        _iconImage.sprite = icon;
        _iconImage.color = Color.white;
        _iconImage.enabled = true;
    }

    private void ResolveMissingReferences()
    {
        if (_iconImage == null)
            _iconImage = GetComponent<Image>();

        if (_iconDatabase == null)
            _iconDatabase = FindFirstObjectByType<InputIconDatabase>(FindObjectsInactive.Include);
    }

    private void LogMissingReferences()
    {
        if (_hasLoggedMissingReferences)
            return;

        Debug.LogWarning("[InputIconUI] Falta InputIconDatabase ou Image. Arraste as referencias no Inspector ou mantenha um InputIconDatabase ativo na cena.", this);
        _hasLoggedMissingReferences = true;
    }

    private void LogMissingIcon()
    {
        if (_hasLoggedMissingIcon)
            return;

        Debug.LogWarning($"[InputIconUI] Icone nao encontrado para {_action} em {_lastDeviceType}. Preencha o InputIconDatabase.", this);
        _hasLoggedMissingIcon = true;
    }
}
