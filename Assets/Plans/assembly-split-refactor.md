# Project Overview

> **Nature of this plan:** This is a **structural refactor**, not a gameplay feature. The goal is to split the monolithic `Assembly-CSharp` game code under `Assets/Scripts` into a set of layered assembly definitions (`.asmdef`) for faster incremental compile times and enforceable decoupling. **All game mechanics, controls, and UI behavior must be preserved exactly** — this is behavior-neutral.

- **Game Title:** Rage Rooster
- **High-Level Concept:** (unchanged — 3D action-platformer)
- **Players:** Single player
- **Render Pipeline:** URP (URP-HighFidelity)
- **Target Platform:** StandaloneWindows64
- **Unity Version:** 6000.2.6f2
- **Input System:** Both (New Input System is primary via `Input` ScriptableObject asset)

## Refactor Goals
1. Split `Assets/Scripts` game code into **7 layered assemblies** so a change in one domain recompiles only that assembly + dependents, not the whole project.
2. Make coupling **visible and enforceable** (asmdef reference rules) instead of implicit global-namespace access.
3. Break the mutual cycles using the project's **existing patterns**: the `Services` static locator + C# events for *statically-accessible* functionality, and **interfaces** (defined in Core) for *instance-related* functionality.
4. Namespace code per assembly (`RageRooster.*`) as each layer is carved.

# Game Mechanics

**Unchanged.** This refactor must not alter the core gameplay loop, combat, movement, room transitions, save/load, or dialog. Verification (below) exists specifically to confirm zero behavioral change.

## Controls and Input Methods

**Unchanged.** Input continues to flow through the existing `Input` ScriptableObject (New Input System). Note: `Systems/Input/PlayerActions.cs` currently declares `namespace UnityEngine` — this landmine is fixed during the split (see Step 1).

# UI

**Unchanged visually/behaviorally.** Structurally, UI becomes its own assembly (`RageRooster.UI`) that is a **peer of Actors** — neither references the other directly. The current bidirectional UI↔Entities coupling is inverted:
- **UI → Player state** (HUD reading `Player.Health/Ammo/Currency`): via **interfaces** in Core, obtained through a static `Services.Player` accessor.
- **Entities → UI** (`PlayerRanged`/`ItemPickup`/`Hen` calling `UIHUDSystem.Instance` for hitmarkers/ammo): via **Services/events** (`Services.Hud`), since the HUD is a static-accessible singleton.

# Key Asset & Context

## Current State (discovered)
- `Assets/Scripts` = **262 game scripts**, almost entirely in **`Assembly-CSharp`** and the **global namespace**.
- Pre-existing assemblies: `Utilities` (`Assets/Scripts/UtilityCore`), `Utilities.Editor`, `VisualNodes`, `Audio`, plus plugin asmdefs (FMOD, DOTween, `SLS.StateMachineH` = HierarchyStateMachine, `AYellowpaper.*`, EditorAttributes, UltEvents, ListUtilities, EPO).
- **Existing decoupling seam (keystone):** `Assets/Scripts/UtilityCore/Services/` — `Service<T>`, `GetterSetterService<T>`, `IService<T>` (in `Service.cs`) + `GameServices.cs` (namespace `Services`, currently exposes only `Services.Gameplay` and `Services.RoomManager`). This is the mechanism we expand.
- **Event seam already present:** `Player.Health/Ammo/Currency` models expose `updateHealth`/`updateAmmo`/`updateCurrency` Actions that `UIHUDSystem` already subscribes to.

