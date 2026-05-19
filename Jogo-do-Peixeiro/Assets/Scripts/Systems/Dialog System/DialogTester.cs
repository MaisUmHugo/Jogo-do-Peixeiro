using UnityEngine;

public class DialogTester : MonoBehaviour
{
    [SerializeField] private DialogSequenceData dialog;
    [SerializeField] private TextCanvaManager canvaManager;

    [ContextMenu("Test Dialog")]
    private void DialogTest()
    {

        canvaManager.InitializeDialog(dialog);

    }

}
