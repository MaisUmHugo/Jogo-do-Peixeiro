using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class UISelectionHelper
{
    public static void Select(Selectable _preferred, GameObject _scope = null)
    {
        Selectable target = GetUsableSelectable(_preferred, _scope);

        if (target == null || EventSystem.current == null)
            return;

        EventSystem.current.SetSelectedGameObject(null);
        EventSystem.current.SetSelectedGameObject(target.gameObject);
    }

    public static Selectable CurrentSelectableInScope(GameObject _scope)
    {
        if (EventSystem.current == null)
            return null;

        GameObject current = EventSystem.current.currentSelectedGameObject;

        if (current == null || !IsInScope(current, _scope))
            return null;

        return current.GetComponent<Selectable>();
    }

    public static void ClearSelection(GameObject _scope = null)
    {
        if (EventSystem.current == null)
            return;

        GameObject current = EventSystem.current.currentSelectedGameObject;

        if (current == null)
            return;

        if (!IsInScope(current, _scope))
            return;

        EventSystem.current.SetSelectedGameObject(null);
    }

    public static bool IsUsable(Selectable _selectable)
    {
        return _selectable != null &&
               _selectable.gameObject.activeInHierarchy &&
               _selectable.IsInteractable();
    }

    public static Selectable FirstUsable(GameObject _scope)
    {
        if (_scope == null)
            return null;

        Selectable[] selectables = _scope.GetComponentsInChildren<Selectable>(true);

        for (int i = 0; i < selectables.Length; i++)
        {
            if (IsUsable(selectables[i]))
                return selectables[i];
        }

        return null;
    }

    private static Selectable GetUsableSelectable(Selectable _preferred, GameObject _scope)
    {
        if (IsUsable(_preferred))
            return _preferred;

        return FirstUsable(_scope);
    }

    private static bool IsInScope(GameObject _target, GameObject _scope)
    {
        if (_target == null)
            return false;

        if (_scope == null)
            return true;

        return _target == _scope || _target.transform.IsChildOf(_scope.transform);
    }
}
