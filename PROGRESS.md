# PROGRESS.md - Thread Race Final Tracker

## 1. Current Status

**Overall status:** `READY_FOR_SUBMISSION`

**Project:** Thread Race live-event case study for Thread Fever

**Final user state:** Project is complete and ready for company submission.

**Final update date:** 2026-07-03

**Main scene:** `Assets/Scenes/ThreadRace_Main.unity`

**Submission README:** `README.md`

The project is now presented as a finished Unity case-study module, not an in-progress milestone log. Earlier milestone notes, temporary debugging notes, batch-run notes, and crash-recovery details were consolidated into this final tracker so the repository reads cleanly during review.

---

## 2. Final Compliance Summary

| Requirement | Status | Evidence |
|---|---|---|
| Unity 2022.3 LTS | Complete | `ProjectSettings/ProjectVersion.txt` = `2022.3.62f2` |
| URP | Complete | `com.unity.render-pipelines.universal` = `14.0.12`; URP assets under `Assets/Settings` |
| Mobile portrait target | Complete | Main Canvas reference resolution `1080 x 1920`; portrait-oriented UI |
| Main scene | Complete | `Assets/Scenes/ThreadRace_Main.unity` |
| Proper Unity `.gitignore` | Complete | Unity generated folders, IDE files, build outputs, logs, and user settings ignored |
| Required UI Kit | Complete | `Assets/2D Game UI Kit` used for event UI, shop cards, rewards, and visual panels |
| DOTween | Complete | `Assets/Plugins/Demigiant/DOTween`; DOTween-driven popup, button, row, and progress motion |
| Extenject | Complete | `Assets/Plugins/Zenject`; `Version.txt` = `9.1.0` |
| Five racers | Complete | Player plus four AI racers in `RaceEventConfigAsset.asset` |
| Finish line at 10 | Complete | `_finishTarget: 10` in `RaceEventConfigAsset.asset` |
| Success advances player | Complete | Covered by gameplay rules/tests |
| Fail does not advance player | Complete | Covered by gameplay rules/tests |
| Independent AI progress | Complete | Central `RaceSimulationDriver : ITickable` plus deterministic AI race plans |
| Countdown/event timer | Complete | UTC-backed timing state and countdown presentation |
| Offline catch-up | Complete | Running saves apply deterministic elapsed-UTC AI catch-up |
| Expiration DNF/no reward | Complete | `RaceCompletionReason.EventExpired` and no-reward result path |
| Dynamic ranking/overtakes | Complete | Rank snapshots, lane reorder, row/rank punch feedback |
| Rewards only rank 1-3 | Complete | Config-driven reward tiers for ranks 1, 2, and 3 |
| No reward after top-three slots fill / DNF | Complete | Reward eligibility requires player finish before all reward positions are filled |
| AI usage documented | Complete | README and AI workflow log document tools, corrections, and verification |
| README | Complete | `README.md` rewritten as final external-facing submission document |

---

## 3. Technical Baseline

| Area | Final state |
|---|---|
| Unity | `2022.3.62f2 (7670c08855a9)` |
| Render pipeline | Universal Render Pipeline |
| URP package | `14.0.12` |
| UI | uGUI + TextMeshPro |
| Animation | DOTween |
| Dependency injection | Extenject / Zenject |
| Orientation | Mobile portrait |
| Reference resolution | `1080 x 1920` |
| Namespace root | `ThreadRace` |
| Main scene | `Assets/Scenes/ThreadRace_Main.unity` |
| Build Settings | Thread Race main scene is the enabled project scene |
| Save backend | PlayerPrefs JSON repository behind gameplay contract |
| Time authority | Device UTC, monotonic last-observed guard |

URP remains the approved rendering pipeline. No conversion to the Built-in Render Pipeline was performed.

---

## 4. Final Project Structure

