using System.Collections.Generic;
using System.Text;

namespace AdminMenu.Localization;

/// <summary>
/// Makes right-to-left text drawable by TextMeshPro, which neither joins Arabic letters nor
/// lays a line out right to left.
///
/// The game gets away with flipping its own labels with <c>isRightToLeftText</c>, which only
/// reverses the characters; that would also reverse a player's Latin name or a number, and
/// the menu's lines mix all three. So the text is turned into the order it is drawn in
/// instead: each letter becomes the presentation form for its place in the word, then runs
/// of Arabic are reversed while runs of Latin text and digits keep their order. Text with no
/// Arabic in it comes back unchanged.
/// </summary>
internal static class ArabicText
{
    /// <summary>True when the game is in a right-to-left language.</summary>
    internal static bool IsActive
    {
        get
        {
            LocaleAsset locale = Strings.ActiveLocaleAsset();
            return locale != null && locale.RightToLeft;
        }
    }

    /// <summary>The text in the order TextMeshPro should draw it, left to right.</summary>
    internal static string ToVisual(string text)
    {
        if (string.IsNullOrEmpty(text) || !ContainsRightToLeft(text))
        {
            return text;
        }

        return Reorder(Shape(text));
    }

    private static bool ContainsRightToLeft(string text)
    {
        foreach (char c in text)
        {
            if (IsRightToLeft(c))
            {
                return true;
            }
        }

        return false;
    }

    // ---- Letter shaping ----

    /// <summary>Joins each letter to its neighbours by picking its presentation form.</summary>
    private static string Shape(string text)
    {
        StringBuilder builder = new StringBuilder(text.Length);

        for (int i = 0; i < text.Length; i++)
        {
            char current = text[i];

            if (!Forms.TryGetValue(current, out char[] forms))
            {
                builder.Append(current);
                continue;
            }

            bool joinsPrevious = JoinsToFollowing(Neighbour(text, i, -1));
            int next = NeighbourIndex(text, i, 1);

            // Lam followed by an alef is one glyph, not two.
            if (current == Lam && next >= 0 && LamAlef.TryGetValue(text[next], out char[] ligature))
            {
                builder.Append(joinsPrevious ? ligature[1] : ligature[0]);
                for (int skipped = i + 1; skipped < next; skipped++)
                {
                    builder.Append(text[skipped]);
                }

                i = next;
                continue;
            }

            bool dual = forms.Length == 4;
            bool joinsNext = dual && next >= 0 && JoinsToPrevious(text[next]);

            if (forms.Length == 1)
            {
                builder.Append(forms[0]);
            }
            else if (joinsPrevious && joinsNext)
            {
                builder.Append(forms[3]);
            }
            else if (joinsPrevious)
            {
                builder.Append(forms[1]);
            }
            else if (joinsNext)
            {
                builder.Append(forms[2]);
            }
            else
            {
                builder.Append(forms[0]);
            }
        }

        return builder.ToString();
    }

    /// <summary>Whether a letter reaches out to the letter after it.</summary>
    private static bool JoinsToFollowing(char c)
    {
        return c == Tatweel || Forms.TryGetValue(c, out char[] forms) && forms.Length == 4;
    }

    /// <summary>Whether a letter accepts a join from the letter before it.</summary>
    private static bool JoinsToPrevious(char c)
    {
        return c == Tatweel || Forms.TryGetValue(c, out char[] forms) && forms.Length > 1;
    }

    private static char Neighbour(string text, int index, int step)
    {
        int found = NeighbourIndex(text, index, step);
        return found < 0 ? '\0' : text[found];
    }

    /// <summary>The nearest character in one direction, skipping vowel marks.</summary>
    private static int NeighbourIndex(string text, int index, int step)
    {
        for (int i = index + step; i >= 0 && i < text.Length; i += step)
        {
            if (!IsMark(text[i]))
            {
                return i;
            }
        }

        return -1;
    }

    private static bool IsMark(char c)
    {
        return c >= 'ً' && c <= 'ٟ' || c == 'ٰ';
    }

    // ---- Line order ----

    private enum Direction
    {
        Neutral,
        LeftToRight,
        RightToLeft,
    }

    /// <summary>
    /// A cut-down bidirectional layout for a single right-to-left line: neutral characters
    /// between two runs of one direction take that direction, any others go with the line.
    /// </summary>
    private static string Reorder(string text)
    {
        Direction[] directions = new Direction[text.Length];
        for (int i = 0; i < text.Length; i++)
        {
            directions[i] = Classify(text[i]);
        }

        for (int i = 0; i < text.Length; i++)
        {
            if (directions[i] != Direction.Neutral)
            {
                continue;
            }

            int end = i;
            while (end < text.Length && directions[end] == Direction.Neutral)
            {
                end++;
            }

            Direction before = i > 0 ? directions[i - 1] : Direction.RightToLeft;
            Direction after = end < text.Length ? directions[end] : Direction.RightToLeft;
            Direction resolved = before == after ? before : Direction.RightToLeft;

            for (int j = i; j < end; j++)
            {
                directions[j] = resolved;
            }

            i = end - 1;
        }

        List<string> runs = new List<string>();
        int start = 0;
        for (int i = 1; i <= text.Length; i++)
        {
            if (i < text.Length && directions[i] == directions[start])
            {
                continue;
            }

            string run = text.Substring(start, i - start);
            runs.Add(directions[start] == Direction.RightToLeft ? ReverseRun(run) : run);
            start = i;
        }

        runs.Reverse();
        return string.Concat(runs);
    }

