using ClaudeWorkspacePicker;
using ClaudeWorkspacePicker.Models;
using ClaudeWorkspacePicker.Ui;
using Spectre.Tui.App;
using System.Diagnostics;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.InputEncoding = System.Text.Encoding.UTF8;

if (args.Contains("--install-profile"))
{
    ProfileInstaller.Run();

    return;
}

const string ApplicationName = "ClaudeWorkspacePicker";
const string SettingsFileName = "settings.jsonc";

string settingsPath = Path.Combine(AppContext.BaseDirectory, SettingsFileName);

if (!File.Exists(settingsPath))
{
    try
    {
        using Stream src = typeof(Program).Assembly.GetManifestResourceStream($"{ApplicationName}.{SettingsFileName}")!;
        using FileStream dst = File.Create(settingsPath);
        await src.CopyToAsync(dst);
    }
    catch (IOException ex)
    {
        Console.Error.WriteLine($"Could not write default settings to {settingsPath}: {ex.Message}");
        Console.Error.WriteLine("Move the binary to a writable directory and try again.");

        return;
    }
}

Result<AppState> result = ConfigLoader.Load(settingsPath);

Application application = Application.Create();

if (!result.TryGetValue(out AppState? appState))
{
    await application.RunAsync(new ErrorScreen(result.Errors, settingsPath));

    return;
}

MainScreen mainScreen = new(appState);

await application.RunAsync(mainScreen);

if (mainScreen.SelectedEntry is not { } selectedEntry) return;

Environment.CurrentDirectory = selectedEntry.Path;

string claudeCommand = string.IsNullOrWhiteSpace(selectedEntry.Arguments) ? "claude" : $"claude {selectedEntry.Arguments}";

(string shell, string shellArgs) = OperatingSystem.IsWindows()
    ? ("cmd.exe", $"/c {claudeCommand}")
    : ("sh", $"-c '{claudeCommand.Replace("'", "'\\''")}'");

using Process process = new() { StartInfo = new ProcessStartInfo(shell, shellArgs) { UseShellExecute = false } };

process.Start();
