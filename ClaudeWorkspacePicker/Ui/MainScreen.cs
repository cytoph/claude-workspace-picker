using System.Diagnostics;
using System.Globalization;
using ClaudeWorkspacePicker.Helpers;
using ClaudeWorkspacePicker.Models;
using Spectre.Tui;
using Spectre.Tui.App;
using Color = Spectre.Console.Color;
using Style = Spectre.Console.Style;

namespace ClaudeWorkspacePicker.Ui;

sealed class MainScreen : Screen
{
    private const string HintsText = "⚙️ Ctrl+E";

    private const int ListItemPadding = 2;
    private const int BoxLabelPadding = 3;

    private readonly ClearWidget _screenBackground;

    private readonly ListWidget<DirectoryListItem> _list;

    private readonly int _boxWidth;
    private readonly int _boxHeight;
    private readonly BoxWidget _box;

    private readonly int _customPathIndex;
    private readonly int _customPathPrefixWidth;
    private readonly ClearWidget _customPathBackground;
    private readonly TextBoxWidget _customPathTextBox;

    private readonly string? _globalArguments;
    private readonly string _settingsPath;

    private string _pathError = "";

    public LaunchTarget? SelectedEntry { get; private set; }

    public MainScreen(AppState appState)
    {
        _globalArguments = appState.GlobalArguments;
        _settingsPath = appState.SettingsPath;

        ScreenTheme screenTheme = appState.ScreenTheme;
        ListItemTheme listItemTheme = appState.ListItemTheme;
        List<MenuEntry> entries = appState.Entries;

        string labelSeparator = new(' ', ListItemPadding);

        int maxIconWidth = entries.Max(e => UnicodeHelper.GetDisplayWidth(e.Icon));

        string[] paddedLabels = [.. entries.Select(e =>
        {
            int pad = maxIconWidth - UnicodeHelper.GetDisplayWidth(e.Icon);
            return $"{e.Icon}{new string(' ', pad)}{labelSeparator}{e.Name}";
        })];

        string titleText = $"{screenTheme.TitleIcon}{labelSeparator}{screenTheme.TitleText}";

        List<DirectoryListItem> items = [.. entries.Select((e, i) => new DirectoryListItem(e, listItemTheme, paddedLabels[i]))];

        _customPathIndex = entries.Count - 1;
        _customPathPrefixWidth = maxIconWidth + 2;

        (_boxWidth, _boxHeight) = CalculateBoxSize(paddedLabels, titleText, HintsText);

        _screenBackground = BuildClearWidget(screenTheme);

        _list = BuildListWidget(items);
        _box = BuildBoxWidget(screenTheme, titleText, HintsText, _list);

        _customPathBackground = BuildCustomPathBackground(listItemTheme);
        _customPathTextBox = BuildCustomPathTextBox(listItemTheme);
    }

    public override void OnMessage(ApplicationContext context, ApplicationMessage message)
    {
        if (message is not KeyMessage key)
            return;

        if (key.Modifiers.HasFlag(KeyModifier.Ctrl) && key.Character is 'e' or 'E' or '\x05')
        {
            OpenSettingsFile();
            return;
        }

        bool customSelected = _list.SelectedItem?.Entry.IsCustomPath == true;

        switch (key.Key)
        {
            case Key.Enter:
                if (customSelected)
                    ConfirmCustomPath(context);
                else
                    HandleSelection(context);
                break;

            case Key.Escape:
                context.Quit();
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
                {
                    _customPathTextBox.KeyMap.HandleKey(key);
                }
                else
                {
                    _list.KeyMap.HandleKey(key);
                }
                _pathError = "";
                break;
        }
    }

    private void OpenSettingsFile()
    {
        Process.Start(new ProcessStartInfo(_settingsPath) { UseShellExecute = true });
    }

    private void HandleSelection(ApplicationContext context)
    {
        DirectoryListItem? item = _list.SelectedItem;

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

        string? overrideArguments = _list.SelectedItem?.Entry.OverrideArguments;

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

    public override void Render(RenderContext context)
    {
        context.Render(_screenBackground);
        Rectangle boxArea = context.Screen.Center(new Size(_boxWidth, _boxHeight));
        context.Render(_box, boxArea);

        if (_list.SelectedItem?.Entry.IsCustomPath == true)
        {
            int itemX = boxArea.X + 3;                         // 1 border + 2 padding
            int itemY = boxArea.Y + 2 + _customPathIndex;      // 1 border + 1 padding + item offset
            int textBoxX = itemX + _customPathPrefixWidth;
            int textBoxWidth = _boxWidth - (6 + _customPathPrefixWidth); // 6 (borders+padding) + prefix

            Rectangle textBoxArea = new(textBoxX, itemY, textBoxWidth, 1);

            context.Render(_customPathBackground, textBoxArea);
            context.Render(_customPathTextBox, textBoxArea);

            if (_pathError.Length > 0)
            {
                Paragraph errorParagraph = Paragraph.FromString(_pathError, new Style(foreground: Color.Red));
                Rectangle errorArea = new(boxArea.X, boxArea.Y + boxArea.Height, _boxWidth, 1);

                context.Render(errorParagraph, errorArea);
            }
        }
    }

    private static ClearWidget BuildClearWidget(ScreenTheme screenTheme) => new(style: screenTheme.ScreenStyle);

    private static ListWidget<DirectoryListItem> BuildListWidget(List<DirectoryListItem> items) => new ListWidget<DirectoryListItem>(items).WrapAround().SelectedIndex(0);

    private static BoxWidget BuildBoxWidget(ScreenTheme theme, string titleText, string hintsText, ListWidget<DirectoryListItem> list)
    {
        TextLine titleLine = TextLine.FromString($" {titleText} ", theme.TitleStyle);
        TextLine hintsLine = TextLine.FromString($" {hintsText} ", theme.HintStyle);

        return new BoxWidget()
            .Border(Border.Rounded)
            .Style(theme.BoxStyle)
            .TitlePadding(BoxLabelPadding)
            .Title(titleLine)
            .Title(hintsLine, TitlePosition.Bottom, Justify.Right)
            .Inner(new PaddingWidget(new Padding(2, 1), list));
    }

    private static ClearWidget BuildCustomPathBackground(ListItemTheme listItemTheme) => new(style: listItemTheme.SelectedStyle);

    private static TextBoxWidget BuildCustomPathTextBox(ListItemTheme listItemTheme)
    {
        TextBoxWidget box = new TextBoxWidget()
            .AsSingleLine()
            .Placeholder("e.g. C:\\Projects\\myapp or %USERPROFILE%")
            .Style(listItemTheme.SelectedStyle);

        box.IsFocused = true;

        return box;
    }

    private static (int Width, int Height) CalculateBoxSize(string[] labels, string titleText, string hintsText)
    {
        int width = labels
            .Select(label => UnicodeHelper.GetDisplayWidth(label, padding: 6))                         // 2 borders + 4 PaddingWidget (left + right)
            .Append(UnicodeHelper.GetDisplayWidth(titleText, padding: 4 + 2 * BoxLabelPadding))        // 2 borders + 2 spaces (around) + BoxLabelPadding on each side
            .Append(UnicodeHelper.GetDisplayWidth(hintsText, padding: 4 + 2 * BoxLabelPadding))        // 2 borders + 2 spaces (around) + BoxLabelPadding on each side
            .Max();

        int height = labels.Length + 4; // 2 borders + 2 PaddingWidget (top + bottom)

        return (width, height);
    }
}
