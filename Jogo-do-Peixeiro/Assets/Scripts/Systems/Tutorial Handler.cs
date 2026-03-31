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
    [SerializeField] private string mainMenuSceneName = "MainMenu";

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
        if (currentTutorialPoint != null)
        {
            tutorialsPoints.Remove(currentTutorialPoint);

            if (tutorialsPoints.Count == 0)
            {
                FinishTutorial();
                return;
            }

            tutorialTexts.RemoveAt(0);
            currentTutorialText.text = tutorialTexts[0];

            if (isFinishedTalk && isFinishedFishing == false)
            {
                isFishing = true;
                currentTutorialText.text += $" ({inventory.GetCurrentWeight()}/10) ";
            }

            currentTutorialPoint = tutorialsPoints[0];
            tutorialPointer.transform.position = tutorialsPoints[0].position;
        }
    }

    public void AttFishWeightTutorialText()
    {
        currentTutorialText.text = tutorialTexts[0];
        currentTutorialText.text += $" ({inventory.GetCurrentWeight()}/10) ";
    }

    private void FinishTutorial()
    {
        IsTutorialFinished = true;

        Time.timeScale = 0f;

        if (tutorialPointer != null)
            tutorialPointer.SetActive(false);

        if (finishPanel != null)
            finishPanel.SetActive(true);

        if (interactionUI != null)
            interactionUI.Hide();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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

        if (tutorialPointer != null && tutorialsPoints.Count > 0)
            tutorialPointer.transform.position = tutorialsPoints[0].position;

        if (finishPanel != null)
            finishPanel.SetActive(false);
    }
}