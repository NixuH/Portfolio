# Command System

A lightweight, extensible command framework for unit and building actions (Move, Attack, Build, ...) in a Unity RTS-style game. Commands go through a shared lifecycle and can hook extra behaviour (VFX, sound, animation) without subclassing.

## Overview

Every player-triggered or AI-triggered action (moving a unit, placing a building, attacking a target) is modeled as a `Command`. A `CommandCaster` component sits on each `GameObject` that can issue commands (units, buildings) and drives their lifecycle every physics tick.

```
Start -> [NeedAction] -> Execute -> [Wait] -> Finish / Cancel / Error
```

- **Start** — the command initializes itself and, if it doesn't need a target, runs immediately.
- **NeedAction** — the command is waiting for the player to click a ground position or select a target.
- **Execute** — runs every `FixedUpdate` while the command is active (e.g. a building preview following the cursor).
- **Wait** — the command pauses, optionally running a queue of coroutines (e.g. "wait 3 seconds for construction"), then resumes automatically.
- **Finish / Cancel / Error** — terminal states. `Complete()` is always called at the end, and `Dispose()` cleans up any resources the command allocated (like preview objects).

## Core Classes

| File | Responsibility |
|---|---|
| `Command.cs` | Abstract base class. Owns the lifecycle, exception-safe hooks, and coroutine-based waiting. |
| `CommandCaster.cs` | `MonoBehaviour` that owns the currently active command, ticks passive commands, and forwards player input to whichever command is waiting for it. |
| `CommandEnums.cs` | Shared enums: `CommandState`, `CallType`, `TargetType`, `WaitFailure`, `AfterWaitBehaviour`, `CommandType`. |
| `CommandsData.cs` | `ScriptableObject` holding designer-editable data (building costs, prefabs, build times), looked up by `BuildKey`. |
| `Build_Command.cs` | Concrete command: instantiates a building and waits out its construction time. |
| `PlaceBuilding_Command.cs` | Concrete command: shows a semi-transparent preview that follows the cursor, then spawns a `Build_Command` on confirm. |

## Adding a New Command

1. Subclass `Command` and implement the abstract members (`OnStart`, `Execute`, `OnAction`, `OnFinish`, `OnCancel`, `OnError`, `Cleaning`).
2. Pick the right `CallType`:
   - `Active` — runs immediately (e.g. `Build_Command`).
   - `NeedAction` — waits for a target (e.g. `PlaceBuilding_Command`).
   - `Passive` — ticked every frame without being the "current" command (e.g. auto-attack).
   - `OnEvent` — triggered externally by a specific game event.
3. Pick a `TargetType` so `CommandCaster` knows what layer to raycast against and which `Action(...)` overload to call.
4. If the command needs to pause (animations, timers, sequential steps), call `Wait(...)` with a queue of `Func<IEnumerator>` and an `AfterWaitBehaviour` describing what happens once the wait ends.
5. Always release resources you allocate (spawned previews, temporary objects) in `Cleaning()` — check `state` first if the object should survive a successful `Finish()` (see `Build_Command.Cleaning()`).

## Known Gotchas

- **`Cleaning()` runs on every terminal state**, including a successful `Finish()`. If a command spawns something meant to persist after success (like a finished building), guard the destroy call with a state check — don't destroy on `CommandState.Finished`.
- **Constructor parameters must be assigned to their matching fields.** A mismatched name (e.g. `buildkey` parameter vs `buildKey` field) compiles fine but silently leaves the field at its default value.
- **`Dispose()` has no finalizer on purpose** — cleanup touches Unity APIs that must run on the main thread, so a missed explicit `Dispose()` call will leak the wait-coroutine handle.

## Requirements

- Unity (new Input System)
- `Game.Commands` namespace for all files above except the two concrete example commands, which currently live in the global namespace (consider moving them into `Game.Commands` for consistency)