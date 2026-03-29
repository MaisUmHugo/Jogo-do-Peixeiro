using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TutorialHandler : MonoBehaviour
{
    [SerializeField] private List<Transform> tutorialsPoints = new List<Transform>();
    private Transform currentTutorialPoint;

    [SerializeField] private List<string> tutorialTexts = new List<string>();
    [SerializeField] private TMP_Text currentTutorialText;

    [SerializeField] private GameObject tutorialPointer;

    [SerializeField] private GameObject finishPanel;

    [ContextMenu("teste")]
    public void GoNextObjective()
    {
        if (currentTutorialPoint != null)
        {
            tutorialsPoints.Remove(currentTutorialPoint);
            
            if (tutorialsPoints.Count == 0) { FinishTutorial(); return; }

            tutorialTexts.RemoveAt(0);
            currentTutorialText.text = tutorialTexts[0];

            currentTutorialPoint = tutorialsPoints[0];
            tutorialPointer.transform.position = tutorialsPoints[0].position;

        }

    }

    void FinishTutorial()
    {
        Time.timeScale = 0.0f;
        finishPanel.SetActive(true);

    }

    private void Awake()
    {
        currentTutorialPoint = tutorialsPoints[0];
        currentTutorialText.text = tutorialTexts[0]; 
        tutorialPointer.transform.position = tutorialsPoints[0].position;
    }

    private void Update()
    {
        
    }
}
