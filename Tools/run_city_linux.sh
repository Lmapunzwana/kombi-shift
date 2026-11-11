#!/usr/bin/env bash
set -euo pipefail
script_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
player="$script_dir/../../../Builds/CityInfrastructure2026/Linux/KombiShift2.x86_64"
if [[ ! -f "$player" ]]; then
  echo "Local city build missing. Open the Unity main menu or build with KombiCityInfrastructureBuild.Linux." >&2
  exit 1
fi
exec "$player" -force-glcore "$@"
