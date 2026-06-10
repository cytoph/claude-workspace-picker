using System.Globalization;
using ClaudeWorkspacePicker.Helpers;
using ClaudeWorkspacePicker.Models;
using Spectre.Tui;
using Spectre.Tui.App;
using Color = Spectre.Console.Color;
using Decoration = Spectre.Console.Decoration;
using Style = Spectre.Console.Style;

namespace ClaudeWorkspacePicker.Ui;

sealed class MainScreen : AppScreen
{
    private const int ListItemPadding = 2;

    private readonly string[] _paddedLabels;

    private readonly ListWidget<DirectoryListItem> _listWidget;

    private readonly int _customPathIndex;
    private readonly int _customPathPrefixWidth;
    private readonly ClearWidget _customPathBackground;
    private readonly TextBoxWidget _customPathTextBox;

    private readonly string? _globalArguments;

    private string _pathError = "";

    public LaunchTarget? SelectedEntry { get; private set; }

    public MainScreen(AppState appState) : base(appState.ScreenTheme, appState.SettingsPath)
    {
        _globalArguments = appState.GlobalArguments;

        ListItemTheme listItemTheme = appState.ListItemTheme;
        List<MenuEntry> entries = appState.Entries;

        string labelSeparator = new(' ', ListItemPadding);

        int maxIconWidth = entries.Max(e => UnicodeHelper.GetDisplayWidth(e.Icon));

        _paddedLabels = [.. entries.Select(e =>
        {
            int pad = maxIconWidth - UnicodeHelper.GetDisplayWidth(e.Icon);
            return $"{e.Icon}{new string(' ', pad)}{labelSeparator}{e.Name}";
        })];

        List<DirectoryListItem> items = [.. entries.Select((e, i) => new DirectoryListItem(e, listItemTheme, _paddedLabels[i]))];

        _listWidget = new ListWidget<DirectoryListItem>(items).WrapAround().SelectedIndex(0);

        _customPathIndex = entries.Count - 1;
        _customPathPrefixWidth = maxIconWidth + ListItemPadding;
        _customPathBackground = new ClearWidget(style: listItemTheme.SelectedStyle);
        _customPathTextBox = BuildCustomPathTextBox(listItemTheme);
    }

    protected override IWidget BuildInnerWidget() => _listWidget;

    protected override IReadOnlyList<string> GetContentLines() => _paddedLabels;

    protected override void OnKeyMessage(ApplicationContext context, KeyMessage key)
    {
        bool customSelected = _listWidget.SelectedItem?.Entry.IsCustomPath == true;

        switch (key.Key)
        {
            case Key.Enter:
                if (customSelected)
                    ConfirmCustomPath(context);
                else
                    HandleSelection(context);
                break;

            // Workaround for https://github.com/spectreconsole/spectre.tui/pull/29 -
            // TextBoxWidget doesn't pin _horizontalOffset when the cursor retreats from
            // the right edge on deletion. Fix: reset offset by Clear()+Text+MoveToEnd().
            // Remove this case once the PR is merged and the package is updated.
            case Key.Backspace when customSelected && _customPathTextBox.Cursor.Column == _customPathTextBox.Length:
                DeleteLastGraphemeFromPathBox();
                _pathError = "";
                break;

            default:
                if (customSelected && key.Key is not (Key.Up or Key.Down))
                    _customPathTextBox.KeyMap.HandleKey(key);
                else
                    _listWidget.KeyMap.HandleKey(key);
                _pathError = "";
                break;
        }
    }

    protected override void RenderOverlays(RenderContext context, Rectangle boxArea)
    {
        if (_listWidget.SelectedItem?.Entry.IsCustomPath != true)
            return;

        int itemX = boxArea.X + 3;                         // 1 border + 2 padding
        int itemY = boxArea.Y + 2 + _customPathIndex;      // 1 border + 1 padding + item offset
        int textBoxX = itemX + _customPathPrefixWidth;
        int textBoxWidth = boxArea.Width - (6 + _customPathPrefixWidth); // 6 (borders+padding) + prefix

        Rectangle textBoxArea = new(textBoxX, itemY, textBoxWidth, 1);

        context.Render(_customPathBackground, textBoxArea);
        context.Render(_customPathTextBox, textBoxArea);

        if (_pathError.Length > 0)
        {
            Paragraph errorParagraph = Paragraph.FromString(_pathError, new Style(foreground: Color.Red));
            Rectangle errorArea = new(boxArea.X, boxArea.Y + boxArea.Height, boxArea.Width, 1);
            context.Render(errorParagraph, errorArea);
        }
    }

    private void HandleSelection(ApplicationContext context)
    {
        DirectoryListItem? item = _listWidget.SelectedItem;

        if (item is null)
            return;

        if (item.Entry.ResolvedPath is not { } path)
            return;

        SelectedEntry = new LaunchTarget(path, item.Entry.OverrideArguments ?? _globalArguments);
        context.Quit();
    }

    private void ConfirmCustomPath(ApplicationContext context)
    {
        string path = Environment.ExpandEnvironmentVariables(_customPathTextBox.Text).Trim();

        if (!Directory.Exists(path))
        {
            _pathError = "Directory does not exist.";
            return;
        }

        string? overrideArguments = _listWidget.SelectedItem?.Entry.OverrideArguments;

        SelectedEntry = new LaunchTarget(path, overrideArguments ?? _globalArguments);
        context.Quit();
    }

    private void DeleteLastGraphemeFromPathBox()
    {
        string current = _customPathTextBox.Text;

        if (current.Length == 0)
            return;

        List<string> graphemes = [];
        TextElementEnumerator en = StringInfo.GetTextElementEnumerator(current);

        while (en.MoveNext())
            graphemes.Add(en.GetTextElement());

        string newText = string.Concat(graphemes.Take(graphemes.Count - 1));
        _customPathTextBox.Clear();
        _customPathTextBox.Text = newText;
        _customPathTextBox.MoveToEnd();
    }

    private static TextBoxWidget BuildCustomPathTextBox(ListItemTheme listItemTheme)
    {
        Style selectedStyle = listItemTheme.SelectedStyle;
        Style hintStyle = selectedStyle with { Decoration = selectedStyle.Decoration | Decoration.Dim };

        TextBoxWidget box = new TextBoxWidget()
            .AsSingleLine()
            .Placeholder("e.g. C:\\Projects\\myapp or %USERPROFILE%")
            .PlaceholderStyle(hintStyle)
            .Style(selectedStyle);

        box.IsFocused = true;

        return box;
    }

    private sealed class DirectoryListItem : IListWidgetItem
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

        public Text CreateText(bool isSelected) =>
            Text.FromString(_paddedLabel, isSelected ? _theme.SelectedStyle : _theme.NormalStyle);
    }
}
