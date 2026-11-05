#!/usr/bin/env bash
set -euo pipefail

project_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
player="$project_dir/Builds/RCC_Hiace_EasyRoadsTest/KombiShift_RCC_EasyRoadsTest.x86_64"

if [[ ! -x "$player" ]]; then
    echo "EasyRoads test build is missing. Build it from Unity before running this launcher." >&2
    exit 1
fi

exec "$player" "$@"
