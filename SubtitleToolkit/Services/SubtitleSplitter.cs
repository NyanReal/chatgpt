using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using SubtitleToolkit.Models;

namespace SubtitleToolkit.Services;

public class SubtitleSplitter
{
    private static readonly Regex SentencePattern = new(
        "[^.!?！？。]*[.!?！？。]+|[^.!?！？。]+",
        RegexOptions.Compiled);

    public (SubRipDocument Document, List<string> Summary) SplitDocument(SubRipDocument document, int maxCharactersPerCue, int wordSplitLimit)
    {
        if (maxCharactersPerCue <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxCharactersPerCue));
        }

        if (wordSplitLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(wordSplitLimit));
        }

        var result = new SubRipDocument();
        var summary = new List<string>();

        foreach (var cue in document.Cues)
        {
            var segments = SplitCueIntoSegments(cue, maxCharactersPerCue, wordSplitLimit);
            if (segments.Count > 1)
            {
                summary.Add($"#{cue.Index} → {segments.Count}개 분할");
            }

            result.Cues.AddRange(DistributeTiming(cue, segments));
        }

        for (int i = 0; i < result.Cues.Count; i++)
        {
            result.Cues[i].Index = i + 1;
        }

        return (result, summary);
    }

    private static List<string> SplitCueIntoSegments(SubRipCue cue, int maxCharactersPerCue, int wordSplitLimit)
    {
        var text = string.Join(" ", cue.Lines).Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return new List<string> { string.Empty };
        }

        var strippedLength = CountCharacters(text);
        var sentences = SentencePattern
            .Matches(text)
            .Select(m => CleanSpaces(m.Value))
            .Where(s => !string.IsNullOrEmpty(s))
            .ToList();

        if (strippedLength <= maxCharactersPerCue && sentences.Count <= 1)
        {
            return new List<string> { text };
        }

        var segments = new List<string>();
        if (sentences.Count > 1)
        {
            foreach (var sentence in sentences)
            {
                if (CountCharacters(sentence) > maxCharactersPerCue)
                {
                    segments.AddRange(SplitByWords(sentence, wordSplitLimit));
                }
                else
                {
                    segments.Add(sentence);
                }
            }
        }
        else
        {
            segments.AddRange(SplitByWords(text, maxCharactersPerCue));
        }

        // Ensure no segment exceeds the hard limit by further splitting if necessary.
        var refined = new List<string>();
        foreach (var segment in segments)
        {
            if (CountCharacters(segment) > maxCharactersPerCue)
            {
                refined.AddRange(SplitByWords(segment, wordSplitLimit));
            }
            else
            {
                refined.Add(segment);
            }
        }

        return refined.Count > 0 ? refined : new List<string> { text };
    }

    private static IEnumerable<SubRipCue> DistributeTiming(SubRipCue source, IReadOnlyList<string> segments)
    {
        if (segments.Count == 0)
        {
            yield break;
        }

        if (segments.Count == 1)
        {
            yield return new SubRipCue
            {
                Index = source.Index,
                Start = source.Start,
                End = source.End,
                Lines = new List<string> { segments[0] }
            };
            yield break;
        }

        var totalDuration = source.End - source.Start;
        if (totalDuration < TimeSpan.Zero)
        {
            totalDuration = TimeSpan.Zero;
        }

        var charCounts = segments.Select(CountCharacters).Select(count => Math.Max(1, count)).ToList();
        var totalChars = charCounts.Sum();
        if (totalChars == 0)
        {
            totalChars = segments.Count;
            charCounts = Enumerable.Repeat(1, segments.Count).ToList();
        }

        var currentStart = source.Start;
        double accumulated = 0;
        for (int i = 0; i < segments.Count; i++)
        {
            var text = segments[i];
            accumulated += charCounts[i];
            TimeSpan segmentEnd;
            if (i == segments.Count - 1 || totalDuration == TimeSpan.Zero)
            {
                segmentEnd = source.End;
            }
            else
            {
                var ratio = accumulated / totalChars;
                var target = source.Start + TimeSpan.FromMilliseconds(totalDuration.TotalMilliseconds * ratio);
                if (target <= currentStart)
                {
                    target = currentStart + TimeSpan.FromMilliseconds(1);
                }

                if (target > source.End)
                {
                    target = source.End;
                }

                segmentEnd = target;
            }

            yield return new SubRipCue
            {
                Index = source.Index,
                Start = currentStart,
                End = segmentEnd,
                Lines = new List<string> { text }
            };

            currentStart = segmentEnd;
        }
    }

    private static IEnumerable<string> SplitByWords(string text, int maxCharacters)
    {
        var words = Regex.Matches(text, "\\S+").Select(m => m.Value).ToList();
        if (words.Count == 0)
        {
            return new[] { text };
        }

        var segments = new List<string>();
        var buffer = new List<string>();
        foreach (var word in words)
        {
            if (buffer.Count == 0)
            {
                buffer.Add(word);
                continue;
            }

            var candidate = string.Join(" ", buffer.Append(word));
            if (CountCharacters(candidate) > maxCharacters && buffer.Count > 0)
            {
                segments.Add(string.Join(" ", buffer));
                buffer = new List<string> { word };
            }
            else
            {
                buffer.Add(word);
            }
        }

        if (buffer.Count > 0)
        {
            segments.Add(string.Join(" ", buffer));
        }

        return segments;
    }

    private static string CleanSpaces(string value)
    {
        return Regex.Replace(value, "\\s+", " ").Trim();
    }

    private static int CountCharacters(string value)
    {
        return value.Count(ch => !char.IsWhiteSpace(ch));
    }
}
