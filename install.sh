#!/usr/bin/env bash
set -euo pipefail

echo 'Installing Claude Workspace Picker...'

# Detect OS
case "$(uname -s)" in
    Linux)  os='linux' ;;
    Darwin) os='osx' ;;
    *)      echo "Error: Unsupported OS: $(uname -s)" >&2; exit 1 ;;
esac

# Detect architecture
case "$(uname -m)" in
    x86_64)        arch='x64' ;;
    arm64|aarch64) arch='arm64' ;;
    *)             echo "Error: Unsupported architecture: $(uname -m)" >&2; exit 1 ;;
esac

# Rosetta 2 check: prefer native arm64 binary even when running in an x64 shell on Apple Silicon
if [ "$os" = 'osx' ] && [ "$arch" = 'x64' ]; then
    if [ "$(sysctl -n sysctl.proc_translated 2>/dev/null)" = '1' ]; then
        arch='arm64'
    fi
fi

# Step 1 - resolve latest release
tag=$(curl -fsSL 'https://api.github.com/repos/cytoph/claude-workspace-picker/releases/latest' \
    | grep '"tag_name"' \
    | sed 's/.*"tag_name": *"\([^"]*\)".*/\1/')
if [ -z "$tag" ]; then
    echo 'Error: Could not resolve latest release.' >&2
    exit 1
fi
echo "  [1/5] Resolved latest release: $tag"

# Step 2 - create install directory
install_dir="$HOME/.local/share/ClaudeWorkspacePicker"
mkdir -p "$install_dir"
echo "  [2/5] Install directory: $install_dir"

# Step 3 - download binary
asset_name="ClaudeWorkspacePicker-${os}-${arch}"
asset_url="https://github.com/cytoph/claude-workspace-picker/releases/download/${tag}/${asset_name}"
binary_path="$install_dir/ClaudeWorkspacePicker"
echo "  [3/5] Downloading $asset_name..."
curl -fsSL "$asset_url" -o "$binary_path"
chmod +x "$binary_path"

# Step 4 - write starter settings.jsonc if absent
settings_path="$install_dir/settings.jsonc"
if [ ! -f "$settings_path" ]; then
    settings_url="https://raw.githubusercontent.com/cytoph/claude-workspace-picker/${tag}/ClaudeWorkspacePicker/settings.jsonc"
    curl -fsSL "$settings_url" -o "$settings_path"
    echo '  [4/5] Wrote starter settings.jsonc'
else
    echo '  [4/5] settings.jsonc already exists - skipped'
fi

# Step 5 - create symlink in ~/.local/bin if the directory exists
# (Windows Terminal profile installation is Windows-only - not applicable here)
symlink_path="$HOME/.local/bin/ClaudeWorkspacePicker"
if [ -d "$HOME/.local/bin" ]; then
    ln -sf "$binary_path" "$symlink_path"
    echo "  [5/5] Symlink created at $symlink_path"
else
    echo '  [5/5] Skipped symlink (~/.local/bin not found)'
fi

echo ''
echo "Done. ClaudeWorkspacePicker installed to $install_dir"
echo "Edit settings: $install_dir/settings.jsonc"
if [ ! -d "$HOME/.local/bin" ]; then
    echo ''
    echo 'To use by name, add to your PATH:'
    echo "  export PATH=\"\$HOME/.local/share/ClaudeWorkspacePicker:\$PATH\""
fi
