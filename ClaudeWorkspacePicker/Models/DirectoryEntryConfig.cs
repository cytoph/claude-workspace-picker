using System.Text.Json.Serialization;

namespace ClaudeWorkspacePicker.Models;

sealed record DirectoryEntryConfig(
    string Path,
    string Icon,
    string? DisplayName,
    [property: JsonPropertyName("overrideArgs")] string? OverrideArguments
);
