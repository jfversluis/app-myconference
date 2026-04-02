#!/usr/bin/env bash
# configure.sh — White-label configuration script
# Reads event_config.json and updates .csproj with app metadata.
#
# Usage:
#   ./configure.sh                          # uses default event_config.json
#   ./configure.sh --config my_event.json   # uses custom config file
#
# Requires: python3 (for JSON parsing)

set -euo pipefail

CONFIG_FILE="src/Conference.Maui/Resources/Raw/event_config.json"
CSPROJ="src/Conference.Maui/Conference.Maui.csproj"

# Parse arguments
while [[ $# -gt 0 ]]; do
    case $1 in
        --config)
            CONFIG_FILE="$2"
            shift 2
            ;;
        --help|-h)
            echo "Usage: ./configure.sh [--config path/to/event_config.json]"
            echo ""
            echo "Reads event configuration and updates the .csproj with:"
            echo "  - ApplicationTitle (from app.displayName)"
            echo "  - ApplicationId (from app.bundleId)"
            echo "  - ApplicationDisplayVersion (from app.version)"
            echo ""
            echo "Also copies the config to the app's Resources/Raw/ folder if"
            echo "a custom path is specified."
            exit 0
            ;;
        *)
            echo "Unknown option: $1"
            exit 1
            ;;
    esac
done

if [ ! -f "$CONFIG_FILE" ]; then
    echo "❌ Config file not found: $CONFIG_FILE"
    exit 1
fi

if [ ! -f "$CSPROJ" ]; then
    echo "❌ Project file not found: $CSPROJ"
    exit 1
fi

echo "📋 Reading config from: $CONFIG_FILE"

# Extract values using python3 (available on macOS and most Linux)
read_json() {
    python3 -c "
import json, sys
with open('$CONFIG_FILE') as f:
    data = json.load(f)
keys = '$1'.split('.')
val = data
for k in keys:
    val = val.get(k, '')
    if val == '':
        break
print(val)
"
}

APP_NAME=$(read_json "app.displayName")
BUNDLE_ID=$(read_json "app.bundleId")
APP_VERSION=$(read_json "app.version")
EVENT_NAME=$(read_json "event.name")

echo "  Event: $EVENT_NAME"
echo "  App Name: $APP_NAME"
echo "  Bundle ID: $BUNDLE_ID"
echo "  Version: $APP_VERSION"

# Update .csproj values using sed
update_csproj() {
    local tag="$1"
    local value="$2"
    if [ -n "$value" ]; then
        if [[ "$OSTYPE" == "darwin"* ]]; then
            sed -i '' "s|<${tag}>.*</${tag}>|<${tag}>${value}</${tag}>|" "$CSPROJ"
        else
            sed -i "s|<${tag}>.*</${tag}>|<${tag}>${value}</${tag}>|" "$CSPROJ"
        fi
        echo "  ✅ ${tag} → ${value}"
    fi
}

echo ""
echo "🔧 Updating $CSPROJ..."
update_csproj "ApplicationTitle" "$APP_NAME"
update_csproj "ApplicationId" "$BUNDLE_ID"
update_csproj "ApplicationDisplayVersion" "$APP_VERSION"

# Copy config to Resources/Raw if using a custom path
DEST="src/Conference.Maui/Resources/Raw/event_config.json"
if [ "$CONFIG_FILE" != "$DEST" ]; then
    cp "$CONFIG_FILE" "$DEST"
    echo "  ✅ Copied config to $DEST"
fi

echo ""
echo "✨ Done! Build your app with:"
echo "   dotnet build src/Conference.Maui/Conference.Maui.csproj -f net10.0-ios"
