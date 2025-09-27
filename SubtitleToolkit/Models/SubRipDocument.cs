using System.Collections.Generic;
using System.Linq;

namespace SubtitleToolkit.Models;

public class SubRipDocument
{
    public List<SubRipCue> Cues { get; set; } = new();

    public SubRipDocument Clone()
    {
        return new SubRipDocument
        {
            Cues = Cues.Select(cue => new SubRipCue
            {
                Index = cue.Index,
                Start = cue.Start,
                End = cue.End,
                Lines = new List<string>(cue.Lines)
            }).ToList()
        };
    }
}
