using TMPro;
using UnityEngine;

public class TutorialUI : MonoBehaviour
{
    [SerializeField] private TMP_Text objectiveText;

    public void SetObjectiveText(string _text)
    {
        if (objectiveText != null)
            objectiveText.text = _text;
    }
}