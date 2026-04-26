using UnityEngine;

[CreateAssetMenu(menuName = "Input/Input Icon Database")]
public class InputIconDatabase : ScriptableObject
{
    [System.Serializable]
    public struct InputIconSet
    {
        public Sprite moveUp;
        public Sprite moveDown;
        public Sprite moveLeft;
        public Sprite moveRight;
        public Sprite interact;
        public Sprite aim;
        public Sprite pause;
    }

    [Header("Keyboard")]
    [SerializeField] private InputIconSet _keyboard;

    [Header("Generic Controller")]
    [SerializeField] private InputIconSet _genericController;

    public Sprite GetIcon(InputDeviceType _deviceType, InputIconAction _action)
    {
        InputIconSet iconSet = GetIconSet(_deviceType);

        switch (_action)
        {
            case InputIconAction.MoveUp:
                return iconSet.moveUp;

            case InputIconAction.MoveDown:
                return iconSet.moveDown;

            case InputIconAction.MoveLeft:
                return iconSet.moveLeft;

            case InputIconAction.MoveRight:
                return iconSet.moveRight;

            case InputIconAction.Interact:
                return iconSet.interact;

            case InputIconAction.Aim:
                return iconSet.aim;

            case InputIconAction.Pause:
                return iconSet.pause;

            default:
                return null;
        }
    }

    private InputIconSet GetIconSet(InputDeviceType _deviceType)
    {
        switch (_deviceType)
        {
            case InputDeviceType.GenericController:
                return _genericController;

            default:
                return _keyboard;
        }
    }
}