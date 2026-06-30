# PROGRESS.md - Thread Race Development Tracker

## 1. Current status

**Overall status:** `MILESTONE_1_COMPLETE`

**Active milestone:** Milestone 2 - Simulation and persistence

**Milestone 0 verification:** `USER_VERIFIED`

**Milestone 1 verification:** `USER_VERIFIED`

**Milestone 1 status:** `COMPLETE`

**Current gate:** Milestone 1 runtime domain/configuration, deterministic AI simulation, ranking, finish resolution, and EditMode tests are complete and manually verified. Milestone 2 is next but has not started; wait for explicit approval before implementation.

**Deadline:** One week from receiving the brief

---

## 2. Confirmed setup

| Item | Status | Notes |
|---|---|---|
| Project root | VERIFIED | `C:\Users\user\Desktop\VPCaseStudy` |
| Unity version | VERIFIED | `2022.3.62f2 (7670c08855a9)` from `ProjectSettings/ProjectVersion.txt`; batch executable `C:\Program Files\Unity\Hub\Editor\2022.3.62f2\Editor\Unity.exe` |
| Manual Unity verification | USER_VERIFIED | User verified Milestone 0 and Milestone 1 import/compile completed in Unity `2022.3.62f2` with no blocking red compile errors |
| URP package | VERIFIED | `com.unity.render-pipelines.universal` = `14.0.12` |
| Rendering pipeline | LOCKED | URP remains active; no migration to Built-in |
| Active Graphics URP asset | RECORDED | `Assets/Settings/URP-HighFidelity.asset` via `ProjectSettings/GraphicsSettings.asset` |
| Current editor quality | RECORDED | Quality index `2` = High Fidelity |
| Android/iPhone default quality | RECORDED | Both default to quality index `1` = Balanced |
| Extenject | INSTALLED | `Assets/Plugins/Zenject`; `Version.txt` = `9.1.0`; package display name `Extenject` |
| DOTween | INSTALLED_SETUP | `Assets/Plugins/Demigiant/DOTween`; `DOTWEEN` scripting define present; standard `DOTweenModuleUI.cs` exists; no DOTween Pro TMP module required |
| TextMeshPro | INSTALLED | Package `3.0.7`; Essentials present at `Assets/TextMesh Pro/Resources/TMP Settings.asset` |
| Required UI Kit | INSTALLED | `Assets/300Mind/2D Game UI Kit`; `Help.pdf` title identifies `2D Mobile Game UI Kit`; no explicit semantic version file found |
| Git repository | COMMITTED | Branch `main`; initial commit `513337d2a30b399e59cdd19f173a1e505fd1c03e`; milestone commits are created only after user approval |
| Unity `.gitignore` | CREATED | `Library/`, `Temp/`, `Logs/`, `Obj/`, `UserSettings/`, generated `.csproj`, generated `.sln`, IDE files, and build outputs ignored |
| Unity generated folders | CONFIRMED | `Library/`, `Logs/`, and `UserSettings/` currently exist; `Temp/` and `Obj/` absent at inspection time |
| Portrait settings | APPLIED | Set through Unity PlayerSettings API; serialized as `defaultScreenOrientation: 0`, `useOSAutorotation: 0`, portrait allowed, upside-down/landscapes disabled |
| ThreadRace folders | CREATED | Approved folder skeleton exists under `Assets/ThreadRace` |
| ThreadRace asmdefs | CREATED | Six planned assemblies exist; `ThreadRace.Gameplay` has `noEngineReferences: true` |
| Milestone 1 domain/config | IMPLEMENTED | Unity-independent runtime config, racer identity/state, lifecycle, ranking, finish resolution, and immutable snapshots |
| Milestone 1 deterministic randomness | IMPLEMENTED | Core random abstraction plus seeded infrastructure implementation; no `UnityEngine.Random` |
| Milestone 1 EditMode tests | USER_VERIFIED_PASS | User verified all 31 ThreadRace EditMode tests passed; batch XML also reported 31 ThreadRace tests and 452/452 full EditMode tests passed |
| Extenject foundations | CREATED | Minimal `ThreadRaceProjectInstaller` and `ThreadRaceSceneInstaller`; no service bindings yet |
| Scene foundation | CREATED | `Assets/ThreadRace/Scenes/ThreadRace_Demo.unity` created through Unity batch-mode Editor automation |
| SceneContext | USER_VERIFIED | Scene has `Zenject.SceneContext` and registered `ThreadRaceSceneInstaller` MonoInstaller |
| ProjectContext | DEFERRED | Not configured in Milestone 0 because no long-lived project services exist yet |
| Build Settings | USER_VERIFIED | `ThreadRace_Demo.unity` is first enabled build scene; SampleScene entry removed from Build Settings without deleting the file |

