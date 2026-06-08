using System.Diagnostics;
using ClaudeWorkspacePicker;
using ClaudeWorkspacePicker.Models;
using ClaudeWorkspacePicker.Ui;
using Spectre.Tui.App;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.InputEncoding = System.Text.Encoding.UTF8;

if (args.Contains("--install-profile"))
{
    ProfileInstaller.Run();
    return;
}

string applicationName = "ClaudeWorkspacePicker";
string settingsFileName = "settings.jsonc";

string localPath = Path.Combine(AppContext.BaseDirectory, settingsFileName);
string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), applicationName, settingsFileName);

string settingsPath;

if (File.Exists(localPath))
{
    settingsPath = localPath;
}
else if (File.Exists(appDataPath))
{
    settingsPath = appDataPath;
}
else
{
    Directory.CreateDirectory(Path.GetDirectoryName(appDataPath)!);
    using Stream src = typeof(Program).Assembly.GetManifestResourceStream($"{applicationName}.settings.jsonc")!;
    using var dst = File.Create(appDataPath);
    await src.CopyToAsync(dst);
    settingsPath = appDataPath;
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

using Process process = new()
{
    StartInfo = new ProcessStartInfo("cmd.exe", $"/c {claudeCommand}")
    {
        UseShellExecute = false,
    }
};

process.Start();
