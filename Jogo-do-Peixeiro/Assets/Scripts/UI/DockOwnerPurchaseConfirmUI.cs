using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DockOwnerPurchaseConfirmUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;
    [SerializeField] private Selectable firstSelected;
    [SerializeField] private GameObject[] groupsToHideWhileOpen;

    private Action pendingAction;
    private GameObject selectionScope;
    private Selectable selectionBeforePopup;
    private bool[] hiddenGroupPreviousStates;
    private bool areButtonsBound;

    public bool IsOpen => PanelObject != null && PanelObject.activeInHierarchy;

    private GameObject PanelObject => panel != null ? panel : gameObject;

    private void Awake()
    {
        ResolveReferences();
        BindButtons();
    }

    private void OnEnable()
    {
        ResolveReferences();
        BindButtons();
    }

    private void OnDisable()
    {
        UnbindButtons();
        RestoreHiddenGroups();
        pendingAction = null;
    }

    public void Open(string _title, string _message, Action _purchaseAction, GameObject _selectionScope, Selectable _restoreSelection = null)
    {
        ResolveReferences();
        pendingAction = _purchaseAction;
        selectionScope = _selectionScope != null ? _selectionScope : gameObject;
        selectionBeforePopup = _restoreSelection != null
            ? _restoreSelection
            : UISelectionHelper.CurrentSelectableInScope(selectionScope);

        if (titleText != null)
            titleText.text = _title;

        if (messageText != null)
            messageText.text = _message;

        if (PanelObject == null)
        {
            Confirm();
            return;
        }

        HideGroups();
        PanelObject.SetActive(true);
        UISelectionHelper.Select(
            UISelectionHelper.IsUsable(firstSelected) ? firstSelected : yesButton,
            PanelObject
        );
    }

    public bool TryHandleBack()
    {
        if (!IsOpen)
            return false;

        Close(true);
        return true;
    }

    public void Confirm()
    {
        Action action = pendingAction;
        Close(false);
        action?.Invoke();
    }

    public void Close(bool _restoreSelection)
    {
        if (PanelObject != null)
            PanelObject.SetActive(false);

        pendingAction = null;
        RestoreHiddenGroups();

        if (_restoreSelection)
            UISelectionHelper.Select(selectionBeforePopup, selectionScope);
    }

    private void ResolveReferences()
    {
        if (panel == null)
            panel = gameObject;
    }

    private void BindButtons()
    {
        if (areButtonsBound)
            return;

        if (yesButton != null)
            yesButton.onClick.AddListener(Confirm);

        if (noButton != null)
            noButton.onClick.AddListener(OnCancelClicked);

        areButtonsBound = true;
    }

    private void UnbindButtons()
    {
        if (!areButtonsBound)
            return;

        if (yesButton != null)
            yesButton.onClick.RemoveListener(Confirm);

        if (noButton != null)
            noButton.onClick.RemoveListener(OnCancelClicked);

        areButtonsBound = false;
    }

    private void OnCancelClicked()
    {
        Close(true);
    }

    private void HideGroups()
    {
        if (groupsToHideWhileOpen == null || groupsToHideWhileOpen.Length == 0)
            return;

        hiddenGroupPreviousStates = new bool[groupsToHideWhileOpen.Length];

        for (int i = 0; i < groupsToHideWhileOpen.Length; i++)
        {
            GameObject group = groupsToHideWhileOpen[i];
            hiddenGroupPreviousStates[i] = group != null && group.activeSelf;

            if (group == null || ShouldKeepVisible(group))
                continue;

            group.SetActive(false);
        }
    }

    private void RestoreHiddenGroups()
    {
        if (groupsToHideWhileOpen == null || hiddenGroupPreviousStates == null)
            return;

        for (int i = 0; i < groupsToHideWhileOpen.Length && i < hiddenGroupPreviousStates.Length; i++)
        {
            GameObject group = groupsToHideWhileOpen[i];

            if (group != null && !ShouldKeepVisible(group))
                group.SetActive(hiddenGroupPreviousStates[i]);
        }

        hiddenGroupPreviousStates = null;
    }

    private bool ShouldKeepVisible(GameObject _group)
    {
        if (_group == null || PanelObject == null)
            return false;

        return _group == PanelObject || PanelObject.transform.IsChildOf(_group.transform);
    }
}