## Central hubs & cycles (must be broken)
| Cycle | Direction A | Direction B | Break mechanism |
|---|---|---|---|
| UI ↔ Actors | `UIHUDSystem` reads `Player.Health/Ammo/Currency` | `PlayerRanged`/`ItemPickup`/`Hen`/`Player` call `UIHUDSystem.Instance` | Interfaces in Core (`IPlayerState`) via `Services.Player`; `Services.Hud` + events for the reverse |
| Save ↔ Player | `SaveData`/`Upgrades` write `Player.*` (13/2 refs) | `Player` reads `SaveData.Current` | `Services.Player` interface; Player writes Save directly (Actors→World allowed) |
| Room ↔ Player | `RoomManager` reads `Player.ActivityState` | Player reads `RoomManager.TransitionStyle` | `Services.Player`; `Services.RoomManager` (exists) |
| Room ↔ UI | `RoomManager` drives `Overlay`/`OverlayLoading`/`Music` fades | `AreaTitle` reads `RoomManager` | Overlay fade routines injected via `Services`/`TransitionData` (seam exists); App wires them |
| Dialog ↔ Player | `ConversationManager` → `Player.StateMachine` | `PlayerHealth` reads `ConversationManager.instance` | `Services.Player` interface; `Services.Dialog` for the reverse |
| Physics → Gameplay | `PhysicsBody` reads `Gameplay.GameState` | (chained via Player) | Swap to existing `Services.Gameplay.GameState` |
| DamageSystem ↔ Player | `PlayerProjectile`/`PlayerLassoProjectile` reference `Player` | `PlayerHealth : Health` | **Move** player projectiles out of DamageSystem into Actors |
| Cameras → Player | `Cameras` reads `Player.Transform` | `Player`/`PlayerRanged` read `Cameras.*` | `Cameras` lives in App (top layer) — allowed to reference down |

## Namespace landmines (fix during split)
- `Systems/Input/PlayerActions.cs` — declared in `namespace UnityEngine` (pollution → ambiguity across assemblies).
- `Systems/Dialog_System/TMP_Animated.cs` — declared in `namespace TMPro`.

## Obsolete code (exclude / delete before splitting)
- `Assets/Scripts/Obsolete/` (13 files incl. `GameplayProxy`, `ZoneSystem/*`, `PlayerProxy`), `IInteractable` `[Obsolete]`, `ChaseEB` `[Obsolete]`, `PlayerGroundedMovement_Old`. Confirm removal or quarantine so they don't force spurious references.

# Target Architecture — Layered Assemblies

Dependencies point **downward only**. `UI` and `Actors` are **peers** (no cross-reference).

```
Layer 5  RageRooster.App          (Gameplay, Cameras, GlobalState, bootstrap, Services wiring)
              │  references everything below; NOTHING references App
Layer 4  RageRooster.UI  ┄┄peers┄┄  RageRooster.Actors
              │                          │   (Player, Enemies, Bosses, Collectibles,
              │                          │    entity components, player projectiles)
Layer 3  RageRooster.World         (RoomSystem, SaveSystem, Dialog, Cutscene, spawners, environment)
              │
Layer 2  RageRooster.Systems       (Input, Physics, Settings, TargetingSystem)
              │
Layer 1  RageRooster.Core          (shared data + interfaces: Attack/TagSet/IDamagable/IAttackSource,
              │                      Health base, shared entity components, Destination/SceneReference,
              │                      IPlayerState/IHud/ISave interfaces)
Layer 0  Foundation (existing)     (Utilities [+Services locator], Audio, plugins)
```

**Reference matrix (who may reference whom):**
- `Core` → Foundation only.
- `Systems` → Core, Foundation.
- `World` → Systems, Core, Foundation. (NOT Actors/UI — inverted via Services/interfaces.)
- `Actors` → World, Systems, Core, Foundation. (Enemy→Player OK, same assembly.)
- `UI` → World, Systems, Core, Foundation. (NOT Actors — inverted.)
- `App` → all.

