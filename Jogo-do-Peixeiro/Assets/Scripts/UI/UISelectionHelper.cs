using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public static class UISelectionHelper
{
    private const float MinimumVerticalScrollSensitivity = 35f;

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

    public static void ConfigureVerticalOnlyScrollRect(ScrollRect _scrollRect)
    {
        if (_scrollRect == null)
            return;

        _scrollRect.horizontal = false;
        _scrollRect.vertical = true;
        _scrollRect.movementType = ScrollRect.MovementType.Clamped;
        _scrollRect.scrollSensitivity = Mathf.Max(_scrollRect.scrollSensitivity, MinimumVerticalScrollSensitivity);

        if (_scrollRect.horizontalScrollbar != null)
        {
            SetNavigationNone(_scrollRect.horizontalScrollbar);
            _scrollRect.horizontalScrollbar.gameObject.SetActive(false);
        }

        if (_scrollRect.verticalScrollbar != null)
            SetNavigationNone(_scrollRect.verticalScrollbar);

        _scrollRect.horizontalNormalizedPosition = 0f;

        RectTransform content = _scrollRect.content;

        if (content == null)
            return;

        Vector2 anchoredPosition = content.anchoredPosition;

        if (Mathf.Approximately(anchoredPosition.x, 0f))
            return;

        anchoredPosition.x = 0f;
        content.anchoredPosition = anchoredPosition;
    }

    public static float GetMouseScrollDeltaY()
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null)
        {
            float inputSystemScroll = NormalizeScrollDelta(Mouse.current.scroll.ReadValue().y);

            if (Mathf.Abs(inputSystemScroll) > 0.01f)
                return inputSystemScroll;
        }
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
        return NormalizeScrollDelta(Input.mouseScrollDelta.y);
#else
        return 0f;
#endif
    }

    public static bool ApplyMouseWheelScroll(ScrollRect _scrollRect, float _scrollDeltaY, float _scrollPixels)
    {
        if (_scrollRect == null ||
            !_scrollRect.gameObject.activeInHierarchy ||
            Mathf.Abs(_scrollDeltaY) <= 0.01f)
        {
            return false;
        }

        ConfigureVerticalOnlyScrollRect(_scrollRect);

        RectTransform content = _scrollRect.content;
        RectTransform viewport = _scrollRect.viewport != null
            ? _scrollRect.viewport
            : _scrollRect.GetComponent<RectTransform>();

        if (content == null || viewport == null)
            return false;

        Canvas.ForceUpdateCanvases();

        float hiddenHeight = content.rect.height - viewport.rect.height;

        if (hiddenHeight <= 0.01f)
            return false;

        float normalizedDelta = _scrollDeltaY * Mathf.Max(1f, _scrollPixels) / hiddenHeight;
        float targetPosition = Mathf.Clamp01(_scrollRect.verticalNormalizedPosition + normalizedDelta);

        if (Mathf.Approximately(targetPosition, _scrollRect.verticalNormalizedPosition))
            return false;

        _scrollRect.verticalNormalizedPosition = targetPosition;
        _scrollRect.velocity = Vector2.zero;
        return true;
    }

    public static void ConfigureVerticalContentNavigation(
        IList<Selectable> _selectables,
        Selectable _exitUp = null,
        Selectable _exitDown = null,
        Selectable _exitLeft = null,
        Selectable _exitRight = null)
    {
        if (_selectables == null)
            return;

        for (int i = 0; i < _selectables.Count; i++)
        {
            Selectable current = _selectables[i];

            if (current == null)
                continue;

            Navigation navigation = current.navigation;
            navigation.mode = Navigation.Mode.Explicit;
            navigation.selectOnUp = GetPreviousUsable(_selectables, i);
            navigation.selectOnDown = GetNextUsable(_selectables, i);
            navigation.selectOnLeft = GetUsableExit(_exitLeft);
            navigation.selectOnRight = GetUsableExit(_exitRight);

            if (navigation.selectOnUp == null)
                navigation.selectOnUp = GetUsableExit(_exitUp);

            if (navigation.selectOnDown == null)
                navigation.selectOnDown = GetUsableExit(_exitDown);

            current.navigation = navigation;
        }
    }

    private static Selectable GetUsableSelectable(Selectable _preferred, GameObject _scope)
    {
        if (IsUsable(_preferred))
            return _preferred;

        return FirstUsable(_scope);
    }

    private static void SetNavigationNone(Selectable _selectable)
    {
        if (_selectable == null)
            return;

        Navigation navigation = _selectable.navigation;
        navigation.mode = Navigation.Mode.None;
        _selectable.navigation = navigation;
    }

    private static Selectable GetPreviousUsable(IList<Selectable> _selectables, int _index)
    {
        for (int i = _index - 1; i >= 0; i--)
        {
            if (IsUsable(_selectables[i]))
                return _selectables[i];
        }

        return null;
    }

    private static Selectable GetNextUsable(IList<Selectable> _selectables, int _index)
    {
        for (int i = _index + 1; i < _selectables.Count; i++)
        {
            if (IsUsable(_selectables[i]))
                return _selectables[i];
        }

        return null;
    }

    private static Selectable GetUsableExit(Selectable _selectable)
    {
        return IsUsable(_selectable) ? _selectable : null;
    }

    private static float NormalizeScrollDelta(float _delta)
    {
        if (Mathf.Abs(_delta) > 10f)
            return _delta / 120f;

        return _delta;
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