```text
Assets/
  Scenes/
    ThreadRace_Main.unity

  Scripts/
    Core/
      Audio/
      Progress/
      Random/
      Time/

    Gameplay/
      Application/
      Config/
      Contracts/
      Domain/
      Persistence/

    Infrastructure/
      Audio/
      Config/
      Persistence/
      Randomness/
      Time/

    Presentation/
      Animation/
      Models/
      Navigation/
      Presenters/
      Signals/
      Views/

    App/
      Installers/
      Runtime drivers and composition glue

    Tests/
      EditMode/

  Prefabs/
    UI/

  ScriptableObjects/
    RaceEventConfigAsset.asset

  Audio/
    Sounds/

  Fonts/
  Resources/
  Settings/
  Sprites/
  2D Game UI Kit/
  Plugins/
```

The project now uses a flat, review-friendly `Assets` structure instead of burying all feature code under a duplicate wrapper folder.

---

## 5. Assembly and Dependency Boundaries

```text
ThreadRace.Core
      ^
      |
ThreadRace.Gameplay
      ^                         ^
      |                         |
ThreadRace.Infrastructure   ThreadRace.Presentation
          \                 /
           \               /
            ThreadRace.App
```

| Assembly | Responsibility | Notes |
|---|---|---|
| `ThreadRace.Core` | Small shared contracts: time, random, audio cues, host progress | `noEngineReferences: true` |
| `ThreadRace.Gameplay` | Pure rules, config models, runtime state, AI plans, ranking, result, persistence contracts | `noEngineReferences: true`; references only `ThreadRace.Core` |
| `ThreadRace.Infrastructure` | Unity adapters for PlayerPrefs, time, random, ScriptableObject config, audio | Depends on Core + Gameplay |
| `ThreadRace.Presentation` | Views, presenters, models, signals, navigation, DOTween animation | Depends on Core + Gameplay |
| `ThreadRace.App` | Extenject composition, runtime drivers, command router, lifecycle bridges | Depends on all runtime assemblies + Zenject |
| `ThreadRace.Tests` | EditMode coverage for core rules, app bridge, presentation, scene/config validation | Editor-only test assembly |

Gameplay remains Unity-independent and contains no `MonoBehaviour`, uGUI, DOTween, scene reference, or Extenject dependency.

---

## 6. Extenject Composition

### Project Installer

`ThreadRaceProjectInstaller` binds long-lived services:

- `IRaceTimeProvider`
- `IUtcClock`
- `IDeterministicRandomSourceFactory`
- `IRaceSaveRepository`
- `IHostLevelProgressRepository`
- `IHostLevelProgressService`
- `RaceAudioLibrary`
- `IRaceAudioService`
- `IRaceMusicService`
- `IRaceSfxService`
- audio volume/mute services
- `RaceAudioDriver`

### Scene Installer

`ThreadRaceSceneInstaller` binds scene/event objects:

- validated `RaceEventSettings`
- `RaceSaveDataMapper`
- `RaceEventController`
- view interfaces for main menu, entry, HUD, placeholder level, and result
- `RaceUiCommandRouter`
- `LevelResultSource`
- `ILevelResultSource`
- `ILevelResultReporter`
- `RaceFlowPresenter`
- `EntryPopupPresenter`
- `RaceHudPresenter`
- `PlaceholderLevelPresenter`
- `RaceResultPresenter`
- `RaceAudioPresenter`
- `RaceLevelResultListener`
- `RacePresentationWarmup`
- `RacePresentationBootstrap`
- `RaceSimulationDriver`
- `RaceEventTimeDriver`
- Zenject signals

All runtime composition is visible through installers. No custom DI container, service locator, or static singleton service graph was added.

---

## 7. Final Race Configuration

`Assets/ScriptableObjects/RaceEventConfigAsset.asset`

| Field | Final value |
|---|---|
| Save schema | `3` |
| Save key | `ThreadRace.Save.V3` |
| Default seed | `26062026` |
| Demo duration | `1800` seconds |
| Countdown interval | `1` second |
| Finish target | `10` |
| Rewarded positions | `3` |

### Racers

