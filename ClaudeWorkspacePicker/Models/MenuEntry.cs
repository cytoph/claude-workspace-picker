namespace ClaudeWorkspacePicker.Models;

sealed record MenuEntry(string Icon, string Name, string? ResolvedPath, bool IsCustomPath = false, string? OverrideArguments = null)
{
    public static MenuEntry CustomPathEntry { get; } = new("📂", "Enter custom path...", null, IsCustomPath: true);
}
