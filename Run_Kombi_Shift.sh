#!/usr/bin/env bash
set -euo pipefail
preview_root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
preview_binary="$preview_root/Builds/Linux/KombiShift.x86_64"
if [[ -x "$preview_binary" ]]; then
  exec "$preview_binary" -screen-fullscreen 0 -screen-width 1280 -screen-height 800 "$@"
fi
if command -v unity >/dev/null 2>&1; then
  exec unity open "$preview_root/UnityStarter"
fi
printf '%s\n' 'Open the UnityStarter folder in Unity Hub (Unity 6000.6.2f1), then open Assets/KombiShiftJayden/Generated/KombiShiftStarter.unity and press Play.'
