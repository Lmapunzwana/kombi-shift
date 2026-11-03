#!/usr/bin/env bash
set -euo pipefail
kombi_workspace="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
kombi_project=UnityHDRP
if [[ "${1:-}" == builtin ]]; then kombi_project=UnityStarter; fi
kombi_graphics=()
if [[ "$kombi_project" == UnityHDRP ]]; then kombi_graphics=(-force-vulkan); fi
exec /home/jayden-mapasure/Unity/Hub/Editor/6000.6.2f1/Editor/Unity "${kombi_graphics[@]}" -job-worker-count 2 -projectPath "$kombi_workspace/$kombi_project" -executeMethod KombiShift.JaydenStarter.Editor.GameWorkbench.OpenProject -logFile "$kombi_workspace/Builds/Editor-Interactive.log"