---

## 3. Assembly graph

| Assembly | References | Notes |
|---|---|---|
| `ThreadRace.Core` | none | `noEngineReferences: true` |
| `ThreadRace.Gameplay` | `ThreadRace.Core` | `noEngineReferences: true`; no Unity/Extenject/DOTween/UI references |
| `ThreadRace.Infrastructure` | `ThreadRace.Core`, `ThreadRace.Gameplay` | Runtime implementations placeholder assembly |
| `ThreadRace.Presentation` | `ThreadRace.Core`, `ThreadRace.Gameplay`, `Zenject` | DOTween reference intentionally deferred until scripts require it |
| `ThreadRace.App` | `ThreadRace.Core`, `ThreadRace.Gameplay`, `ThreadRace.Infrastructure`, `ThreadRace.Presentation`, `Zenject` | Contains installer foundations |
| `ThreadRace.Tests` | `ThreadRace.Core`, `ThreadRace.Gameplay`, `ThreadRace.Infrastructure`, Unity Test Framework | Editor-only, `UNITY_INCLUDE_TESTS`, `nunit.framework.dll`, `TestAssemblies` marker |

No circular references were introduced.

---

## 4. Scene foundation

Scene path:

```text
Assets/ThreadRace/Scenes/ThreadRace_Demo.unity
```

Intended hierarchy created:

```text
ThreadRace_Demo
  SceneContext
  ThreadRaceSceneInstaller
  Main Camera
  EventSystem
  Canvas
    SafeArea
      EntryLayer
      RaceHudLayer
      LevelResultLayer
      ResultLayer
      OverlayLayer
```

Scene details recorded from YAML inspection:

- `SceneContext` has `_monoInstallers` entry referencing `ThreadRaceSceneInstaller`.
- `Main Camera` is tagged `MainCamera`, enabled, orthographic, and uses a solid background.
- `EventSystem` exists with the standard `StandaloneInputModule`; no new Input System package was installed.
- `Canvas` render mode is Screen Space Overlay (`m_RenderMode: 0`) with `CanvasScaler`:
  - `m_UiScaleMode: 1` (Scale With Screen Size)
  - `m_ReferenceResolution: {x: 1080, y: 1920}`
  - `m_ScreenMatchMode: 0` (Match Width Or Height)
  - `m_MatchWidthOrHeight: 0.5`
  - `m_ReferencePixelsPerUnit: 100`
- `SafeArea` and each layer have full-stretch anchors, zero offsets, pivot `0.5, 0.5`, and local scale `1,1,1`.
- No UI Kit visuals, race logic, AI, persistence, rewards, DOTween animations, audio, or VFX were added.

---

## 5. URP findings

| Asset | Path | Renderer | Key settings |
|---|---|---|---|
| Performant | `Assets/Settings/URP-Performant.asset` | `URP-Performant-Renderer` | HDR off, MSAA 1, main shadows off, additional lights off, shadow distance 50, no renderer features |
| Balanced | `Assets/Settings/URP-Balanced.asset` | `URP-Balanced-Renderer` | HDR on, MSAA 1, main shadows on, additional lights per object 2, additional light shadows off, shadow distance 50, SSAO feature active |
| High Fidelity | `Assets/Settings/URP-HighFidelity.asset` | `URP-HighFidelity-Renderer` | HDR on, MSAA 4, main shadows on, additional lights per object 8, additional light shadows on, shadow distance 150, SSAO feature active |

Additional notes:

