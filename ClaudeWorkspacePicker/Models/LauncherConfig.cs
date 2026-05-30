namespace ClaudeWorkspacePicker.Models;

sealed record LauncherConfig(
    string? TitleIcon,
    string? TitleText,
    string? TitleForeground,
    string? BoxColor,
    string? HintForeground,
    string? Background,
    string? Foreground,
    string? SelectedBackground,
    string? SelectedForeground,
    string? SelectedTextStyle,
    List<DirectoryEntryConfig>? Directories
);