**Assembly contents (folder → assembly):**
| Assembly | Primary folders / files |
|---|---|
| `RageRooster.Core` | `Entities/DamageSystem` (minus player projectiles), shared components (`RagdollHandler`, `ColorTintAnimation`, `MovementAnimator`, `CullableEntity`, `EntityActivity`, `Grabbable`), new `Core/Interfaces` (`IPlayerState`, `IHud`, `IDialog`, etc.), room/save data types shared cross-layer (`Destination`, `SceneReference`) |
| `RageRooster.Systems` | `Systems/Input`, `Systems/Physics`, `Systems/Settings`, `Systems/TargetingSystem` |
| `RageRooster.World` | `Systems/RoomSystem`, `Systems/SaveSystem`, `Systems/Dialog_System`, `Systems/Cutscene System`, spawners (`LootSpawner`, `EnemyLootSpawner`, `WaveController`, `EntitySpawn`), environment loose files |
| `RageRooster.Actors` | `Entities/PlayerScripts`, `Entities/EnemyBehaviors`, `Entities/EnemyCore`, `Entities/Collectibles`, most `Entities/` loose files, moved player projectiles |
| `RageRooster.UI` | `Assets/Scripts/UI/*` (+ `TweenEffects`) |
| `RageRooster.App` | `Systems/Gameplay` (`Gameplay`, `Cameras`, `GlobalState`, `Music`), `ScriptableObjects/GlobalPrefabs`, scene bootstrap, all `Services` registrations |
| `*.Editor` | Editor-only code (new per-assembly editor asmdefs where custom editors exist) |

> Exact folder-to-assembly assignment for **loose files** is finalized in Step 2 (audit) before any asmdef is created.

# Implementation Steps

> **Guiding principle:** Break every cycle *while the project is still a single assembly and still compiles*, THEN carve assemblies bottom-up. Never create an asmdef until the code it will contain no longer references upward. Compile after every step (Verification section).

### Step 0 — Baseline & safety net
- **Description:** Ensure clean starting point: full compile with zero errors/warnings baseline (`Unity.GetConsoleLogs`), commit/VCS checkpoint, capture a manual smoke-test checklist of current behavior (new game, load save, room transition, take damage, respawn, HUD updates, pause menu, dialog, enemy combat, boss). Confirm `Obsolete/` can be deleted or excluded.
- **Assigned role:** developer
- **Dependencies:** None
- **Parallelizable:** No (gate for everything)

### Step 1 — Fix namespace landmines & remove obsolete code
- **Description:** Move `PlayerActions.cs` out of `namespace UnityEngine` and `TMP_Animated.cs` out of `namespace TMPro` into appropriate `RageRooster.*` namespaces (add `using`s at call sites). Delete or quarantine `Assets/Scripts/Obsolete/` and confirmed-dead files (`PlayerGroundedMovement_Old`, `[Obsolete]` `ChaseEB`, `IInteractable` if unused). Verify no prefab/scene references break.
- **Assigned role:** developer
- **Dependencies:** Step 0
- **Parallelizable:** No (small, foundational)

### Step 2 — Full loose-file & dependency audit; finalize assembly assignment
- **Description:** For every loose file in `Entities/`, `Systems/`, `UI/`, `Utilities/`, `ScriptableObjects/`, confirm its target assembly per the matrix. Confirm `Physics` has no entity refs except the `Gameplay.GameState` check; confirm `Input`/`Settings`/`TargetingSystem` are dependency-clean for Layer 2. Produce the definitive folder→assembly map and the exact list of files that must MOVE (player projectiles out of DamageSystem; any misfiled scripts).
- **Assigned role:** explorer
- **Dependencies:** Step 1
- **Parallelizable:** Yes (can run alongside Step 3 design)

### Step 3 — Expand the `Services` locator + define Core interfaces (NO asmdefs yet)
- **Description:** In `Assets/Scripts/UtilityCore/Services/` (or a new `Core/Interfaces` folder destined for `RageRooster.Core`), add:
  - **Static-access services:** `Services.Player` (exposes `IPlayerState`), `Services.Hud` (hitmarker/ammo/health callbacks — replaces direct `UIHUDSystem.Instance` from Actors), `Services.Overlay` (fade routines for Room), `Services.Dialog` (`inDialogue`, `UnCutscene`), and extend `Services.Gameplay` if needed.
  - **Instance interfaces (Core):** `IPlayerState` (Transform, Center, Health/Ammo/Currency read + update events, ActivityState), `IHud`, `IDialog`. These describe the instance functionality UI/World/Systems currently reach for on the concrete `Player`/`UIHUDSystem`.
  - Nothing consumes these yet; project still compiles as one assembly.
- **Assigned role:** developer
- **Dependencies:** Step 1
- **Parallelizable:** Yes (with Step 2)