| Racer ID | Display name | Type | Pacing |
|---|---|---|---|
| `player` | `You` | Player | N/A |
| `ai_01` | `Nova` | AI | `Steady` |
| `ai_02` | `Bolt` | AI | `Sprinter` |
| `ai_03` | `Mina` | AI | `Closer` |
| `ai_04` | `Rex` | AI | `Wildcard` |

### Rewards

| Rank | Reward ID | Reward |
|---:|---|---|
| 1 | `thread_race_rank_1_coins` | `1000 Coins` |
| 2 | `thread_race_rank_2_coins` | `500 Coins` |
| 3 | `thread_race_rank_3_coins` | `250 Coins` |

Reward tiers are explicitly authored and fail closed when missing. Gameplay reward data uses IDs and icon IDs; presentation sprites are bound separately by ID.

---

## 8. Final Gameplay Rules

The event lifecycle is:

```text
NotStarted -> Running -> Reward -> Completed
```

Final rules:

- AI and player race progress do not start before Start.
- Success advances the player by exactly one step.
- Fail does not advance the player.
- Progress is capped at the finish target.
- Finish order is recorded only once per racer.
- If the player finishes, the final rank is fixed immediately.
- If all rewarded positions are filled before the player finishes, the player is DNF/no reward.
- If the event expires before the player finishes, the player is EventExpired DNF/no reward.
- Rewards are available only for ranks 1-3.
- A reward is never granted unless the player reached the finish.
- Continue/claim transitions `Reward` to `Completed`.
- Reward claim state is persisted and guarded against double-claim.

---

## 9. AI Simulation and Time

AI is deterministic and centralized:

- One `RaceSimulationDriver : ITickable`.
- No per-AI `Update()`.
- Uses unscaled delta time.
- AI only advances while the phase is `Running`.
- Each AI plan is generated from seed, racer ID, finish target, timing envelope, and authored pacing profile.
- Save/restore persists remaining AI step timers and deterministic random state.
- Offline catch-up uses elapsed UTC and clamps at the event end timestamp.

Device UTC is the time authority because the case study has no backend. Effective event time is monotonic through `max(currentUtc, lastObservedUtc)`, so backwards device-clock changes cannot extend the event.

---

## 10. Persistence

Persistence is versioned behind `IRaceSaveRepository`.

Saved data includes:

- save schema
- phase
- racer progress
- AI timers
- finish order
- player final outcome
- completion reason
- reward claim state
- deterministic random state
- start UTC
- end UTC
- last observed UTC

Restore validation rejects:

- wrong schema
- null or missing collections
- duplicate or unknown racer IDs
- out-of-range progress
- invalid finished/un-finished combinations
- duplicate finish placements
- inconsistent finish order
- invalid AI timers
- reward state that contradicts player outcome
- expired outcomes before the event end timestamp

---

## 11. Presentation and Polish

The UI is scene/prefab-authored and uses the required 2D Mobile Game UI Kit visibly.

Final presentation features:

- Main menu shell with Shop, Home, Leaderboard, and locked Coming Soon entries.
- Thread Race entry button on Home.
- Entry popup with countdown, goal, rewards, Start, and close.
- Sky Race-inspired race HUD with reward podium, message band, five lanes, finish stripe, progress markers, rank badges, and countdown.
- Host-game placeholder flow with Challenge, LevelWin, LevelFail, coin claim, and Back Home.
- Result popup with rank/DNF, expired state, reward/no-reward status, and continue.
- Button press feedback on key uGUI buttons.
- Shared DOTween popup fade + spring-in through `PhaseViewBase`.
- Racer row rank reorder and overtake punch feedback.
- Central audio service with menu/gameplay music, crossfade, one-shot SFX, and preload.
- Lilita TMP font integration for generated presentation text.
- Safe area and portrait layout handling.

Views expose intent and render models. Presenters own subscriptions, flow, and command forwarding. Gameplay does not depend on animation, UI hierarchy, audio, or scene objects.

