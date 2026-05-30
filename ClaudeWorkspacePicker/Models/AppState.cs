namespace ClaudeWorkspacePicker.Models;

sealed record AppState(ScreenTheme ScreenTheme, ListItemTheme ListItemTheme, List<MenuEntry> Entries);
