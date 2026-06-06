using System.Text.Json;
using ClaudeWorkspacePicker.Models;
using Spectre.Console;

namespace ClaudeWorkspacePicker;

static class ConfigLoader
{
    private const string DefaultTitleIcon = "\U0001f916";
    private const string DefaultTitleText = "Claude Launcher";
    private const string DefaultBoxColor = "#0037da";
    private static readonly Color s_defaultBoxColor = Color.FromHex(DefaultBoxColor);
    private static readonly Decoration s_defaultSelectedDecoration = Decoration.Bold;

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
            return Result.Errors<AppState>(configResult.Errors);

        List<string> errors = [];

        Color boxColor = ParseHexOrDefault(config.BoxColor, "boxColor", s_defaultBoxColor, errors);
        Color titleForeground = ParseHexOrDefault(config.TitleForeground, "titleForeground", boxColor, errors);
        Color hintForeground = ParseHexOrDefault(config.HintForeground, "hintForeground", boxColor.Blend(Color.Black, 0.4f), errors);
        Color background = ParseHexOrDefault(config.Background, "background", Color.Default, errors);
        Color foreground = ParseHexOrDefault(config.Foreground, "foreground", Color.Default, errors);
        Color selectedForeground = ParseHexOrDefault(config.SelectedForeground, "selectedForeground", Color.Default, errors);
        Color selectedBackground = ParseHexOrDefault(config.SelectedBackground, "selectedBackground", Color.Default, errors);
        Decoration selectedDecoration = ParseDecorationOrDefault(config.SelectedTextStyle, s_defaultSelectedDecoration, errors);

        List<MenuEntry> entries = BuildMenuEntries(config, errors);

        if (errors.Count > 0)
            return Result.Errors<AppState>(errors);

        ScreenTheme screenTheme = new(
            TitleIcon: config.TitleIcon ?? DefaultTitleIcon,
            TitleText: config.TitleText ?? DefaultTitleText,
            TitleStyle: new Style(foreground: titleForeground, background: background, decoration: Decoration.Bold),
            BoxStyle: new Style(foreground: boxColor, background: background),
            HintStyle: new Style(foreground: hintForeground, background: background),
            ScreenStyle: new Style(background: background)
        );

        ListItemTheme listItemTheme = new(
            NormalStyle: new Style(foreground: foreground, background: background),
            SelectedStyle: new Style(foreground: selectedForeground, background: selectedBackground, decoration: selectedDecoration)
        );

        return new AppState(screenTheme, listItemTheme, entries, config.GlobalArguments, path);
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
                return Result.Error<LauncherConfig>("settings.jsonc deserialized to null - check that the root object has a \"directories\" array.");

            return config;
        }
        catch (JsonException ex)
        {
            return Result.Error<LauncherConfig>($"Invalid JSON in settings.jsonc:\n{ex.Message}");
        }
    }

    private static Color ParseHexOrDefault(string? hex, string fieldName, Color fallback, List<string> errors)
    {
        if (hex is null)
            return fallback;

        if (!Color.TryFromHex(hex, out Color color))
        {
            errors.Add($"Invalid hex color for '{fieldName}': \"{hex}\"");

            return fallback;
        }

        return color;
    }

    private static Decoration ParseDecorationOrDefault(string? textStyle, Decoration fallback, List<string> errors)
    {
        if (textStyle is null)
            return fallback;

        Decoration result = Decoration.None;

        foreach (string part in textStyle.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (!TryParseDecorationPart(part, out Decoration d))
            {
                errors.Add($"Unknown style token in 'selectedTextStyle': \"{part}\".");

                continue;
            }

            result |= d;
        }

        return result;
    }

    private static List<MenuEntry> BuildMenuEntries(LauncherConfig config, List<string> errors)
    {
        List<MenuEntry> entries = [];

        if (config.Directories is { } directories)
        {
            for (int i = 0; i < directories.Count; i++)
            {
                DirectoryEntryConfig dirConfig = directories[i];

                if (dirConfig.Path is null)
                {
                    errors.Add($"Directory entry {i}: missing 'path'");
                    continue;
                }

                if (dirConfig.Icon is null)
                {
                    errors.Add($"Directory entry {i}: missing 'icon'");
                    continue;
                }

                string resolvedPath = Environment.ExpandEnvironmentVariables(dirConfig.Path);

                if (!Directory.Exists(resolvedPath))
                {
                    errors.Add($"Directory entry {i}: path does not exist: \"{resolvedPath}\"");
                    continue;
                }

                string trimmedPath = resolvedPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                string leafName = Path.GetFileName(trimmedPath);

                if (string.IsNullOrEmpty(leafName))
                    leafName = trimmedPath;

                string displayName = dirConfig.DisplayName ?? leafName;

                entries.Add(new MenuEntry(dirConfig.Icon, displayName, resolvedPath, OverrideArguments: dirConfig.OverrideArguments));
            }
        }

        entries.Add(MenuEntry.CustomPathEntry);

        return entries;
    }

    private static bool TryParseDecorationPart(string part, out Decoration decoration)
    {
        Decoration? result = part.ToLowerInvariant() switch
        {
            "bold" => Decoration.Bold,
            "italic" => Decoration.Italic,
            "underline" => Decoration.Underline,
            "strikethrough" => Decoration.Strikethrough,
            "blink" => Decoration.SlowBlink,
            "dim" => Decoration.Dim,
            "invert" => Decoration.Invert,
            _ => null,
        };

        decoration = result ?? default;

        return result.HasValue;
    }
}