### Step 4 — Invert upward calls (still one assembly, must keep compiling)
- **Description:** Rewrite the ~19 upward-calling files to use the new seams:
  - `SaveData`(13), `Upgrades`(2), `RoomManager`(2), `RoomEntrance`(5), `SpeakerScript`(4), `DialogueTrigger`(3), `ConversationManager`(1), `TunnelTransition`(2), `SpawnPoint`/`CheckPoint`, `WaveController`(4), `PlayerEnterTrigger*` → replace `Player.*` with `Services.Player`/`IPlayerState`.
  - `PlayerRanged`, `ItemPickup`, `Hen`, `Player` → replace `UIHUDSystem.Instance` with `Services.Hud`/events.
  - `PhysicsBody` → replace `Gameplay.GameState` with `Services.Gameplay.GameState`.
  - `RoomManager` overlay/music driving → route via `Services.Overlay` / injected `TransitionData` routines.
  - `MovementAnimator` `Player.Transform` (Core-bound component) → `Services.Player.Transform`.
  - `ConversationManager`↔`PlayerHealth` → `Services.Player`/`Services.Dialog`.
  - Register all implementations in `App` (Gameplay/bootstrap `InitServices`-style methods).
- **Assigned role:** developer
- **Dependencies:** Steps 2, 3
- **Parallelizable:** No (touches many interdependent files; verify compile continuously)

### Step 5 — Move player projectiles out of DamageSystem
- **Description:** Move `PlayerProjectile.cs`, `PlayerLassoProjectile.cs` (and any player-specific weapon scripts) from `Entities/DamageSystem` into `Entities/PlayerScripts` (destined for Actors). Confirm the remaining DamageSystem (`Attack`, `TagSet`, `IDamagable`, `IAttackSource`, `Health`, `DamageReciever`, `AttackSource*`) has **zero** `Player`/enemy concrete references — this makes Core clean.
- **Assigned role:** developer
- **Dependencies:** Step 4
- **Parallelizable:** No

### Step 6 — Carve `RageRooster.Core` assembly
- **Description:** Create `RageRooster.Core.asmdef` over the damage model + shared entity components + Core interfaces + shared data types. Reference only Foundation assemblies (Utilities, plugins: `SLS.StateMachineH`, `AYellowpaper.*`, DOTween, EditorAttributes, UltEvents). Namespace contents `RageRooster.Core.*`. Fix resulting `using`s. **Compile must pass.**
- **Assigned role:** developer
- **Dependencies:** Step 5
- **Parallelizable:** No (each carve gates the next)

### Step 7 — Carve `RageRooster.Systems` assembly
- **Description:** Create `RageRooster.Systems.asmdef` over Input, Physics, Settings, TargetingSystem. References: Core + Foundation. Namespace `RageRooster.Systems.*` (reconcile with existing `RageRooster.Physics`/`RageRooster.Settings`). Compile must pass.
- **Assigned role:** developer
- **Dependencies:** Step 6
- **Parallelizable:** No

### Step 8 — Carve `RageRooster.World` assembly
- **Description:** Create `RageRooster.World.asmdef` over RoomSystem, SaveSystem, Dialog, Cutscene, spawners, environment. References: Systems + Core + Foundation (NOT Actors/UI). Verify no leftover `Player`/`UIHUDSystem` direct refs (should all be via Services from Step 4). Namespace per sub-domain. Compile must pass.
- **Assigned role:** developer
- **Dependencies:** Step 7
- **Parallelizable:** No

### Step 9 — Carve `RageRooster.Actors` assembly
- **Description:** Create `RageRooster.Actors.asmdef` over PlayerScripts, EnemyBehaviors, EnemyCore, Collectibles, entity loose files, moved projectiles. References: World + Systems + Core + Foundation. Enemy→Player intra-assembly (fine). Namespace `RageRooster.Actors.*`. Compile must pass.
- **Assigned role:** developer
- **Dependencies:** Step 8
- **Parallelizable:** Can overlap with Step 10 (UI) — both are Layer 4 peers, but do sequentially first time to isolate errors.

