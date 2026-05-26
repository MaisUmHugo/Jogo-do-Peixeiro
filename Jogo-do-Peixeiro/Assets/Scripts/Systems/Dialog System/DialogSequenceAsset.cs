using UnityEngine;

[CreateAssetMenu(fileName = "DialogSequence", menuName = "Jogo do Peixeiro/Dialog Sequence")]
public class DialogSequenceAsset : ScriptableObject
{
    [SerializeField] private string dialogueId;
    [SerializeField, TextArea(2, 5)] private string description;
    [SerializeField] private DialogSequenceLineData[] lines;

    public string DialogueId => dialogueId;
    public string Description => description;
    public bool HasLines => lines != null && lines.Length > 0;

    public DialogSequenceData ToDialogSequenceData()
    {
        return new DialogSequenceData(lines);
    }

    public DialogSequenceData GetFormatted(System.Func<string, string> _formatter)
    {
        return ToDialogSequenceData().GetFormatted(_formatter);
    }
}
