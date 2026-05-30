# Claude Workspace Picker

A full-screen terminal UI for picking a workspace and launching [Claude Code](https://claude.ai/code) in it. Run it as a [Windows Terminal](https://github.com/microsoft/terminal) profile — select a directory from your configured list (or enter a custom path), and Claude Code opens there automatically.

## Installation

**1. Clone and publish**

Clone the repo and build.

**2. Add a Windows Terminal profile**

In your Windows Terminal `settings.json`, add a profile under `"profiles"` → `"list"`:

```json
{
    "guid": "{00000000-0000-0000-0000-000000000000}", // replace with a generated GUID
    "name": "Claude",
    "commandline": "C:\\path\\to\\ClaudeWorkspacePicker.exe",
    "startingDirectory": null
}
```

**3. Configure your workspaces**

On first launch, a starter `settings.jsonc` is created at `%LOCALAPPDATA%\ClaudeWorkspacePicker\settings.jsonc`. Edit it to add your directories.
