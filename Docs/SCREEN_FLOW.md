# Screen flow — integration draft
Owner: Tadiwa; integration draft prepared for review on 1 November 2026.

```mermaid
flowchart TD
 Menu[Main menu] --> Instructions
 Instructions --> Menu
 Menu --> Levels[Level select]
 Levels --> Conductor[Conductor selection]
 Conductor --> Playing[Gameplay HUD]
 Playing --> Pause[Pause menu]
 Pause --> Settings
 Settings --> Pause
 Pause --> Playing
 Pause --> Menu
 Playing --> Statement[Shift statement]
 Playing --> GameOver[Game over]
 Statement --> Win
 Statement --> GameOver
 Statement --> Menu
 Win --> Menu
 GameOver --> Menu
```

Level/conductor/instructions/settings are UI routes, not additional GameState values. The state remains Menu during pre-game selection and Paused during settings. Gameplay simulation must subscribe to state changes; pausing time alone does not disable Update input handlers. ShiftEnd/Win/GameOver must block gameplay commands explicitly.
