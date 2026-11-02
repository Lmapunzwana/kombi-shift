# Team integration policy
- main is the integration branch; no direct pushes once remote protection is configured.
- Branch naming: feature/<owner>-<short-task>, fix/<owner>-<short-task>.
- One reviewer other than author for every PR. Merge to main by 20:00 daily.
- Every PR: working build check, changelog line, contribution and AI-use log.
- Scene ownership: Jayden (environment), Euclide (vehicle test), Tendai (traffic test), Tadiwa (passengers/UI), Leroy (bootstrap/integration). Agree actual scene names at standup.
- Avoid editing another person's .unity scene; use prefabs and C# contracts.
- Git LFS must be installed on all workstations before asset commits.

Main branch protection configured on GitHub: PR required, one approval, stale approvals dismissed, conversations resolved and administrator bypass disabled. Force pushes/deletion remain disallowed.
