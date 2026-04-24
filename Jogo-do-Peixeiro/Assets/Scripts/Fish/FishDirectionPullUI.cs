using UnityEngine;
using UnityEngine.UI;

public class FishDirectionPullUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private FishDirectionPull _directionPull;
    [SerializeField] private InputIconDatabase _iconDatabase;
    [SerializeField] private Image _directionIcon;

    private void Update()
    {
        if (_directionPull == null || !_directionPull.UseDirectionalPull || _iconDatabase == null)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);
        UpdateIcon();
    }

    private void UpdateIcon()
    {
        InputIconAction action = GetOppositeDirectionAction(_directionPull.CurrentFishDirection);

        Sprite icon = _iconDatabase.GetIcon(
            InputDeviceDetector.CurrentDeviceType,
            action
        );

        if (_directionIcon != null)
            _directionIcon.sprite = icon;
    }

    private InputIconAction GetOppositeDirectionAction(FishDirectionPull.FishForceDirection _direction)
    {
        switch (_direction)
        {
            case FishDirectionPull.FishForceDirection.Left:
                return InputIconAction.MoveRight;

            case FishDirectionPull.FishForceDirection.Right:
                return InputIconAction.MoveLeft;

            case FishDirectionPull.FishForceDirection.Up:
                return InputIconAction.MoveDown;

            case FishDirectionPull.FishForceDirection.Down:
                return InputIconAction.MoveUp;

            default:
                return InputIconAction.MoveLeft;
        }
    }

    private void SetVisible(bool _visible)
    {
        if (_directionIcon != null)
            _directionIcon.gameObject.SetActive(_visible);
    }
}