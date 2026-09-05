#!/bin/sh
set -eu
root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
state="$HOME/Library/Application Support/CodexTokenQuest"
mkdir -p "$state"
if [ "$(uname -s)" != Darwin ]; then
    echo 'This launcher supports macOS. Use start-desktop.ps1 on Windows.' >&2
    exit 1
fi
if [ "${1:-}" != --worker ]; then
    nohup /bin/sh "$root/scripts/start-desktop.sh" --worker </dev/null >>"$state/bootstrap.log" 2>&1 &
    exit 0
fi
# Serialize only the bootstrap compilation; all desktop build/lifetime decisions
# live in the common C# launcher. shlock reclaims locks whose owner has exited.
/usr/bin/shlock -f "$state/bootstrap.lock" -p "$$" || exit 0
trap 'rm -f "$state/bootstrap.lock"' EXIT HUP INT TERM
sdk=''
for candidate in "${DOTNET_ROOT:-$HOME/.dotnet}/dotnet" "$HOME/.dotnet/dotnet" "$(command -v dotnet || true)" /usr/local/share/dotnet/dotnet /opt/homebrew/bin/dotnet; do
    if [ -x "$candidate" ] && "$candidate" --list-sdks | /usr/bin/grep -Eq '^10\.0\.[0-9]+ \['; then
        sdk="$candidate"
        break
    fi
done
if [ -z "$sdk" ]; then
    echo '.NET 10 SDK is required. Install it from https://dotnet.microsoft.com/download/dotnet/10.0' >&2
    exit 1
fi
export DOTNET_ROOT="$(dirname "$sdk")"
export DOTNET_HOST_PATH="$sdk"
cd "$root"
"$sdk" run --project "$root/src/CodexTokenQuest.Launcher" --artifacts-path "$state/build/launcher" -c Release -p:UseSharedCompilation=false -- "$root"
