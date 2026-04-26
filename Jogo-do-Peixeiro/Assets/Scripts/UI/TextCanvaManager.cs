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
    public float TextSpeed;

    public void Click(InputAction.CallbackContext input)
    {
        if (input.started)
        {            
            if (!isWritting)
            {
                if (textIndex >= senteces.Count - 1)
                {
                    FinishDialog();
                    return;
                }

                textIndex++;
                dialogTMPText.text = "";
                StartCoroutine(TypeLineCourotine());

            }
            else
            {
                FinishWriteSentence();
            }
        }
    }

    public void InitializeDialog(DialogData _dialog)
    {
        if (_dialog.senteces.Length == 0) return;

        nameTMPtext.text = _dialog.speakerName;
        SetDialogActive(true);
        senteces.AddRange(_dialog.senteces);
        StartCoroutine(TypeLineCourotine());
    }

    private void FinishDialog()
    {
        textIndex = 0;       
        SetDialogActive(false);
        
    }

    private void FinishWriteSentence()
    {

        StopAllCoroutines();
        dialogTMPText.text = senteces[textIndex];
        isWritting = false;

    }

    private IEnumerator TypeLineCourotine()
    {
        foreach (char _character in senteces[textIndex])
        {
            dialogTMPText.text += _character;
            isWritting = true;
            yield return new WaitForSeconds(TextSpeed);
        }
        isWritting = false;
    }

    private void SetDialogActive(bool _active)
    {
        dialogTMPText.text = "";
        dialogTMPText.gameObject.SetActive(_active);
        nameTMPtext.gameObject.SetActive(_active);
        dialogBackGroundImage.gameObject.SetActive(_active);
        nameBackGroundImage.gameObject.SetActive(_active);

    }
}