---

## 12. Host Game Integration Flow

The placeholder Success/Fail screen is decoupled from Thread Race state mutation.

```text
Success / Fail button
        |
PlaceholderLevelView event
        |
PlaceholderLevelPresenter
        |
ILevelResultReporter.Report(LevelResult)
        |
LevelResultSource.ResultReported
        |
RaceLevelResultListener
        |
IRaceEventCommandHandler.ReportLevelResult(LevelResult)
        |
RaceUiCommandRouter
        |
RaceEventController
```

This keeps the event integration-ready for a real host game. The host-game placeholder can be replaced without changing race rules.

---

## 13. Validation Summary

Recorded validation during development:

| Stage | Result |
|---|---|
| Milestone 1 EditMode | 31 Thread Race tests passed; full EditMode suite 452/452 passed |
| Milestone 2 EditMode | 86 Thread Race tests passed; full EditMode suite 507/507 passed |
| Milestone 3 timed-event EditMode | Full EditMode suite 580/580 passed |
| Milestone 3 presentation/balancing EditMode | Full EditMode suite 581/581 passed; 160 Thread Race tests passed |
| Manual Unity verification | User verified final project state as ready for submission |

Primary covered areas:

- Success +1
- Fail +0
- progress cap
- ranking and tie policy
- finish order once
- top-three reward mapping
- no reward after top-three reward slots fill, DNF, or expiration
- player must finish to receive reward
- reward cannot be claimed twice
- state transitions
- deterministic AI continuation
- save/restore round trip
- invalid save rejection
- timed event expiration
- offline catch-up
- level-result event-source bridge
- presenter command flow
- countdown formatting/publishing
- safe-area/navigation behavior
- scene/config validation

---

## 14. Final Milestone Board

| # | Milestone | Final status | Notes |
|---:|---|---|---|
| 0 | Inspect and finalize project setup | Complete | Unity version, URP, dependencies, scene, gitignore, asmdefs |
| 1 | Domain, config, and tests | Complete | Pure gameplay rules, ranking, rewards, deterministic random, tests |
| 2 | Simulation and persistence | Complete | Controller, save/restore, PlayerPrefs repository, central tick driver |
| 3 | UI structure | Complete | Main menu, Entry, HUD, host gameplay, result flow, countdown |
| 4 | Polish | Complete | DOTween motion, button feedback, overtake feedback, audio, warmup |
| 5 | Hardening | Complete | Save validation, reward guards, config validation, architecture split |
| 6 | Submission | Complete | README, AI usage documentation, final tracker cleanup |

---

## 15. File/Feature Highlights

| Area | Representative files |
|---|---|
| Composition | `Assets/Scripts/App/Installers/ThreadRaceProjectInstaller.cs`, `Assets/Scripts/App/Installers/ThreadRaceSceneInstaller.cs` |
| Central tick drivers | `RaceSimulationDriver.cs`, `RaceEventTimeDriver.cs`, `RaceAudioDriver.cs` |
| Gameplay rules | `RaceSession.cs`, `RaceEventController.cs`, `RaceOutcomeResolver.cs`, `RaceFinishTracker.cs`, `RaceRankingService.cs` |
| AI planning | `AiRacePlan.cs`, `AiRacePlanGenerator.cs`, `AiPacingProfile.cs`, `AiPacingStyle.cs` |
| Save/restore | `RaceSaveDataMapper.cs`, `RaceSaveData.cs`, `PlayerPrefsRaceSaveRepository.cs` |
| Config | `RaceEventConfigAsset.cs`, `RaceEventConfigAsset.asset`, `RaceEventConfigAssetEditor.cs` |
| Presentation flow | `RaceFlowPresenter.cs`, `EntryPopupPresenter.cs`, `RaceHudPresenter.cs`, `PlaceholderLevelPresenter.cs`, `RaceResultPresenter.cs` |
| Views | `MainMenuView.cs`, `EntryPopupView.cs`, `RaceHudView.cs`, `PlaceholderLevelView.cs`, `RaceResultView.cs`, `RacerHudRowView.cs` |
| Animation/polish | `PhaseViewBase.cs`, `ButtonPressFeedback.cs`, `LockedNavbarTooltip.cs`, `PlayButtonAttentionAnimator.cs` |
| Audio | `RaceAudioService.cs`, `RaceAudioLibraryAsset.cs`, `RaceAudioPresenter.cs`, `ThreadRaceAudioLibrary.asset` |
| Tests | `Assets/Scripts/Tests/EditMode` |
| Submission docs | `README.md`, `PLAN.md`, `PROGRESS.md` |

