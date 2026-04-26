using System.Collections.Generic;
using UnityEngine;

public class DialogTester : MonoBehaviour
{
    [SerializeField] private DialogData dialog;
    [SerializeField] private TextCanvaManager canvaManager;

    [ContextMenu("teste diálogo")]
    private void DialogTest()
    {

        canvaManager.InitializeDialog(dialog);

    }

}
