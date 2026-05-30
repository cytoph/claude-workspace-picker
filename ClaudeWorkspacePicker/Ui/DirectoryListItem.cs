using ClaudeWorkspacePicker.Models;
using Spectre.Tui;

namespace ClaudeWorkspacePicker.Ui;

sealed class DirectoryListItem : IListWidgetItem
{
    public MenuEntry Entry { get; }
    private readonly ListItemTheme _theme;
    private readonly string _paddedLabel;

    public DirectoryListItem(MenuEntry entry, ListItemTheme theme, string paddedLabel)
    {
        Entry = entry;
        _theme = theme;
        _paddedLabel = paddedLabel;
    }

    public Text CreateText(bool isSelected) => Text.FromString(_paddedLabel, isSelected ? _theme.SelectedStyle : _theme.NormalStyle);
}
