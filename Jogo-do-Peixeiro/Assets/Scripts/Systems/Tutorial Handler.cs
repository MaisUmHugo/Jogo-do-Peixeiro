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

    public bool isFinishedTalk = false;
    public bool isFinishedFishing = false;

    public bool isFishing = false;

    [SerializeField] private ShipInventory inventory;

    private static TutorialHandler instance;

    public static TutorialHandler Instance
    {

        get
        {
            if (instance == null)
            {

                Debug.Log("não tem instância");
                return null;

            }
            return instance;


        }
    }

    [ContextMenu("teste")]
    public void GoNextObjective()
    {
        if (currentTutorialPoint != null)
        {
            tutorialsPoints.Remove(currentTutorialPoint);
            
            if (tutorialsPoints.Count == 0) { FinishTutorial(); return; }


            tutorialTexts.RemoveAt(0);
            currentTutorialText.text = tutorialTexts[0];
            if (isFinishedTalk && isFinishedFishing == false) { isFishing = true; currentTutorialText.text += $" ({inventory.GetCurrentWeight()}/5) "; }

            currentTutorialPoint = tutorialsPoints[0];
            tutorialPointer.transform.position = tutorialsPoints[0].position;

        }

    }

    public void AttFishWeightTutorialText()
    {
        currentTutorialText.text = tutorialTexts[0];
        currentTutorialText.text += $" ({inventory.GetCurrentWeight()}/5) ";
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

        instance = this;
    }
}
