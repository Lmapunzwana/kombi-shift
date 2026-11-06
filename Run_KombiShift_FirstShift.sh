#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"
exec ./Builds/KombiShift_FirstShift/KombiShift.x86_64 "$@"