- Graphics active asset: `Assets/Settings/URP-HighFidelity.asset`.
- Android/iPhone default quality assets: `Assets/Settings/URP-Balanced.asset`.
- Renderer GUIDs:
  - Performant renderer: `707360a9c581a4bd7aa53bfeb1429f71`
  - Balanced renderer: `e634585d5c4544dd297acaee93dc2beb`
  - High Fidelity renderer: `c40be3174f62c4acf8c1216858c64956`
- Template `Assets/Scenes/SampleScene.unity` has a Global Volume using `Assets/Settings/SampleSceneProfile.asset` with Tonemapping, Bloom, and Vignette active.
- `ThreadRace_Demo.unity` has no Volume and no camera post-processing component.
- No URP assets or renderer assets were modified.

Mobile optimization recommendations for later review, not applied in Milestone 0:

- Prefer Balanced or Performant quality for mobile demo verification.
- Disable SSAO unless it visibly improves the final UI/game feel.
- Keep HDR off unless bloom/post effects are intentionally used.
- Keep MSAA low for UI-heavy mobile content.
- Reduce shadow distance and avoid additional light shadows unless needed by final visuals.

---

## 6. Milestone board

| # | Milestone | Status | User verification |
|---:|---|---|---|
| 0 | Inspect and finalize project setup | COMPLETE | USER_VERIFIED |
| 1 | Domain, config, and tests | COMPLETE | USER_VERIFIED |
| 2 | Simulation and persistence | NOT_STARTED | NEXT |
| 3 | UI structure with required UI Kit | BLOCKED | PENDING |
| 4 | Motion, audio, VFX, polish | BLOCKED | PENDING |
| 5 | Hardening and tests | BLOCKED | PENDING |
| 6 | README, demo, build, submission | BLOCKED | PENDING |

---

## 7. Milestone 0 checklist

### Project inspection

- [x] Read `AGENTS.md`, `PLAN.md`, and `PROGRESS.md`
- [x] Inspect Git status and current diff/state
- [x] Confirm project root path
- [x] Confirm Unity executable path for `2022.3.62f2`
- [x] Confirm Unity-generated folders currently exist
- [x] Confirm URP package version
- [x] Record active URP pipeline asset path
- [x] Record active renderer asset paths
- [x] Record renderer feature status and mobile suitability risks
- [x] Apply portrait project setting through Unity PlayerSettings API
- [x] Confirm scene Canvas reference resolution by YAML inspection
- [x] Confirm safe-area/layer approach by YAML inspection
- [x] Record manual Unity verification

### Dependencies

- [x] Extenject installed and recorded
- [x] DOTween imported/setup recorded
- [x] Required UI Kit imported and recorded
- [x] TextMeshPro Essentials imported
- [x] Vendor folders left untouched
- [x] Extenject OptionalExtras not removed

### Architecture

- [x] Target folders created
- [x] `ThreadRace.Core.asmdef` created
- [x] `ThreadRace.Gameplay.asmdef` created
- [x] Gameplay `noEngineReferences: true`
- [x] `ThreadRace.Infrastructure.asmdef` created
- [x] `ThreadRace.Presentation.asmdef` created
- [x] `ThreadRace.App.asmdef` created
- [x] `ThreadRace.Tests.asmdef` created
- [x] Assembly references verified
- [x] No circular references

### Extenject

- [x] `ThreadRaceProjectInstaller` created
- [x] `ThreadRaceSceneInstaller` created
- [x] SceneContext configured through Unity batch-mode Editor API
- [x] SceneContext references `ThreadRaceSceneInstaller`
- [x] ProjectContext intentionally deferred
- [x] No custom DI code exists
- [x] No service locator exists
- [x] No gameplay bindings added

### Scene and settings

- [x] `ThreadRace_Demo.unity` created
- [x] Intended root hierarchy created
- [x] Canvas Scaler configured
- [x] SafeArea and UI layer placeholders created
- [x] Build Settings configured with ThreadRace scene first enabled
- [x] SampleScene removed from Build Settings without deleting the asset
- [x] No vendor demo scenes touched
- [x] User manually verified scene hierarchy, SceneContext, Canvas Scaler, Build Settings, portrait settings, no visible missing scripts, and no Extenject blocking errors

