using System.Diagnostics;
using ClaudeWorkspacePicker;
using ClaudeWorkspacePicker.Models;
using ClaudeWorkspacePicker.Ui;
using Spectre.Tui.App;

Console.OutputEncoding = System.Text.Encoding.UTF8;
Console.InputEncoding = System.Text.Encoding.UTF8;

string applicationName = "ClaudeWorkspacePicker";
string settingsFileName = "settings.jsonc";

string localPath = Path.Combine(AppContext.BaseDirectory, settingsFileName);
string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), applicationName, settingsFileName);

string settingsPath;

if (File.Exists(appDataPath))
{
    settingsPath = appDataPath;
}
else if (File.Exists(localPath))
{
    settingsPath = localPath;
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

if (!result.TryGetValue(out AppState? appState))
{
    Console.WriteLine($"Configuration Error: {result.ErrorMessage}");
    return;
}

MainScreen mainScreen = new(appState);

await Application.Create().RunAsync(mainScreen);

string? selectedPath = mainScreen.SelectedPath;

if (selectedPath is null) return;

Environment.CurrentDirectory = selectedPath;

using Process process = new()
{
    StartInfo = new ProcessStartInfo("cmd.exe", "/c claude")
    {
        UseShellExecute = false,
    }
};

process.Start();
process.WaitForExit();
