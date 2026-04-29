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
    public float TextSpeed;
    public bool IsDialogActive { get; private set; }

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

        StopAllCoroutines();
        senteces.Clear();
        textIndex = 0;
        isWritting = false;
        onDialogFinished = _onFinished;

        if (nameTMPtext != null)
            nameTMPtext.text = _dialog.speakerName;

        SetDialogActive(true);
        senteces.AddRange(_dialog.senteces);
        StartCoroutine(TypeLineCourotine());
    }

    private void FinishDialog()
    {
        Action callback = onDialogFinished;
        onDialogFinished = null;
        textIndex = 0;
        senteces.Clear();
        isWritting = false;
        SetDialogActive(false);
        callback?.Invoke();
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
