using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DialogSequenceLineData
{
    [SerializeField] private string speakerName;
    [SerializeField, TextArea] private string sentence;
    [SerializeField] private DialogCameraFocusTarget cameraFocusTarget;

    public string SpeakerName => speakerName;
    public string Sentence => sentence;
    public DialogCameraFocusTarget CameraFocusTarget => cameraFocusTarget;

    public DialogSequenceLineData()
    {
    }

    public DialogSequenceLineData(string _speakerName, string _sentence, DialogCameraFocusTarget _cameraFocusTarget = null)
    {
        speakerName = _speakerName;
        sentence = _sentence;
        cameraFocusTarget = _cameraFocusTarget;
    }

    public DialogSequenceLineData GetFormatted(Func<string, string> _formatter)
    {
        if (_formatter == null)
            return new DialogSequenceLineData(speakerName, sentence, cameraFocusTarget);

        return new DialogSequenceLineData(
            _formatter(speakerName),
            _formatter(sentence),
            cameraFocusTarget
        );
    }
}

[Serializable]
public class DialogSequenceData
{
    [SerializeField] private DialogSequenceLineData[] lines;

    [HideInInspector] public string speakerName;
    [HideInInspector] public string[] senteces;

    public bool HasLines => GetLineCount() > 0;

    public DialogSequenceData()
    {
    }

    public DialogSequenceData(DialogSequenceLineData[] _lines)
    {
        lines = _lines;
    }

    public DialogSequenceLineData[] GetLines()
    {
        if (lines != null && lines.Length > 0)
            return lines;

        return GetLegacyLines();
    }

    public int GetLineCount()
    {
        if (lines != null && lines.Length > 0)
            return lines.Length;

        return senteces != null ? senteces.Length : 0;
    }

    public DialogSequenceData GetFormatted(Func<string, string> _formatter)
    {
        DialogSequenceLineData[] sourceLines = GetLines();
        DialogSequenceLineData[] formattedLines = new DialogSequenceLineData[sourceLines.Length];

        for (int i = 0; i < sourceLines.Length; i++)
        {
            DialogSequenceLineData line = sourceLines[i];
            formattedLines[i] = line != null
                ? line.GetFormatted(_formatter)
                : new DialogSequenceLineData();
        }

        return new DialogSequenceData
        {
            lines = formattedLines
        };
    }

    private DialogSequenceLineData[] GetLegacyLines()
    {
        if (senteces == null || senteces.Length == 0)
            return Array.Empty<DialogSequenceLineData>();

        List<DialogSequenceLineData> legacyLines = new List<DialogSequenceLineData>(senteces.Length);

        for (int i = 0; i < senteces.Length; i++)
            legacyLines.Add(new DialogSequenceLineData(speakerName, senteces[i]));

        return legacyLines.ToArray();
    }
}