---

## 16. AI Workflow Log

| Date | Tool | Task | Output used | Correction / override | Verification |
|---|---|---|---|---|---|
| 2026-06-30 | ChatGPT | Architecture planning | Initial layered architecture and milestone plan | Rejected custom/manual DI; standardized on Extenject | Compared against case brief and project rules |
| 2026-06-30 | Codex | Project setup | Gitignore, folder plan, asmdefs, installers, portrait setup | Deferred fragile scene YAML edits until Unity-authored workflow | File inspection, asmdef graph review, Git status |
| 2026-06-30 | Codex | Domain foundation | Runtime config, deterministic random, race state, ranking, rewards, tests | Corrected test asmdef setup and runner shutdown workflow | Unity EditMode XML, dependency grep, source review |
| 2026-06-30 | Codex | Simulation and persistence | Save model, strict validation, PlayerPrefs repository, controller, central driver | Replaced `System.Random` continuation with explicit deterministic state | EditMode XML, scene/prefab inspection, dependency grep |
| 2026-06-30 | Codex | Presentation flow | View/presenter layer, UI prefabs, command router, signals | Kept UI scene/prefab-authored instead of runtime-generated | Unity setup logs, tests, scene validation |
| 2026-07-01 | Codex | Timed-event correction | UTC timing, offline catch-up, expiration DNF, countdown | Corrected stale V1/no-timing helpers; documented device-clock limitation | Timed-event EditMode pass, save validation tests |
| 2026-07-01 | Codex | Main menu shell | Swipe navigation, navbar, Home live-event entry | Removed template-specific DI/SFX/ExecuteAlways dependencies | Static code/test inspection, scene validation |
| 2026-07-01 | Codex | Thread Fever/HoleCraze visual adaptation | Main-menu sprites, tooltip bubble, font assets | Copied only narrow project-owned visual assets, not foreign project code/services | Asset/GUID inspection, scene validation |
| 2026-07-03 | Codex | Audio | Central audio service, cue library, presenter | Rejected direct HoleCraze `SoundManager` port; implemented project-owned Extenject-visible service | Static inspection, presenter tests |
| 2026-07-03 | Codex | AI pacing | Deterministic race plans and data-authored AI profiles | Replaced min/max-only AI behavior with seed+racerId profile plans | Deterministic tests, source review |
| 2026-07-03 | Codex | Architecture hardening | Split ranking, finish, outcome, snapshot, save snapshot helpers | Reduced `RaceSession` scope without changing public behavior | Gameplay dependency grep, compile/static checks |
| 2026-07-03 | Codex | Level-result decoupling | Event-source/reporter bridge | Removed unused direct `ILevelResultHandler` path | Static grep, command/presenter tests |
| 2026-07-03 | Codex | README | External-facing submission README | Kept claims tied to code/assets/logs; documented correction and verification | Source inspection, markdown review |

The README contains the required AI section: tools used, where AI helped, one concrete correction/override, and how the output was verified.

---

## 17. Decision Log

