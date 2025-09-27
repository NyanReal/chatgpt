using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using SubtitleToolkit.Models;

namespace SubtitleToolkit.Services;

public class SubRipParser
{
    private static readonly Regex TimeSpanRegex = new(
        "^(?<start>\\d{2}:\\d{2}:\\d{2},\\d{3}) --> (?<end>\\d{2}:\\d{2}:\\d{2},\\d{3})$",
        RegexOptions.Compiled);

    public SubRipDocument Parse(string contents)
    {
        var lines = contents.Replace("\r", string.Empty).Split('\n');
        var cues = new List<SubRipCue>();
        int i = 0;
        while (i < lines.Length)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line))
            {
                i++;
                continue;
            }

            if (!int.TryParse(line, NumberStyles.Integer, CultureInfo.InvariantCulture, out int index))
            {
                // Skip malformed block
                i++;
                continue;
            }

            i++;
            if (i >= lines.Length)
            {
                break;
            }

            var timeLine = lines[i].Trim();
            i++;
            var match = TimeSpanRegex.Match(timeLine);
            if (!match.Success)
            {
                continue;
            }

            var start = ParseTimestamp(match.Groups["start"].Value);
            var end = ParseTimestamp(match.Groups["end"].Value);
            var textLines = new List<string>();
            while (i < lines.Length && !string.IsNullOrWhiteSpace(lines[i]))
            {
                textLines.Add(lines[i]);
                i++;
            }

            // Skip blank separators
            while (i < lines.Length && string.IsNullOrWhiteSpace(lines[i]))
            {
                i++;
            }

            cues.Add(new SubRipCue
            {
                Index = index,
                Start = start,
                End = end,
                Lines = textLines
            });
        }

        // Renumber sequentially for safety
        for (int idx = 0; idx < cues.Count; idx++)
        {
            cues[idx].Index = idx + 1;
        }

        return new SubRipDocument { Cues = cues };
    }

    public string Serialize(SubRipDocument document)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < document.Cues.Count; i++)
        {
            var cue = document.Cues[i];
            sb.AppendLine((i + 1).ToString(CultureInfo.InvariantCulture));
            sb.AppendLine(
                $"{FormatTimestamp(cue.Start)} --> {FormatTimestamp(cue.End)}");
            foreach (var line in cue.Lines)
            {
                sb.AppendLine(line);
            }

            if (i < document.Cues.Count - 1)
            {
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    private static TimeSpan ParseTimestamp(string value)
    {
        var span = TimeSpan.ParseExact(value, "hh\\:mm\\:ss\\,fff", CultureInfo.InvariantCulture);
        return span < TimeSpan.Zero ? TimeSpan.Zero : span;
    }

    private static string FormatTimestamp(TimeSpan span)
    {
        if (span < TimeSpan.Zero)
        {
            span = TimeSpan.Zero;
        }

        return span.ToString("hh\\:mm\\:ss\\,fff", CultureInfo.InvariantCulture);
    }
}