### Git

- [x] Git repo confirmed
- [x] Unity `.gitignore` confirmed
- [x] Generated folders ignored
- [x] Initial commit created: `513337d2a30b399e59cdd19f173a1e505fd1c03e`
- [x] Working tree was clean after the initial commit

---

## 8. Milestone 1 checklist

### Runtime configuration

- [x] Immutable plain C# runtime configuration model added
- [x] Exactly five racers validated
- [x] Exactly one player and four AI racers validated
- [x] Duplicate/empty racer IDs rejected
- [x] Empty display names rejected
- [x] Finish target and rewarded positions validated
- [x] AI delay ranges validated
- [x] Stable initial ordering used for deterministic ties
- [x] No ScriptableObject runtime config adapter added in this milestone

### Domain and application

- [x] `RacerId`, `RacerType`, `RacePhase`, and `LevelResult` added
- [x] Internal mutable racer state kept private to the session
- [x] Immutable racer, ranking, finisher, outcome, and race snapshots added
- [x] Race starts in `NotStarted`
- [x] Start transitions to `Running`
- [x] Invalid state transitions rejected
- [x] Success advances the player by exactly one step
- [x] Fail does not advance the player
- [x] Progress is capped at the configured finish target
- [x] No mutation is allowed after completion

### Deterministic AI simulation

- [x] Core deterministic random abstraction added
- [x] Seeded infrastructure implementation added
- [x] No `UnityEngine.Random` used
- [x] Four AI racers use independent countdown timers
- [x] Large delta time can trigger multiple deterministic AI steps
- [x] Finished AI racers stop progressing
- [x] AI simulation stops after race completion
- [x] Invalid or non-positive generated AI delays are guarded

### Ranking and outcome

- [x] Finished racers rank by recorded finish placement
- [x] Unfinished racers rank by progress descending
- [x] Equal unfinished progress uses stable initial order
- [x] Ranking recalculates after progress mutations
- [x] Finish order is recorded once
- [x] Player finish outcome records real placement
- [x] Reward eligibility requires player finish and rewarded placement
- [x] If reward positions are filled before the player finishes, the player outcome is DNF with no reward

### Tests and validation

- [x] 31 ThreadRace EditMode tests added
- [x] Full EditMode suite run in Unity batch mode
- [x] 452 total EditMode tests passed, 0 failed
- [x] ThreadRace intended tests discovered: 31
- [x] User manually verified Unity import/compile completed with no blocking red console errors
- [x] User manually verified all 31 ThreadRace EditMode tests passed
- [x] No C# compiler errors found in final validation log
- [x] No missing assembly reference errors found in final validation log
- [x] `ThreadRace.Gameplay` remains `noEngineReferences: true`
- [x] `ThreadRace.Gameplay` still references only `ThreadRace.Core`
- [x] No UI, scene wiring, persistence, audio, VFX, or polish implemented

---

## 9. Unity batch-mode log

Unity batch mode was authorized by the user for remaining Milestone 0 scene/project setup.

Final durable results:

- Scene setup was created through Unity Editor APIs, not by manual scene YAML authoring.
- Temporary Editor automation was created under `Assets/ThreadRace/Editor/ThreadRaceMilestone0SceneSetup.cs`.
- Temporary Editor automation was removed after use, along with its `.meta` and empty `Editor` folder.
- Final active setup log: `Logs/ThreadRaceMilestone0SceneSetup.log` (ignored by Git).
- Historical batch logs from intermediate attempts are also under ignored `Logs/`.

Notable batch notes:

- Initial automation attempt used `PlayerSettings.useOSAutorotation`, which is not available in Unity `2022.3.62f2`; the API call was removed and portrait settings were enforced through available PlayerSettings fields.
- Unity's `EditorBuildSettingsScene` API wrote a zero GUID for the new scene entry; `ProjectSettings/EditorBuildSettings.asset` was narrowly corrected to match `ThreadRace_Demo.unity.meta`.
- Final batch log contains Unity licensing/shutdown warnings, but no `error CS`, no "Scripts have compiler errors", and no thrown setup exception in the final setup pass.
- Scene YAML inspection found no `m_Script: {fileID: 0}` missing-script references.

