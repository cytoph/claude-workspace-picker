using ClaudeWorkspacePicker.Helpers;
using ClaudeWorkspacePicker.Models;
using Spectre.Tui;
using Spectre.Tui.App;
using System.Diagnostics;
using Style = Spectre.Console.Style;

namespace ClaudeWorkspacePicker.Ui;

abstract class AppScreen : Screen
{
    protected const int BoxLabelPadding = 3;
    protected const string HintText = "⚙️ Ctrl+E";

    private readonly ScreenTheme _theme;
    private readonly string _settingsPath;

    private int _boxWidth;
    private int _boxHeight;
    private BoxWidget _box = null!;

    protected AppScreen(ScreenTheme theme, string settingsPath)
    {
        _theme = theme;
        _settingsPath = settingsPath;
    }

    protected abstract IReadOnlyList<string> GetContentLines();
    protected abstract IWidget BuildInnerWidget();

    protected string TitleText => $"{_theme.TitleIcon}  {_theme.TitleText}";

    public override void OnEnter(ApplicationContext context)
    {
        IReadOnlyList<string> contentLines = GetContentLines();

        _boxWidth = contentLines
            .Select(line => UnicodeHelper.GetDisplayWidth(line, padding: 6))
            .Append(UnicodeHelper.GetDisplayWidth(TitleText, padding: 4 + 2 * BoxLabelPadding))
            .Append(UnicodeHelper.GetDisplayWidth(HintText, padding: 4 + 2 * BoxLabelPadding))
            .Max();

        _boxHeight = contentLines.Count + 4;

        _box = BuildBox();
    }

    public sealed override void OnMessage(ApplicationContext context, ApplicationMessage message)
    {
        if (message is not KeyMessage key)
            return;

        if (key.Modifiers.HasFlag(KeyModifier.Ctrl) && key.Character is 'e' or 'E' or '\x05')
        {
            Process.Start(new ProcessStartInfo(_settingsPath) { UseShellExecute = true });
            return;
        }

        if (key.Key is Key.Escape)
        {
            context.Quit();
            return;
        }

        OnKeyMessage(context, key);
    }

    protected virtual void OnKeyMessage(ApplicationContext context, KeyMessage key) { }

    public override void Render(RenderContext context)
    {
        Rectangle boxArea = context.Screen.Center(new Size(_boxWidth, _boxHeight));
        context.Render(new ClearWidget(style: _theme.ScreenStyle));
        context.Render(_box, boxArea);
        RenderOverlays(context, boxArea);
    }

    protected virtual void RenderOverlays(RenderContext context, Rectangle boxArea) { }

    private BoxWidget BuildBox() =>
        new BoxWidget()
            .Border(Border.Rounded)
            .Style(_theme.BoxStyle)
            .TitlePadding(BoxLabelPadding)
            .Title(TextLine.FromString($" {TitleText} ", _theme.TitleStyle))
            .Title(TextLine.FromString($" {HintText} ", _theme.HintStyle), TitlePosition.Bottom, Justify.Right)
            .Inner(new PaddingWidget(new Padding(2, 1), BuildInnerWidget()));
}
