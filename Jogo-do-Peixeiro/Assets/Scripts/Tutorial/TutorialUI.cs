using TMPro;
using UnityEngine;

public class TutorialUI : MonoBehaviour
{
    [SerializeField] private GameObject objectiveRoot;
    [SerializeField] private GameObject objectiveTitle;
    [SerializeField] private TMP_Text objectiveText;

    public void SetObjectiveText(string _text)
    {
        if (objectiveText != null)
            objectiveText.text = _text;
    }

    public void SetObjectiveVisible(bool _visible)
    {
        if (objectiveRoot != null)
            objectiveRoot.SetActive(_visible);

        if (objectiveTitle != null && objectiveTitle != objectiveRoot)
            objectiveTitle.SetActive(_visible);

        if (objectiveText != null && objectiveText.gameObject != objectiveRoot)
            objectiveText.gameObject.SetActive(_visible);
    }

    public void ClearObjectiveText()
    {
        SetObjectiveText(string.Empty);
    }
}
