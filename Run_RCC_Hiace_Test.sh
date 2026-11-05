#!/usr/bin/env bash
set -euo pipefail

project_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
player="$project_dir/Builds/RCC_Hiace_Test/KombiShift_RCC_Test.x86_64"

if [[ ! -x "$player" ]]; then
    echo "RCC Hiace test build is missing. Build it from Unity before running this launcher." >&2
    exit 1
fi

exec "$player" "$@"