| Date | Decision | Reason |
|---|---|---|
| 2026-06-30 | Use URP | Project was created from Universal 3D template; URP remains locked |
| 2026-06-30 | Use Extenject | Required dependency and clear composition boundary |
| 2026-06-30 | No custom DI container | Avoid hidden dependency graph and duplicate frameworks |
| 2026-06-30 | Gameplay is Unity-independent | Better testability and clean architecture boundary |
| 2026-06-30 | Scene/prefab-authored UI | Required UI Kit and polished mobile presentation |
| 2026-06-30 | One central AI driver | Avoid per-AI `Update()` and simplify deterministic simulation |
| 2026-06-30 | Versioned save data | Safe restore and schema evolution |
| 2026-06-30 | Device UTC time | No backend exists in the case study |
| 2026-07-01 | Event countdown visible before joining | Better live-ops UX; Start accepts current event window |
| 2026-07-01 | Success/Fail belongs to host flow | Thread Race should integrate with a host game through an abstraction |
| 2026-07-01 | Home Play remains independent | Completed event must not block normal host gameplay |
| 2026-07-01 | Shop/Leaderboard are presentation-only | Improve host shell without adding economy/backend scope |
| 2026-07-02 | Use Lilita TMP font | Improve casual-game visual consistency |
| 2026-07-03 | Project-owned audio service | Avoid importing unrelated HoleCraze services/assumptions |
| 2026-07-03 | Dynamic AI race plans | Fixed min/max intervals felt primitive and predictable |
| 2026-07-03 | AI tuning is authored data | Avoid hidden gameplay presets in code |
| 2026-07-03 | Reward tiers fail closed | Missing reward config should not silently generate fallback rewards |
| 2026-07-03 | Explicit `Reward` phase | Separate result display/claim from completed acknowledgement |
| 2026-07-03 | Host level progress service | Keep presenter focused on UI and move persistence behind service |
| 2026-07-03 | README is final submission document | Company review asks for structure, architecture decisions, AI usage, verification, and one-more-day improvements |
| 2026-07-03 | PROGRESS is final tracker | Clean repository presentation matters for case-study review |
| 2026-07-03 | Race resolves when top-three reward slots are filled | Case brief allowed documenting the end condition; lower placements do not affect rewards after all podium slots are gone |
| 2026-07-03 | Countdown is visible before Start | Present the event as an active Royal Match Sky Race-style live-ops window that the player joins |
| 2026-07-03 | AGENTS countdown requirement aligned with original brief | The case document marks multi-day countdown as nice-to-have and does not require a three-day default |

---

## 18. Final Submission Notes

- `README.md` is the external-facing document for reviewers.
- `PROGRESS.md` is now a clean final tracker, not a raw work journal.
- `PLAN.md` remains the implementation plan and decision context.
- The project intentionally keeps host gameplay, shop, leaderboard, analytics, backend time, and economy as scoped placeholders or future work.
- Demo video is hosted as a submission artifact: [YouTube Shorts](https://youtube.com/shorts/h3hAK5zwKgo?si=MPjAOpBHNaINDq1a).

---

## 19. Final Checklist

- [x] Unity `2022.3.62f2` compatible
- [x] URP retained
- [x] Portrait-oriented UI with `1080 x 1920` reference resolution
- [x] Extenject composition
- [x] No custom DI container
- [x] Gameplay assembly has no Unity dependency
- [x] Required UI Kit visibly used
- [x] Five racers in demo config
- [x] Finish target is ten
- [x] Success advances player by one
- [x] Fail does not advance player
- [x] Independent deterministic AI
- [x] Central AI tick driver
- [x] Ranking and overtake feedback
- [x] UTC countdown and offline catch-up
- [x] Event expiration DNF/no reward
- [x] Rewards only ranks 1-3
- [x] Rank 4/5/DNF no reward
- [x] Player must finish to receive reward
- [x] Versioned save/restore
- [x] Strict invalid-save rejection
- [x] Reward claim guard
- [x] Scene/prefab-authored UI
- [x] DOTween popup/button/racer polish
- [x] Central audio service
- [x] Host level result decoupling
- [x] EditMode tests added
- [x] README complete
- [x] AI workflow documented
- [x] One AI correction documented
- [x] One-more-day improvements documented
- [x] Final tracker cleaned
