using Style = Spectre.Console.Style;

namespace ClaudeWorkspacePicker.Models;

sealed record ScreenTheme(
    string TitleIcon,
    string TitleText,
    Style TitleStyle,    // fg: titleFg, bg: background, decoration: Bold
    Style BoxStyle,      // fg: boxColor, bg: background
    Style HintStyle,     // fg: hintFg, bg: background
    Style ScreenStyle    // bg: background only — for ClearWidget
);
