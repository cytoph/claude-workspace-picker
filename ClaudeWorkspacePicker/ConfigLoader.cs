using System.Text.Json;
using ClaudeWorkspacePicker.Helpers;
using ClaudeWorkspacePicker.Models;
using Color = Spectre.Console.Color;
using Decoration = Spectre.Console.Decoration;
using Style = Spectre.Console.Style;

namespace ClaudeWorkspacePicker;

static class ConfigLoader
{
    private const string DefaultTitleIcon = "\U0001f916";
    private const string DefaultTitleText = "Claude Launcher";
    private const string DefaultBoxColor = "#0037da";
    private const string DefaultSelectedTextStyle = "bold";

    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static Result<AppState> Load(string path)
    {
        Result<LauncherConfig> configResult = ReadConfig(path);

        if (!configResult.TryGetValue(out LauncherConfig? config))
            return Result.Error<AppState>(configResult.ErrorMessage!);

        ScreenTheme screenTheme = BuildScreenTheme(config);
        ListItemTheme listItemTheme = BuildListItemTheme(config);
        List<MenuEntry> entries = BuildMenuEntries(config);

        return new AppState(screenTheme, listItemTheme, entries);
    }

    private static Result<LauncherConfig> ReadConfig(string path)
    {
        if (!File.Exists(path))
            return Result.Error<LauncherConfig>($"Settings file not found:\n{path}");

        try
        {
            string json = File.ReadAllText(path);
            LauncherConfig? config = JsonSerializer.Deserialize<LauncherConfig>(json, s_jsonOptions);

            if (config is null)
                return Result.Error<LauncherConfig>("settings.jsonc deserialized to null — check that the root object has a \"directories\" array.");

            return config;
        }
        catch (JsonException ex)
        {
            return Result.Error<LauncherConfig>($"Invalid JSON in settings.jsonc:\n{ex.Message}");
        }
    }

    private static ScreenTheme BuildScreenTheme(LauncherConfig config)
    {
        string resolvedBoxHex = ValidHex(config.BoxColor) ?? DefaultBoxColor;
        Color boxColor = ParseColor(resolvedBoxHex);
        Color titleFg = ColorHelper.TryParseHexColor(config.TitleForeground, out Color tfc) ? tfc : boxColor;
        Color hintFg = ParseColor(config.HintForeground ?? ColorHelper.DarkenHex(resolvedBoxHex));
        Color background = ColorHelper.TryParseHexColor(config.Background, out Color bg) ? bg : Color.Default;

        return new ScreenTheme(
            TitleIcon: config.TitleIcon ?? DefaultTitleIcon,
            TitleText: config.TitleText ?? DefaultTitleText,
            TitleStyle: new Style(foreground: titleFg, background: background, decoration: Decoration.Bold),
            BoxStyle: new Style(foreground: boxColor, background: background),
            HintStyle: new Style(foreground: hintFg, background: background),
            ScreenStyle: new Style(background: background)
        );
    }

    private static ListItemTheme BuildListItemTheme(LauncherConfig config)
    {
        Color foreground = ColorHelper.TryParseHexColor(config.Foreground, out Color fg) ? fg : Color.Default;
        Color background = ColorHelper.TryParseHexColor(config.Background, out Color bg) ? bg : Color.Default;
        Color selectedForeground = ColorHelper.TryParseHexColor(config.SelectedForeground, out Color sfg) ? sfg : Color.Default;
        Color selectedBackground = ColorHelper.TryParseHexColor(config.SelectedBackground, out Color sbg) ? sbg : Color.Default;
        Decoration selectedDecoration = ParseDecoration(config.SelectedTextStyle ?? DefaultSelectedTextStyle);

        return new ListItemTheme(
            NormalStyle: new Style(foreground: foreground, background: background),
            SelectedStyle: new Style(foreground: selectedForeground, background: selectedBackground, decoration: selectedDecoration)
        );
    }

    private static List<MenuEntry> BuildMenuEntries(LauncherConfig config)
    {
        List<MenuEntry> entries = [];

        if (config.Directories is { } directories)
        {
            foreach (DirectoryEntryConfig dirConfig in directories)
            {
                if (dirConfig.Path is null || dirConfig.Icon is null) continue;

                string resolvedPath = Environment.ExpandEnvironmentVariables(dirConfig.Path);

                if (!Directory.Exists(resolvedPath)) continue;

                string trimmedPath = resolvedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string leafName = Path.GetFileName(trimmedPath);

                if (string.IsNullOrEmpty(leafName))
                    leafName = trimmedPath;

                string displayName = dirConfig.DisplayName ?? leafName;

                entries.Add(new MenuEntry(dirConfig.Icon, displayName, resolvedPath));
            }
        }

        entries.Add(MenuEntry.CustomPathEntry);

        return entries;
    }

    private static Decoration ParseDecoration(string textStyle)
    {
        Decoration result = Decoration.None;

        foreach (string part in textStyle.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            result |= ParseDecorationPart(part);

        return result;
    }

    private static Decoration ParseDecorationPart(string part) => part.ToLowerInvariant() switch
    {
        "bold" => Decoration.Bold,
        "italic" => Decoration.Italic,
        "underline" => Decoration.Underline,
        "strikethrough" => Decoration.Strikethrough,
        "blink" => Decoration.SlowBlink,
        "dim" => Decoration.Dim,
        "invert" => Decoration.Invert,
        _ => Decoration.None
    };

    private static Color ParseColor(string? hex) =>
        ColorHelper.TryParseHexColor(hex, out Color color) ? color : Color.Default;

    private static string? ValidHex(string? value) => ColorHelper.TryParseHexColor(value, out _) ? value : null;
}
