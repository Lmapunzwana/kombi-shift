#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
GAME_EXECUTABLE="$SCRIPT_DIR/../Builds/CityInfrastructure2026/Linux/KombiShift2.x86_64"

if [[ ! -x "$GAME_EXECUTABLE" ]]; then
    printf 'Kombi Shift 2 Linux build was not found or is not executable:\n  %s\n' "$GAME_EXECUTABLE" >&2
    printf 'Rebuild it with KombiCityInfrastructureBuild.Linux, then try again.\n' >&2
    exit 1
fi

cd -- "$(dirname -- "$GAME_EXECUTABLE")"
exec "$GAME_EXECUTABLE" -force-glcore "$@"
