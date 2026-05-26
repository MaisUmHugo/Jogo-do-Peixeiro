using System.ComponentModel;
using UnityEngine;
using UnityEngine.Timeline;

[DisplayName("Roteiro Note")]
public class RoteiroTimelineNoteMarker : Marker
{
    public string cueId;
    public string title;
    public string speaker;
    [TextArea(3, 8)] public string text;
    public DialogSequenceAsset dialogAsset;
}
