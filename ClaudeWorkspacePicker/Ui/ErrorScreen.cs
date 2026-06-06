using ClaudeWorkspacePicker.Models;
using Spectre.Tui;
using Color = Spectre.Console.Color;
using Decoration = Spectre.Console.Decoration;
using Style = Spectre.Console.Style;

namespace ClaudeWorkspacePicker.Ui;

sealed class ErrorScreen : AppScreen
{
    private static readonly Color s_errorColor = Color.FromHex("#cc0000");
    private static readonly Color s_hintColor = s_errorColor.Blend(Color.Black, 0.4f);
    private static readonly ScreenTheme s_theme = new(
        TitleIcon: "⚠️",
        TitleText: "Configuration Error",
        TitleStyle: new Style(foreground: s_errorColor, decoration: Decoration.Bold),
        BoxStyle: new Style(foreground: s_errorColor),
        HintStyle: new Style(foreground: s_hintColor),
        ScreenStyle: new Style(background: Color.Default)
    );

    private readonly IReadOnlyList<string> _errors;

    private readonly ErrorLinesWidget _errorWidget;

    public ErrorScreen(IReadOnlyList<string> errors, string settingsPath) : base(s_theme, settingsPath)
    {
        _errors = errors;

        _errorWidget = new ErrorLinesWidget(_errors);
    }

    protected override IReadOnlyList<string> GetContentLines() => _errors;

    protected override IWidget BuildInnerWidget() => _errorWidget;

    private sealed class ErrorLinesWidget : IWidget
    {
        private static readonly Style s_style = new(foreground: Color.Default);
        private readonly IReadOnlyList<string> _lines;

        internal ErrorLinesWidget(IReadOnlyList<string> lines) => _lines = lines;

        public void Render(RenderContext context)
        {
            Rectangle area = context.Viewport;

            for (int i = 0; i < _lines.Count; i++)
            {
                context.Render(Paragraph.FromString(_lines[i], s_style), new Rectangle(area.X, area.Y + i, area.Width, 1));
            }
        }
    }
}
