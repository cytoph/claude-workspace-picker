using System.Text.Json.Serialization;

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
    [property: JsonPropertyName("globalArgs")] string? GlobalArguments,
    List<DirectoryEntryConfig>? Directories
);
