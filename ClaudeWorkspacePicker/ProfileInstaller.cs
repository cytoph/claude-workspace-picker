using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace ClaudeWorkspacePicker;

internal static partial class ProfileInstaller
{
    private const string ProfileGuid = "{3bd7de81-a386-40fe-b2e9-59692388ee7a}";
    private const string ProfileName = "Claude Workspace Picker";

    private const string SettingsPathStore = @"Packages\Microsoft.WindowsTerminal_8wekyb3d8bbwe\LocalState\settings.json";
    private const string SettingsPathPreview = @"Packages\Microsoft.WindowsTerminalPreview_8wekyb3d8bbwe\LocalState\settings.json";
    private const string SettingsPathUnpackaged = @"Microsoft\Windows Terminal\settings.json";

    private static readonly JsonSerializerOptions s_jsonSerializerOptions = new()
    {
        WriteIndented = true,
        IndentSize = 4,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    public static void Run()
    {
        if (!OperatingSystem.IsWindows())
        {
            Console.WriteLine("--install-profile is only supported on Windows.");

            return;
        }

        string? settingsPath = FindSettingsPath();

        if (settingsPath is null)
        {
            Console.WriteLine("Windows Terminal settings.json not found. Is Windows Terminal installed?");

            return;
        }

        try
        {
            string text = File.ReadAllText(settingsPath);
            JsonNode? root = JsonNode.Parse(text, documentOptions: new JsonDocumentOptions
            {
                CommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
            });

            if (root?["profiles"]?["list"] is not JsonArray profileList)
            {
                Console.WriteLine("Unexpected settings.json structure - could not find profiles list.");

                return;
            }

            if (Environment.ProcessPath is not { } exePath)
            {
                Console.WriteLine("Failed to install profile: could not determine exe path.");

                return;
            }

            foreach (JsonNode? node in profileList)
            {
                if (node is not JsonObject entry) continue;
                if (!string.Equals(entry["guid"]?.GetValue<string>(), ProfileGuid, StringComparison.OrdinalIgnoreCase)) continue;

                string? oldCommandline = entry["commandline"]?.GetValue<string>();

                if (string.Equals(oldCommandline, exePath, StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Profile already installed.");

                    return;
                }

                entry["commandline"] = JsonValue.Create(exePath);

                if (string.Equals(entry["icon"]?.GetValue<string>(), oldCommandline, StringComparison.OrdinalIgnoreCase))
                {
                    entry["icon"] = JsonValue.Create(exePath);
                }

                WriteSettings(settingsPath, root!);

                Console.WriteLine("Profile updated.");

                return;
            }

            JsonObject profile = new()
            {
                ["commandline"] = JsonValue.Create(exePath),
                ["guid"] = JsonValue.Create(ProfileGuid),
                ["hidden"] = JsonValue.Create(false),
                ["icon"] = JsonValue.Create(exePath),
                ["name"] = JsonValue.Create(ProfileName),
            };

            profileList.Add(profile);

            WriteSettings(settingsPath, root!);

            Console.WriteLine("Profile installed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to install profile: {ex.Message}");
        }
    }

    private static string? FindSettingsPath()
    {
        string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        string[] candidates =
        [
            Path.Combine(localAppData, SettingsPathStore),
            Path.Combine(localAppData, SettingsPathPreview),
            Path.Combine(localAppData, SettingsPathUnpackaged),
        ];

        return candidates.FirstOrDefault(File.Exists);
    }

    private static void WriteSettings(string path, JsonNode root)
    {
        string json = root.ToJsonString(s_jsonSerializerOptions);

        json = MoveOpeningBracketsToNextLine(json);

        File.WriteAllText(path, json);
    }

    private static string MoveOpeningBracketsToNextLine(string json)
    {
        // Normalize to \n so that $ in the regex reliably matches end of line -
        // on Windows, System.Text.Json emits \r\n and $ does not match before \r.
        json = json.ReplaceLineEndings("\n");

        // Move any { or [ that immediately follows a JSON key onto its own line at the same indent.
        json = OpeningBracketAfterKeyPattern().Replace(json, "$1$2\n$1$3");

        // Restore \r\n to match Windows Terminal's line ending convention.
        return json.ReplaceLineEndings("\r\n");
    }

    [GeneratedRegex(@"^(\s*)(""(?:[^""\\]|\\.)*"": )(\{|\[)$", RegexOptions.Multiline)]
    private static partial Regex OpeningBracketAfterKeyPattern();
}
