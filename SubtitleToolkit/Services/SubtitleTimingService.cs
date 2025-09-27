using System;
using System.Collections.Generic;
using System.Linq;
using SubtitleToolkit.Models;

namespace SubtitleToolkit.Services;

public class SubtitleTimingService
{
    public SubRipDocument Stretch(SubRipDocument document, double charactersPerSecond, TimeSpan minimumDuration, TimeSpan minimumGap)
    {
        if (charactersPerSecond <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(charactersPerSecond));
        }

        var clone = document.Clone();
        var cues = clone.Cues;
        for (int i = 0; i < cues.Count; i++)
        {
            var cue = cues[i];
            var prevEnd = i > 0 ? cues[i - 1].End : TimeSpan.Zero;
            var nextStart = i < cues.Count - 1 ? cues[i + 1].Start : (TimeSpan?)null;

            int charCount = CueCharacterCount(cue);
            if (charCount == 0)
            {
                continue;
            }

            var requiredMs = Math.Max(minimumDuration.TotalMilliseconds,
                Math.Ceiling(charCount / charactersPerSecond * 1000.0));
            var required = TimeSpan.FromMilliseconds(requiredMs);
            var current = cue.End - cue.Start;
            if (current >= required)
            {
                continue;
            }

            var needed = required - current;

            if (nextStart is null)
            {
                var addEnd = needed;
                cue.End += addEnd;
                needed = TimeSpan.Zero;
            }
            else
            {
                var gapToNext = nextStart.Value - cue.End;
                var forwardRoom = gapToNext - minimumGap;
                if (forwardRoom > TimeSpan.Zero)
                {
                    var addForward = forwardRoom < needed ? forwardRoom : needed;
                    cue.End += addForward;
                    needed -= addForward;
                }
            }

            if (needed > TimeSpan.Zero)
            {
                var gapFromPrev = cue.Start - prevEnd;
                var backwardRoom = gapFromPrev - minimumGap;
                if (backwardRoom > TimeSpan.Zero)
                {
                    var addBackward = backwardRoom < needed ? backwardRoom : needed;
                    cue.Start -= addBackward;
                    needed -= addBackward;
                }
            }

            if (cue.Start < TimeSpan.Zero)
            {
                cue.Start = TimeSpan.Zero;
            }
        }

        return clone;
    }

    public IReadOnlyList<string> GetShortCueDiagnostics(SubRipDocument document, double charactersPerSecond, TimeSpan minimumDuration)
    {
        var diagnostics = new List<string>();
        foreach (var cue in document.Cues)
        {
            int charCount = CueCharacterCount(cue);
            if (charCount == 0)
            {
                continue;
            }

            var requiredMs = Math.Max(minimumDuration.TotalMilliseconds,
                Math.Ceiling(charCount / charactersPerSecond * 1000.0));
            var required = TimeSpan.FromMilliseconds(requiredMs);
            var current = cue.End - cue.Start;
            if (current + TimeSpan.FromMilliseconds(1) < required)
            {
                diagnostics.Add($"#{cue.Index}: {current.TotalMilliseconds:F0}ms < {required.TotalMilliseconds:F0}ms (문자 {charCount})");
            }
        }

        return diagnostics;
    }

    private static int CueCharacterCount(SubRipCue cue)
    {
        var text = string.Join(" ", cue.Lines);
        var stripped = new string(text.Where(ch => !char.IsWhiteSpace(ch)).ToArray());
        return stripped.Length;
    }
}