No Play Mode, tests, or build were run.

---

## 10. File change log

| Milestone | Created | Modified | Deleted |
|---|---|---|---|
| Milestone 0 | `.gitignore`; `Assets/ThreadRace` folder skeleton and `.meta` files; six `ThreadRace.*.asmdef` files; `ThreadRaceProjectInstaller.cs`; `ThreadRaceSceneInstaller.cs`; `Assets/ThreadRace/Scenes/ThreadRace_Demo.unity`; `Assets/ThreadRace/Scenes/ThreadRace_Demo.unity.meta` | `ProjectSettings/ProjectSettings.asset`; `ProjectSettings/EditorBuildSettings.asset`; `PROGRESS.md` | none |
| Milestone 0 temporary automation | `Assets/ThreadRace/Editor/ThreadRaceMilestone0SceneSetup.cs` and `.meta`; `Assets/ThreadRace/Editor.meta`; ignored batch logs under `Logs/` | none retained | Temporary Editor automation files removed after successful setup |
| Milestone 1 | `IDeterministicRandomSource.cs`; `SeededRandomSource.cs`; `AiStepTiming.cs`; `RaceConfiguration.cs`; `RacerDefinition.cs`; `RaceSession.cs`; `RacerId.cs`; `RacerType.cs`; `RacePhase.cs`; `LevelResult.cs`; `RacerRuntimeState.cs`; `RacerSnapshot.cs`; `RaceRankingEntry.cs`; `FinishedRacerResult.cs`; `PlayerRaceOutcome.cs`; `RaceSnapshot.cs`; `RaceDomainTests.cs`; matching Unity `.meta` files for each new source file | `Assets/ThreadRace/Tests/ThreadRace.Tests.asmdef`; `PROGRESS.md` | none |

Planning documents `AGENTS.md` and `PLAN.md` were not modified in Milestone 1.

Milestone 1 created file paths:

- `Assets/ThreadRace/Runtime/Core/Random/IDeterministicRandomSource.cs`
- `Assets/ThreadRace/Runtime/Core/Random/IDeterministicRandomSource.cs.meta`
- `Assets/ThreadRace/Runtime/Infrastructure/Randomness/SeededRandomSource.cs`
- `Assets/ThreadRace/Runtime/Infrastructure/Randomness/SeededRandomSource.cs.meta`
- `Assets/ThreadRace/Runtime/Gameplay/Config/AiStepTiming.cs`
- `Assets/ThreadRace/Runtime/Gameplay/Config/AiStepTiming.cs.meta`
- `Assets/ThreadRace/Runtime/Gameplay/Config/RaceConfiguration.cs`
- `Assets/ThreadRace/Runtime/Gameplay/Config/RaceConfiguration.cs.meta`
- `Assets/ThreadRace/Runtime/Gameplay/Config/RacerDefinition.cs`
- `Assets/ThreadRace/Runtime/Gameplay/Config/RacerDefinition.cs.meta`
- `Assets/ThreadRace/Runtime/Gameplay/Application/RaceSession.cs`
- `Assets/ThreadRace/Runtime/Gameplay/Application/RaceSession.cs.meta`
- `Assets/ThreadRace/Runtime/Gameplay/Domain/FinishedRacerResult.cs`
- `Assets/ThreadRace/Runtime/Gameplay/Domain/FinishedRacerResult.cs.meta`
- `Assets/ThreadRace/Runtime/Gameplay/Domain/LevelResult.cs`
- `Assets/ThreadRace/Runtime/Gameplay/Domain/LevelResult.cs.meta`
- `Assets/ThreadRace/Runtime/Gameplay/Domain/PlayerRaceOutcome.cs`
- `Assets/ThreadRace/Runtime/Gameplay/Domain/PlayerRaceOutcome.cs.meta`
- `Assets/ThreadRace/Runtime/Gameplay/Domain/RacePhase.cs`
- `Assets/ThreadRace/Runtime/Gameplay/Domain/RacePhase.cs.meta`
- `Assets/ThreadRace/Runtime/Gameplay/Domain/RaceRankingEntry.cs`
- `Assets/ThreadRace/Runtime/Gameplay/Domain/RaceRankingEntry.cs.meta`
- `Assets/ThreadRace/Runtime/Gameplay/Domain/RacerId.cs`
- `Assets/ThreadRace/Runtime/Gameplay/Domain/RacerId.cs.meta`
- `Assets/ThreadRace/Runtime/Gameplay/Domain/RacerRuntimeState.cs`
- `Assets/ThreadRace/Runtime/Gameplay/Domain/RacerRuntimeState.cs.meta`
- `Assets/ThreadRace/Runtime/Gameplay/Domain/RacerSnapshot.cs`
- `Assets/ThreadRace/Runtime/Gameplay/Domain/RacerSnapshot.cs.meta`
- `Assets/ThreadRace/Runtime/Gameplay/Domain/RacerType.cs`
- `Assets/ThreadRace/Runtime/Gameplay/Domain/RacerType.cs.meta`
- `Assets/ThreadRace/Runtime/Gameplay/Domain/RaceSnapshot.cs`
- `Assets/ThreadRace/Runtime/Gameplay/Domain/RaceSnapshot.cs.meta`
- `Assets/ThreadRace/Tests/EditMode/RaceDomainTests.cs`
- `Assets/ThreadRace/Tests/EditMode/RaceDomainTests.cs.meta`

