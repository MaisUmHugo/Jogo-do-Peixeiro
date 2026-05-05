using UnityEngine;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

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
    [FormerlySerializedAs("keyboard")]
    [SerializeField] private InputIconSet _keyboard;

    [Header("Generic Controller")]
    [FormerlySerializedAs("genericController")]
    [SerializeField] private InputIconSet _genericController;

#if UNITY_EDITOR
    [ContextMenu("Fill Missing Default Icons")]
    private void FillMissingDefaultIcons()
    {
        bool changed = false;

        changed |= FillMissingKeyboardIcons();
        changed |= FillMissingControllerIcons();

        if (changed)
            EditorUtility.SetDirty(this);
    }

    private bool FillMissingKeyboardIcons()
    {
        bool changed = false;

        changed |= AssignIfMissing(ref _keyboard.moveUp, "Assets/Artes/Icones/Input/Keyboard & Mouse/Default/keyboard_w.png");
        changed |= AssignIfMissing(ref _keyboard.moveDown, "Assets/Artes/Icones/Input/Keyboard & Mouse/Default/keyboard_s.png");
        changed |= AssignIfMissing(ref _keyboard.moveLeft, "Assets/Artes/Icones/Input/Keyboard & Mouse/Default/keyboard_a.png");
        changed |= AssignIfMissing(ref _keyboard.moveRight, "Assets/Artes/Icones/Input/Keyboard & Mouse/Default/keyboard_d.png");
        changed |= AssignIfMissing(ref _keyboard.interact, "Assets/Artes/Icones/Input/Keyboard & Mouse/Default/keyboard_e.png");
        changed |= AssignIfMissing(ref _keyboard.aim, "Assets/Artes/Icones/Input/Keyboard & Mouse/Default/mouse_left.png");
        changed |= AssignIfMissing(ref _keyboard.pause, "Assets/Artes/Icones/Input/Keyboard & Mouse/Default/keyboard_escape.png");

        return changed;
    }

    private bool FillMissingControllerIcons()
    {
        bool changed = false;

        changed |= AssignIfMissing(ref _genericController.moveUp, "Assets/Artes/Icones/Input/Xbox Series/Default/xbox_stick_l_up.png");
        changed |= AssignIfMissing(ref _genericController.moveDown, "Assets/Artes/Icones/Input/Xbox Series/Default/xbox_stick_l_down.png");
        changed |= AssignIfMissing(ref _genericController.moveLeft, "Assets/Artes/Icones/Input/Xbox Series/Default/xbox_stick_l_left.png");
        changed |= AssignIfMissing(ref _genericController.moveRight, "Assets/Artes/Icones/Input/Xbox Series/Default/xbox_stick_l_right.png");
        changed |= AssignIfMissing(ref _genericController.interact, "Assets/Artes/Icones/Input/Xbox Series/Default/xbox_button_a.png");
        changed |= AssignIfMissing(ref _genericController.aim, "Assets/Artes/Icones/Input/Xbox Series/Default/xbox_rt.png");
        changed |= AssignIfMissing(ref _genericController.pause, "Assets/Artes/Icones/Input/Xbox Series/Default/xbox_button_start.png");

        return changed;
    }

    private bool AssignIfMissing(ref Sprite _sprite, string _assetPath)
    {
        if (_sprite != null)
            return false;

        Sprite defaultSprite = AssetDatabase.LoadAssetAtPath<Sprite>(_assetPath);

        if (defaultSprite == null)
            return false;

        _sprite = defaultSprite;
        return true;
    }
#endif

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
