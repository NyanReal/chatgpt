using System;
using SubtitleToolkit.Models;

namespace SubtitleToolkit.ViewModels;

public class CueViewModel
{
    public CueViewModel(SubRipCue cue)
    {
        Cue = cue;
    }

    public SubRipCue Cue { get; }

    public int Index => Cue.Index;
    public TimeSpan Start => Cue.Start;
    public TimeSpan End => Cue.End;
    public string Text => string.Join(" ", Cue.Lines);
}