### Step 10 — Carve `RageRooster.UI` assembly
- **Description:** Create `RageRooster.UI.asmdef` over `Assets/Scripts/UI/*`. References: World + Systems + Core + Foundation (NOT Actors). Verify HUD reads Player only via `Services.Player`/`IPlayerState`, and Actors→HUD is via `Services.Hud`. Namespace `RageRooster.UI.*`. Compile must pass.
- **Assigned role:** developer
- **Dependencies:** Step 9 (so remaining Actors↔UI refs are already inverted)
- **Parallelizable:** No

### Step 11 — Carve `RageRooster.App` assembly (composition root)
- **Description:** Whatever remains in `Assembly-CSharp` (Gameplay, Cameras, GlobalState, Music, GlobalPrefabs, bootstrap) becomes `RageRooster.App.asmdef`, referencing all lower assemblies. Consolidate all `Services` registrations here (`InitServices`). Confirm nothing references App. Namespace `RageRooster.App`/`RageRooster.Systems`. Compile must pass with **empty** `Assembly-CSharp`.
- **Assigned role:** developer
- **Dependencies:** Step 10
- **Parallelizable:** No

### Step 12 — Editor assemblies & final cleanup
- **Description:** Create `*.Editor` asmdefs for any custom editors that now live in gameplay assemblies (e.g. `Gameplay.Editor`), wrap remaining `#if UNITY_EDITOR` editor types appropriately. Verify no `autoReferenced` leaks, no accidental circular asmdef references, and that incremental compile of a single assembly no longer rebuilds the whole project.
- **Assigned role:** developer
- **Dependencies:** Step 11
- **Parallelizable:** No

# Verification & Testing

## After every step (mandatory)
- `Unity.GetConsoleLogs` (errors + warnings) → **zero new compile errors** before proceeding.
- Confirm no missing-script warnings introduced (moved files keep `.meta`/GUIDs — always move via Unity/asset-aware ops so GUIDs are preserved and prefab/scene references survive).

## Cycle / architecture checks
- After each carve, attempt to add a *disallowed* reference mentally/tooling-wise: e.g. confirm `RageRooster.UI` does **not** list `RageRooster.Actors` in its asmdef references and still compiles. If it fails to compile without that reference, a cycle was missed → return to Step 4 seam work.
- Confirm `Assembly-CSharp` is empty (or only truly leftover glue) at the end.

## Behavioral smoke test (must match Step 0 checklist — zero change)
1. Boot `Intro` scene → main menu → new game and load-save both reach gameplay.
2. HUD updates: take damage (health bar), fire ranged (ammo + hitmarker), collect currency/collectible.
3. Room transition (walk between rooms; tunnel transition; fade overlays play).
4. Save at a checkpoint, reload save, respawn after death, pit-fall respawn.
5. Enter dialog (player state locks, pause disabled, resumes correctly).
6. Enemy combat + at least one boss (enemy reads Player position/state; ragdoll; loot spawns).
7. Pause menu (pause/resume, settings, remapping), quit-to-menu (`EndGame`).
8. Physics: movement, moving platforms, grab/lasso, ground slam.

## Compile-time win validation
- Touch a single UI script and a single Actors script; confirm Unity recompiles **only** that assembly (+ App), not all 262 files. Record before/after incremental compile time as the success metric.

## Risk notes
- **GUID preservation:** all file moves must retain `.meta` files; verify serialized references in prefabs/scenes (Player prefab, Gameplay prefab, HUD, enemy prefabs) after moves.
- **`InitializeOnLoadMethod`/`DefaultExecutionOrder`:** `Gameplay.InitServices` and execution-order attributes must remain valid after landing in `App`.
- **Serialized cross-assembly types:** moving a `MonoBehaviour`/`[Serializable]` type between assemblies keeps its GUID (fine) but confirm no `[SerializeReference]`/full-type-name-string serialization breaks.
- **Incremental, always-green:** every step ends compiling. If a carve explodes with cross-references, the fix belongs in the Step 4 seam layer, not in loosening asmdef reference rules.
