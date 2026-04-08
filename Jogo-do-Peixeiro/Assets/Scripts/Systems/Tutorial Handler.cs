using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class TutorialHandler : MonoBehaviour
{
    [SerializeField] private List<Transform> tutorialsPoints = new List<Transform>();
    private Transform currentTutorialPoint;

    [SerializeField] private List<string> tutorialTexts = new List<string>();
    [SerializeField] private TMP_Text currentTutorialText;

    [SerializeField] private GameObject tutorialPointer;
    [SerializeField] private GameObject finishPanel;

    [SerializeField] private InteractionUI interactionUI;

    [Header("Scene")]
    [SerializeField] private string mainMenuSceneName = "Main Menu";

    public bool isFinishedTalk = false;
    public bool isFinishedFishing = false;
    public bool isFishing = false;

    [SerializeField] private ShipInventory inventory;

    public bool IsTutorialFinished { get; private set; }

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
        if (IsTutorialFinished)
            return;

        if (currentTutorialPoint == null)
            return;

        tutorialsPoints.Remove(currentTutorialPoint);

        if (tutorialTexts.Count > 0)
            tutorialTexts.RemoveAt(0);

        if (tutorialsPoints.Count == 0)
        {
            FinishTutorial();
            return;
        }

        if (currentTutorialText != null && tutorialTexts.Count > 0)
        {
            currentTutorialText.text = tutorialTexts[0];

            if (isFinishedTalk && !isFinishedFishing)
            {
                isFishing = true;
                currentTutorialText.text += $" ({inventory.GetCurrentWeight()}/10) ";
            }
        }

        currentTutorialPoint = tutorialsPoints[0];

        if (tutorialPointer != null)
        {
            tutorialPointer.SetActive(true);
            tutorialPointer.transform.position = currentTutorialPoint.position;
        }
    }

    public void AttFishWeightTutorialText()
    {
        if (IsTutorialFinished)
            return;

        if (currentTutorialText == null || tutorialTexts.Count == 0)
            return;

        currentTutorialText.text = tutorialTexts[0];
        currentTutorialText.text += $" ({inventory.GetCurrentWeight()}/10) ";
    }

    private void FinishTutorial()
    {
        IsTutorialFinished = true;
        isFinishedFishing = true;
        isFishing = false;
        currentTutorialPoint = null;

        if (tutorialPointer != null)
            tutorialPointer.SetActive(false);

        if (currentTutorialText != null)
            currentTutorialText.text = "Tutorial concluído!";

        if (finishPanel != null)
            finishPanel.SetActive(false);

        if (interactionUI != null)
            interactionUI.Hide();

        Debug.Log("Tutorial concluído.");
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void Awake()
    {
        instance = this;
        IsTutorialFinished = false;

        if (tutorialsPoints.Count > 0)
            currentTutorialPoint = tutorialsPoints[0];

        if (tutorialTexts.Count > 0 && currentTutorialText != null)
            currentTutorialText.text = tutorialTexts[0];

        if (tutorialPointer != null)
        {
            if (tutorialsPoints.Count > 0)
            {
                tutorialPointer.SetActive(true);
                tutorialPointer.transform.position = tutorialsPoints[0].position;
            }
            else
            {
                tutorialPointer.SetActive(false);
            }
        }

        if (finishPanel != null)
            finishPanel.SetActive(false);
    }

    //if (interactionUI != null)
    //    interactionUI.Hide();

    //Cursor.lockState = CursorLockMode.None;
    //Cursor.visible = true;
}