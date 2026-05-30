using Style = Spectre.Console.Style;

namespace ClaudeWorkspacePicker.Models;

sealed record ListItemTheme(
    Style NormalStyle,
    Style SelectedStyle
);
