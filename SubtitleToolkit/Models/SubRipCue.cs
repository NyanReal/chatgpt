using System;
using System.Collections.Generic;
using System.Linq;

namespace SubtitleToolkit.Models;

public class SubRipCue
{
    public int Index { get; set; }
    public TimeSpan Start { get; set; }
    public TimeSpan End { get; set; }
    public List<string> Lines { get; set; } = new();

    public string Text => string.Join(" ", Lines.Select(l => l.Trim()))
        .Replace("\u200b", string.Empty)
        .Replace("\ufeff", string.Empty)
        .Trim();

    public int CharacterCount => Text.Replace(" ", string.Empty).Length;

    public SubRipCue CloneWithIndex(int index)
    {
        return new SubRipCue
        {
            Index = index,
            Start = Start,
            End = End,
            Lines = new List<string>(Lines)
        };
    }
}
