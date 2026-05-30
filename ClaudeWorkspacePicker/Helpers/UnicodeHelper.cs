using System.Globalization;

namespace ClaudeWorkspacePicker.Helpers;

static class UnicodeHelper
{
    public static int GetDisplayWidth(string text, int padding = 0)
    {
        int total = 0;
        TextElementEnumerator en = StringInfo.GetTextElementEnumerator(text);

        while (en.MoveNext())
            total += GetElementDisplayWidth(en.GetTextElement());

        return total + padding;
    }

    private static int GetElementDisplayWidth(string element)
    {
        if (element.Length == 0)
            return 0;

        if (element.Contains('️'))
            return 2;

        int cp = element.Length >= 2 && char.IsHighSurrogate(element[0])
            ? char.ConvertToUtf32(element[0], element[1])
            : element[0];

        return IsWideCodePoint(cp) ? 2 : 1;
    }

    private static bool IsWideCodePoint(int cp) =>
        (cp >= 0x1100 && cp <= 0x115F) ||
        (cp >= 0x2329 && cp <= 0x232A) ||
        (cp >= 0x2600 && cp <= 0x27BF) ||  // Misc Symbols + Dingbats (✅ ❌ and common emoji)
        (cp >= 0x2E80 && cp <= 0x303E) ||
        (cp >= 0x3041 && cp <= 0x33FF) ||
        (cp >= 0x3400 && cp <= 0x4DBF) ||
        (cp >= 0x4E00 && cp <= 0xA4CF) ||
        (cp >= 0xA960 && cp <= 0xA97F) ||
        (cp >= 0xAC00 && cp <= 0xD7FF) ||
        (cp >= 0xF900 && cp <= 0xFAFF) ||
        (cp >= 0xFE10 && cp <= 0xFE1F) ||
        (cp >= 0xFE30 && cp <= 0xFE6F) ||
        (cp >= 0xFF01 && cp <= 0xFF60) ||
        (cp >= 0xFFE0 && cp <= 0xFFE6) ||
        (cp >= 0x1B000 && cp <= 0x1B001) ||
        cp == 0x1F004 || cp == 0x1F0CF ||
        (cp >= 0x1F200 && cp <= 0x1F251) ||
        (cp >= 0x1F300 && cp <= 0x1F6FF) ||
        (cp >= 0x1F7E0 && cp <= 0x1F7FF) ||
        (cp >= 0x1F900 && cp <= 0x1FAFF) ||
        (cp >= 0x20000 && cp <= 0x2FFFD) ||
        (cp >= 0x30000 && cp <= 0x3FFFD);
}