    /// <summary>Reverses a right-to-left run, keeping each vowel mark after its letter.</summary>
    private static string ReverseRun(string run)
    {
        List<string> clusters = new List<string>();
        StringBuilder cluster = new StringBuilder();

        foreach (char c in run)
        {
            if (IsMark(c) && cluster.Length > 0)
            {
                cluster.Append(c);
                continue;
            }

            if (cluster.Length > 0)
            {
                clusters.Add(cluster.ToString());
                cluster.Clear();
            }

            cluster.Append(Mirror(c));
        }

        if (cluster.Length > 0)
        {
            clusters.Add(cluster.ToString());
        }

        clusters.Reverse();
        return string.Concat(clusters);
    }

    private static char Mirror(char c)
    {
        switch (c)
        {
            case '(': return ')';
            case ')': return '(';
            case '[': return ']';
            case ']': return '[';
            case '<': return '>';
            case '>': return '<';
            default: return c;
        }
    }

    private static Direction Classify(char c)
    {
        if (IsRightToLeft(c))
        {
            return Direction.RightToLeft;
        }

        return char.IsLetterOrDigit(c) ? Direction.LeftToRight : Direction.Neutral;
    }

    private static bool IsRightToLeft(char c)
    {
        return c >= '֐' && c <= 'ࣿ' ||
               c >= 'יִ' && c <= '﷿' ||
               c >= 'ﹰ' && c <= '﻿';
    }

    // ---- Tables ----

    private const char Lam = 'ل';
    private const char Tatweel = 'ـ';

    /// <summary>Isolated and final form of lam joined to each kind of alef.</summary>
    private static readonly Dictionary<char, char[]> LamAlef = new Dictionary<char, char[]>
    {
        ['آ'] = new[] { 'ﻵ', 'ﻶ' },
        ['أ'] = new[] { 'ﻷ', 'ﻸ' },
        ['إ'] = new[] { 'ﻹ', 'ﻺ' },
        ['ا'] = new[] { 'ﻻ', 'ﻼ' },
    };

    /// <summary>
    /// Presentation forms per letter: isolated, final, initial, medial. Letters that only join
    /// to the letter before them have two, hamza has one.
    /// </summary>
    private static readonly Dictionary<char, char[]> Forms = new Dictionary<char, char[]>
    {
        ['ء'] = new[] { 'ﺀ' },
        ['آ'] = new[] { 'ﺁ', 'ﺂ' },
        ['أ'] = new[] { 'ﺃ', 'ﺄ' },
        ['ؤ'] = new[] { 'ﺅ', 'ﺆ' },
        ['إ'] = new[] { 'ﺇ', 'ﺈ' },
        ['ئ'] = new[] { 'ﺉ', 'ﺊ', 'ﺋ', 'ﺌ' },
        ['ا'] = new[] { 'ﺍ', 'ﺎ' },
        ['ب'] = new[] { 'ﺏ', 'ﺐ', 'ﺑ', 'ﺒ' },
        ['ة'] = new[] { 'ﺓ', 'ﺔ' },
        ['ت'] = new[] { 'ﺕ', 'ﺖ', 'ﺗ', 'ﺘ' },
        ['ث'] = new[] { 'ﺙ', 'ﺚ', 'ﺛ', 'ﺜ' },
        ['ج'] = new[] { 'ﺝ', 'ﺞ', 'ﺟ', 'ﺠ' },
        ['ح'] = new[] { 'ﺡ', 'ﺢ', 'ﺣ', 'ﺤ' },
        ['خ'] = new[] { 'ﺥ', 'ﺦ', 'ﺧ', 'ﺨ' },
        ['د'] = new[] { 'ﺩ', 'ﺪ' },
        ['ذ'] = new[] { 'ﺫ', 'ﺬ' },
        ['ر'] = new[] { 'ﺭ', 'ﺮ' },
        ['ز'] = new[] { 'ﺯ', 'ﺰ' },
        ['س'] = new[] { 'ﺱ', 'ﺲ', 'ﺳ', 'ﺴ' },
        ['ش'] = new[] { 'ﺵ', 'ﺶ', 'ﺷ', 'ﺸ' },
        ['ص'] = new[] { 'ﺹ', 'ﺺ', 'ﺻ', 'ﺼ' },
        ['ض'] = new[] { 'ﺽ', 'ﺾ', 'ﺿ', 'ﻀ' },
        ['ط'] = new[] { 'ﻁ', 'ﻂ', 'ﻃ', 'ﻄ' },
        ['ظ'] = new[] { 'ﻅ', 'ﻆ', 'ﻇ', 'ﻈ' },
        ['ع'] = new[] { 'ﻉ', 'ﻊ', 'ﻋ', 'ﻌ' },
        ['غ'] = new[] { 'ﻍ', 'ﻎ', 'ﻏ', 'ﻐ' },
        ['ف'] = new[] { 'ﻑ', 'ﻒ', 'ﻓ', 'ﻔ' },
        ['ق'] = new[] { 'ﻕ', 'ﻖ', 'ﻗ', 'ﻘ' },
        ['ك'] = new[] { 'ﻙ', 'ﻚ', 'ﻛ', 'ﻜ' },
        ['ل'] = new[] { 'ﻝ', 'ﻞ', 'ﻟ', 'ﻠ' },
        ['م'] = new[] { 'ﻡ', 'ﻢ', 'ﻣ', 'ﻤ' },
        ['ن'] = new[] { 'ﻥ', 'ﻦ', 'ﻧ', 'ﻨ' },
        ['ه'] = new[] { 'ﻩ', 'ﻪ', 'ﻫ', 'ﻬ' },
        ['و'] = new[] { 'ﻭ', 'ﻮ' },
        ['ى'] = new[] { 'ﻯ', 'ﻰ' },
        ['ي'] = new[] { 'ﻱ', 'ﻲ', 'ﻳ', 'ﻴ' },
    };
}
