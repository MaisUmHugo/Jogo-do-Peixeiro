using System;
using System.Collections.Generic;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class TextCanvaManager : MonoBehaviour
{
    [Header("Dialog Componentes")]
    [SerializeField] private TMP_Text dialogTMPText;
    [SerializeField] private TMP_Text nameTMPtext;
    [SerializeField] private Image dialogBackGroundImage;
    [SerializeField] private Image nameBackGroundImage;

    
    private List<string> senteces = new List<string>();
    private int textIndex = 0;
    private bool isWritting = false;
    private Action onDialogFinished;
    private int dialogStartedFrame = -1;
    private bool isSubscribedToInput;
    public float TextSpeed;
    public bool IsDialogActive { get; private set; }

    private void OnEnable()
    {
        TrySubscribeInput();
    }

    private void Start()
    {
        TrySubscribeInput();
    }

    private void OnDisable()
    {
        UnsubscribeInput();
    }

    public void Click(InputAction.CallbackContext input)
    {
        if (input.started)
            AdvanceDialog();
    }

    public void AdvanceDialog()
    {
        if (!IsDialogActive)
            return;

        if (!isWritting)
        {
            if (textIndex >= senteces.Count - 1)
            {
                FinishDialog();
                return;
            }

            textIndex++;

            if (dialogTMPText != null)
                dialogTMPText.text = "";

            StartCoroutine(TypeLineCourotine());
        }
        else
        {
            FinishWriteSentence();
        }
    }

    public void InitializeDialog(DialogData _dialog)
    {
        InitializeDialog(_dialog, null);
    }

    public void InitializeDialog(DialogData _dialog, Action _onFinished)
    {
        if (_dialog == null || _dialog.senteces == null || _dialog.senteces.Length == 0)
        {
            _onFinished?.Invoke();
            return;
        }

        TrySubscribeInput();
        StopAllCoroutines();
        senteces.Clear();
        textIndex = 0;
        isWritting = false;
        onDialogFinished = _onFinished;
        dialogStartedFrame = Time.frameCount;

        if (nameTMPtext != null)
            nameTMPtext.text = _dialog.speakerName;

        SetDialogActive(true);
        senteces.AddRange(_dialog.senteces);
        StartCoroutine(TypeLineCourotine());
    }

    public void CloseDialog()
    {
        CloseDialog(false);
    }

    public void CloseDialog(bool _invokeCallback)
    {
        if (!IsDialogActive && senteces.Count == 0)
            return;

        StopAllCoroutines();

        Action callback = _invokeCallback ? onDialogFinished : null;
        onDialogFinished = null;
        textIndex = 0;
        senteces.Clear();
        isWritting = false;
        SetDialogActive(false);
        callback?.Invoke();
    }

    private void FinishDialog()
    {
        CloseDialog(true);
    }

    private void FinishWriteSentence()
    {
        if (senteces.Count == 0)
            return;

        StopAllCoroutines();
        
        if (dialogTMPText != null)
            dialogTMPText.text = senteces[textIndex];

        isWritting = false;

    }

    private IEnumerator TypeLineCourotine()
    {
        if (senteces.Count == 0 || dialogTMPText == null)
            yield break;

        foreach (char _character in senteces[textIndex])
        {
            dialogTMPText.text += _character;
            isWritting = true;
            yield return new WaitForSecondsRealtime(TextSpeed);
        }
        isWritting = false;
    }

    private void HandleInteractPressed()
    {
        if (Time.frameCount == dialogStartedFrame)
            return;

        AdvanceDialog();
    }

    private void TrySubscribeInput()
    {
        if (isSubscribedToInput || InputHandler.instance == null)
            return;

        InputHandler.instance.onInteractPressed += HandleInteractPressed;
        isSubscribedToInput = true;
    }

    private void UnsubscribeInput()
    {
        if (!isSubscribedToInput || InputHandler.instance == null)
            return;

        InputHandler.instance.onInteractPressed -= HandleInteractPressed;
        isSubscribedToInput = false;
    }

    private void SetDialogActive(bool _active)
    {
        IsDialogActive = _active;

        if (dialogTMPText != null)
        {
            dialogTMPText.text = "";
            dialogTMPText.gameObject.SetActive(_active);
        }

        if (nameTMPtext != null)
            nameTMPtext.gameObject.SetActive(_active);

        if (dialogBackGroundImage != null)
            dialogBackGroundImage.gameObject.SetActive(_active);

        if (nameBackGroundImage != null)
            nameBackGroundImage.gameObject.SetActive(_active);

    }
}