Milestone 1 modified file paths:

- `Assets/ThreadRace/Tests/ThreadRace.Tests.asmdef`
- `PROGRESS.md`

---

## 11. Test log

| Date | Milestone | Unity version | Test | Result | Notes |
|---|---|---|---|---|---|
| 2026-06-30 | Milestone 0 | 2022.3.62f2 | Manual Unity import/compile before scene setup | USER_REPORTED_PASS | User reported Unity import/compile completed with no blocking red compile errors before this pass |
| 2026-06-30 | Milestone 0 | 2022.3.62f2 | Unity batch-mode scene setup | COMPLETED | Batch-mode Editor automation created/saved scene and settings; no Play Mode/tests/build run |
| 2026-06-30 | Milestone 0 | 2022.3.62f2 | Manual Unity final verification | USER_VERIFIED_PASS | User verified compile/import, scene open, hierarchy, SceneContext installer registration, Canvas Scaler, Build Settings, portrait settings, no visible Missing Script components, and no Extenject blocking errors |
| 2026-06-30 | Milestone 1 | 2022.3.62f2 | Unity batch-mode EditMode tests | PASS | Final command used `-batchmode -projectPath C:\Users\user\Desktop\VPCaseStudy -runTests -testPlatform editmode -testResults Logs/Milestone1-EditModeResults.xml -logFile Logs/Milestone1-EditMode-NoQuit.log`; exit code `0`; result XML reports 452 total, 452 passed, 0 failed; 31 ThreadRace tests discovered and passed |
| 2026-06-30 | Milestone 1 | 2022.3.62f2 | Manual Unity final verification | USER_VERIFIED_PASS | User verified import/compile completed, Console had no blocking red compile errors, ThreadRace EditMode suite was discovered, and all 31 ThreadRace EditMode tests passed |

Milestone 1 validation notes:

- Explicit `-quit` runs exited with code `0` but did not emit a result XML in this environment.
- The successful final run let the Unity Test Runner control shutdown, produced `Logs/Milestone1-EditModeResults.xml`, and exited with code `0`.
- Final validation log scanned clean for `error CS`, "Scripts have compiler errors", "Compilation failed", missing assembly references, and assembly reference errors.

Never replace failed entries. Add a new row after fixes.

---

## 12. AI workflow log

| Date | Tool | Task | Output used | Developer correction/override | Verification |
|---|---|---|---|---|---|
| 2026-06-30 | ChatGPT | Analyze brief and prepare architecture guidance | Initial planning documents | Rejected custom DI/manual fallback; standardized on installed Extenject | Compared with original brief and project rules |
| 2026-06-30 | Codex | Milestone 0 repository and architecture setup | Git initialization, Unity `.gitignore`, folder skeleton, asmdefs, minimal Extenject installers, portrait setting | Deferred scene/context YAML creation until Unity-authored workflow was approved | Static file inspection, asmdef graph review, `git status --ignored` |
| 2026-06-30 | Codex | Milestone 0 scene setup | Unity batch-mode Editor automation for scene, SceneContext, Canvas, Build Settings, and portrait settings | Corrected unavailable `PlayerSettings.useOSAutorotation` API; kept ProjectContext deferred; corrected Build Settings GUID to match scene meta | Batch logs, scene YAML inspection, ProjectSettings inspection, Git status |
| 2026-06-30 | Codex | Milestone 1 gameplay/domain foundation | Runtime config, deterministic random source, race session, AI timers, ranking, finish outcome, EditMode tests | Corrected test asmdef setup by adding `TestAssemblies`, removing duplicate explicit TestRunner references, and using runner-controlled batch shutdown when explicit `-quit` produced no XML | Unity batch-mode EditMode XML, final log scan, asmdef inspection, dependency grep |

The final README must contain at least one concrete correction example.

---

## 13. Decision log

| Date | Decision | Reason | Owner |
|---|---|---|---|
| 2026-06-30 | Extenject is mandatory | Installed and aligned with project rules | Serhat |
| 2026-06-30 | URP is the approved rendering pipeline | Project intentionally created from Universal 3D template | Serhat |
| 2026-06-30 | No custom DI container | Avoid duplicate frameworks and hidden composition | Serhat / architecture review |
| 2026-06-30 | Gameplay assembly is Unity-independent | Strong compile-time boundary and testability | Serhat / architecture review |
| 2026-06-30 | Scene/prefab authored UI | Required UI Kit and polish priority | Serhat / architecture review |
| 2026-06-30 | Do not manually author complex scene/prefab YAML | SceneContext, Canvas, EventSystem, and installer references should be Unity-authored | Codex / Serhat instruction |
| 2026-06-30 | Do not remove Extenject OptionalExtras during Milestone 0 | No confirmed compile error and user approval required | Codex / Serhat instruction |
| 2026-06-30 | Defer ProjectContext | No global project services exist yet; empty project installer does not justify creating ProjectContext now | Codex |
| 2026-06-30 | Do not add DOTween reference to Presentation yet | No presentation scripts require DOTween in Milestone 0 | Codex |
| 2026-06-30 | Keep SampleScene asset, remove it from Build Settings | User required ThreadRace scene first enabled without deleting default scene file | Codex |
| 2026-06-30 | Runtime config stays plain C# in Milestone 1 | ScriptableObject-to-runtime adapter is explicitly deferred to a later milestone | Codex / latest instruction |
| 2026-06-30 | Race outcome resolves to player DNF when reward positions fill before player finish | Latest Milestone 1 instruction requires completion when the player's rewarded outcome is irreversible | Codex / latest instruction |
| 2026-06-30 | Test assembly references Infrastructure | Milestone 1 tests must verify the seeded random infrastructure implementation | Codex |

---

## 14. Remaining non-blocking risks

1. `ThreadRace.Presentation` is still an empty asmdef and Unity may log a nonblocking warning until presentation scripts are added.
2. Explicit `-quit` Unity test runs exit without result XML in this environment; the validated run used Unity Test Runner controlled shutdown and produced XML.
3. Original case-study brief is not copied into the repository by instruction.
4. UI, presentation, persistence, scene wiring, audio, VFX, and polish work are intentionally deferred to later milestones.
5. Active editor quality uses `URP-HighFidelity.asset`; mobile demo should review quality selection because SSAO/HDR/MSAA/shadows are heavier than needed for a UI-first event.
6. Android/iPhone default quality uses `URP-Balanced.asset`; later polish should verify whether SSAO and HDR are worth the mobile cost.

Milestone 2 is next but has not started.

---

## 15. Next valid action

1. Wait for explicit user approval before beginning Milestone 2.
2. Milestone 2 scope: level-result abstraction, application controller integration, save/restore, persistence, and simulation wiring.

Do not start Milestone 2 until the user explicitly approves it.
