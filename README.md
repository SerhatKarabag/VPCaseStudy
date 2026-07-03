# Thread Race Case Study

Thread Race is a production-style Unity live-event module built for a mobile portrait game flow. The project presents a timed race event inside a lightweight host-game shell: the player joins the event, completes placeholder levels, competes against four deterministic AI racers, and receives a reward only when the final result is eligible.

The goal of the case study was not to ship the largest feature set. The goal was to show maintainable Unity/C# architecture, clear gameplay rules, polished mobile UI presentation, deterministic simulation, save/restore safety, and a documented AI-assisted development workflow.

## Quick Facts

| Area | Implementation |
| --- | --- |
| Unity version | `2022.3.62f2` |
| Render pipeline | Universal Render Pipeline, package `14.0.12` |
| UI stack | uGUI + TextMeshPro |
| Target layout | Portrait, Canvas Scaler reference resolution `1080 x 1920` |
| Main scene | `Assets/Scenes/ThreadRace_Main.unity` |
| DI | Extenject / Zenject, installed under `Assets/Plugins/Zenject`, version `9.1.0` |
| Animation | DOTween under `Assets/Plugins/Demigiant/DOTween` |
| Required UI art | `Assets/2D Game UI Kit` |
| Namespace root | `ThreadRace` |
| Demo config | 1 player, 4 AI racers, finish target `10`, rank `1-3` rewards |
| Demo video | [YouTube Shorts](https://youtube.com/shorts/h3hAK5zwKgo?si=MPjAOpBHNaINDq1a) |

## Experience Overview

The project is framed as a real game module rather than a throwaway test scene.

- A main-menu shell presents Shop, Home, Leaderboard, and locked Coming Soon navigation entries.
- The Home page has a persistent host-game Play button and a Thread Race live-event entry button.
- The Thread Race entry shows the live countdown, event goal, and reward preview before the player joins, matching the feel of an already-active live event like Royal Match Sky Race.
- Starting the event opens a Sky Race-inspired HUD with five lanes, a finish stripe, rank badges, progress markers, reward podium, countdown, and overtake feedback.
- The host-game placeholder level flow is separate from the event popup. Success and Fail are accepted through an integration abstraction, then forwarded to the race only if the event is running.
- Success advances the player by one step. Fail does not advance the player.
- AI racers advance independently over time through one centralized simulation driver.
- Ranking updates while racers overtake each other.
- The result popup shows final placement or DNF, reward eligibility, reward claim state, and a top-three podium.
- Completed or expired events remain visible as `ENDED`, and Home Play continues to work because host gameplay is independent from the event lifecycle.

Demo video: [https://youtube.com/shorts/h3hAK5zwKgo?si=MPjAOpBHNaINDq1a](https://youtube.com/shorts/h3hAK5zwKgo?si=MPjAOpBHNaINDq1a)

## Project Structure

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

The structure mirrors the runtime architecture. Gameplay rules live in `Gameplay`; Unity-facing implementations live in `Infrastructure`, `Presentation`, and `App`.

## Assembly Layout

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

Assemblies:

- `ThreadRace.Core`: small contracts and low-level abstractions such as time, deterministic random, audio cues, and host-level progress.
- `ThreadRace.Gameplay`: pure race rules, state, config models, save data, ranking, finish resolution, reward eligibility, AI plan generation, and controller use cases.
- `ThreadRace.Infrastructure`: Unity-backed adapters for PlayerPrefs persistence, UTC/time, deterministic random sources, ScriptableObject config conversion, and audio.
- `ThreadRace.Presentation`: uGUI views, presenters, render models, navigation, DOTween animation, and Zenject signal payloads.
- `ThreadRace.App`: Extenject installers, runtime tick drivers, lifecycle observer, command router, and event-source bridges.
- `ThreadRace.Tests`: EditMode coverage for domain, application, persistence, presentation, navigation, and scene/config validation.

`ThreadRace.Gameplay` and `ThreadRace.Core` both use `noEngineReferences: true`. That keeps business logic independent from `UnityEngine`, scene objects, MonoBehaviours, DOTween, and Extenject.

## Architecture Decisions

### 1. Gameplay Is Unity-Independent

The race rules are written as plain C#:

- `RaceSession`
- `RaceEventController`
- `RaceRankingService`
- `RaceFinishTracker`
- `RaceOutcomeResolver`
- `RaceSaveDataMapper`
- `AiRacePlanGenerator`

This makes the most important rules testable without scene setup, GameObjects, frame timing, or Play Mode dependencies.

### 2. Extenject Owns Runtime Composition

Runtime object graphs are visible in installers:

- `ThreadRaceProjectInstaller` binds long-lived services:
  - `IRaceTimeProvider`
  - `IUtcClock`
  - `IDeterministicRandomSourceFactory`
  - `IRaceSaveRepository`
  - host-level progress persistence
  - audio library and audio services

- `ThreadRaceSceneInstaller` binds scene/event services:
  - `RaceEventSettings`
  - `RaceSaveDataMapper`
  - `RaceEventController`
  - view interfaces
  - command/query router
  - level-result source/reporter
  - presenters
  - central simulation and time drivers
  - Zenject signals

There is no custom DI container and no static singleton access to gameplay services.

### 3. Commands Are Direct, Notifications Are Signals

Commands such as Start, Claim Reward, Resolve Expired Event, and Report Level Result go through `IRaceEventCommandHandler`.

Presentation-wide state changes are published as signals:

- `RaceSnapshotChangedSignal`
- `RaceCountdownChangedSignal`
- `HostGameplayStartedSignal`
- `HostLevelChangedSignal`
- `HostGameplayScreenChangedSignal`
- `HostGameplayCompletedSignal`

This keeps intent explicit while still allowing UI, audio, and flow presenters to react independently.

### 4. Host Level Results Are Decoupled From the Event

The placeholder Success/Fail screen represents the host game. It does not know about race state and does not mutate race progress directly.

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

This is the integration point a real host game would replace.

### 5. ScriptableObject Config Converts to Runtime Data

`RaceEventConfigAsset` is authoring data. Gameplay receives validated runtime models instead of mutable Unity assets.

Configured data includes:

- save schema version and save key
- deterministic default seed
- event duration
- countdown update interval
- finish target
- rewarded positions
- reward tiers with rank, reward ID, type, amount, display text, and icon ID
- player and AI racer definitions
- AI timing envelopes and pacing profile values

The current demo asset is:

- save schema `3`
- save key `ThreadRace.Save.V3`
- seed `26062026`
- event duration `1800` seconds for a short case-study demo window
- countdown interval `1` second
- finish target `10`
- rewards:
  - rank 1: `1000 Coins`
  - rank 2: `500 Coins`
  - rank 3: `250 Coins`
- racers:
  - `player` / `You`
  - `ai_01` / `Nova` / `Steady`
  - `ai_02` / `Bolt` / `Sprinter`
  - `ai_03` / `Mina` / `Closer`
  - `ai_04` / `Rex` / `Wildcard`

Production-duration tuning is data-only. The demo duration can be changed from `RaceEventConfigAsset.asset` without rewriting gameplay code. The countdown is intentionally visible before Start so the event reads as an active live-ops window the player can join, rather than a timer created only after opting in.

## Race Rules

Thread Race uses an explicit lifecycle:

```text
NotStarted -> Running -> Reward -> Completed
```

- `NotStarted`: the event countdown can be displayed in the menu and entry popup; race progress has not started.
- `Running`: player Success/Fail and AI simulation are accepted.
- `Reward`: the player outcome is final and the result popup is shown.
- `Completed`: the result flow has been acknowledged and any eligible reward has been claimed.

Resolution rules:

1. Finish target is `10` successful steps in the submitted demo.
2. The player advances exactly one step on Success.
3. The player does not advance on Fail.
4. Progress is capped at the finish target.
5. A racer's finish position is recorded only the first time it reaches the finish.
6. If the player reaches the finish, their final rank is locked immediately.
7. The exact race-end moment is intentionally documented here because the brief leaves that decision to the implementation.
8. This implementation is outcome-driven: once the top-three reward positions are filled before the player finishes, the player's reward outcome is irreversible, so the event resolves immediately as DNF/no reward.
9. The event does not continue simulating just to assign cosmetic 4th/5th ranks after the reward outcome is already known.
10. If the timer expires before the player finishes, the player is EventExpired DNF/no reward.
11. Only ranks `1-3` map to rewards.
12. A reward is never granted unless the player reached the finish.
13. Reward claiming is guarded so it cannot be claimed twice.

## AI Simulation

AI movement is deterministic, centralized, and data-driven.

- `RaceSimulationDriver : ITickable` is the only per-frame simulation driver.
- AI advances only while the race phase is `Running`.
- Simulation uses `IRaceTimeProvider.UnscaledDeltaTime`.
- Each AI racer gets a deterministic race plan generated from event seed, racer ID, finish target, timing envelope, and pacing profile.
- Pacing profiles support steady, early, late, volatile, and final-push race shapes without player-reactive cheating.
- Save/restore persists remaining AI step timers and deterministic random state.
- Offline catch-up uses elapsed UTC time and replays deterministic AI progression up to the event end timestamp.

The result is predictable enough to test and replay, but varied enough that the same opponent does not always win when seed/config changes.

## Time and Persistence

The project uses device UTC because there is no backend authority in this case study.

Important safeguards:

- effective time is monotonic through `max(currentUtc, lastObservedUtc)`
- backwards device-clock changes do not extend the event or create negative offline progress
- forward clock jumps may advance or expire the event, which is an accepted offline-only limitation
- running events catch up AI progress from persisted UTC elapsed time
- catch-up is clamped to the configured event end timestamp

Save data is versioned and validated through `RaceSaveDataMapper`.

Persisted state includes:

- schema version
- phase
- racer progress
- AI step timers
- finish order
- player outcome
- completion reason
- reward claim state
- deterministic random state
- timing state: start UTC, end UTC, last observed UTC

Restore rejects invalid data such as:

- unsupported schema
- missing or duplicate racer IDs
- progress outside valid range
- contradictory finished/un-finished state
- duplicate finish placements
- reward eligibility that contradicts final rank
- completed eligible result without reward claimed
- expired result before the end timestamp
- invalid AI timers

## Presentation and Polish

The UI is scene/prefab-authored, not built as a full runtime-generated UI. Views expose user intent and render supplied models; presenters own subscription and flow logic.

Key presentation pieces:

- `MainMenuView`: swipeable host shell with bottom navigation.
- `EntryPopupView`: event intro, goal, reward preview, live countdown, Start and close.
- `RaceHudView`: five racer rows, ranking, progress markers, finish stripe, countdown, reward podium.
- `PlaceholderLevelView`: host challenge, LevelWin, LevelFail, and coin claim flow.
- `RaceResultView`: final placement, DNF/expired state, reward text, claim/continue.
- `PhaseViewBase`: shared unscaled DOTween popup fade + spring-in/dismiss animation.
- `ButtonPressFeedback`: runtime pointer-down/up scale feedback for key uGUI buttons.
- `RacerHudRowView`: rank reorder motion, progress marker tweening, overtake/rank punch feedback.
- `RaceAudioPresenter` + `RaceAudioService`: menu/gameplay music, crossfade, one-shot SFX, preload to reduce first-click hitch.

Polish decisions stayed presentation-only. Gameplay rules do not depend on animation completion, audio playback, UI hierarchy, or scene object references.

## Testing and Verification

The project includes EditMode tests under `Assets/Scripts/Tests/EditMode`.

Coverage areas:

- player Success and Fail behavior
- progress caps
- deterministic ranking and tie handling
- finish order recorded once
- reward mapping for ranks 1-3
- no reward after top-three reward slots fill, DNF, or expired state
- reward cannot be claimed twice
- valid and invalid state transitions
- deterministic AI continuation
- save/restore round trip
- invalid save rejection
- timed event expiration and offline catch-up
- level-result event-source bridge
- presenter command flow
- countdown formatting and publishing
- safe-area and navigation behavior
- scene/config validation

Recorded development validation:

- Milestone 1: `31` Thread Race EditMode tests passed.
- Milestone 2: `86` Thread Race EditMode tests passed.
- Milestone 3 timed-event pass: `580/580` full EditMode suite passed.
- Milestone 3 presentation/balancing pass: `581/581` full EditMode suite passed, including `160` Thread Race tests.

Run the Unity Test Runner from the Editor after any final local changes before packaging the submission.

## How to Run

1. Open the project in Unity `2022.3.62f2`.
2. Open `Assets/Scenes/ThreadRace_Main.unity`.
3. Let Unity import and recompile.
4. Press Play.
5. From Home, tap the Thread Race live-event button to view Entry.
6. Press Start to join the race.
7. Use Home Play to complete placeholder levels.
8. Use Success/Fail, then Claim or Back Home.
9. Watch the race HUD update with player progress, AI movement, ranking changes, and eventual result.

Recommended validation before recording:

- clear or complete any existing saved event state
- start a fresh event
- verify Success advances player once
- verify Fail does not advance player
- verify AI movement starts only after Start
- verify countdown and expiration behavior
- verify rank 1-3 reward and no-reward states when top-three reward slots fill, the player DNFs, or the event expires

## AI Tools Used

AI usage was required for this case study. I used Codex throughout the project as an engineering assistant, but I directed the architecture, reviewed outputs, corrected weak suggestions, and verified the result with code inspection and tests.

Tools and usage:

- Codex: architecture planning, Unity/C# boilerplate, domain rule implementation, test scaffolding, README/AI log documentation, static code review, and refactoring support.
- Unity Editor and Unity Test Runner: compilation, scene/prefab setup validation, manual flow verification, and EditMode test execution.
- Static inspection commands: dependency grep, asmdef review, Git diff review, YAML/asset inspection, and targeted source review.

Where AI helped:

- Generated the initial layered structure and assembly boundaries.
- Drafted pure gameplay models and test cases for race rules.
- Produced save/restore validation paths for corrupted or inconsistent data.
- Helped split presentation views, presenters, signals, and command routing.
- Proposed UI polish passes such as popup spring-in, button feedback, overtake feedback, and audio prewarm.
- Helped summarize implementation decisions into the project documentation.

Concrete correction/override:

- An early implementation direction risked turning the race into a monolithic session/controller and allowing primitive fixed min/max AI timers to define the entire AI behavior.
- I corrected that by splitting responsibilities:
  - `RaceSession` remains the runtime coordinator.
  - ranking moved into `RaceRankingService`.
  - finish-order tracking moved into `RaceFinishTracker`.
  - outcome resolution moved into `RaceOutcomeResolver`.
  - save snapshot creation moved into `RaceSaveSnapshotFactory`.
  - dynamic AI planning moved into `AiRacePlanGenerator`.
- I also rejected hidden code presets for AI styles. The final config stores concrete tuning values per AI racer, and gameplay consumes immutable `AiPacingProfile` data.

How AI output was verified:

- Assembly definitions were checked to ensure `ThreadRace.Gameplay` references only `ThreadRace.Core` and has `noEngineReferences: true`.
- Grep/static review was used to catch forbidden coupling such as stale direct level-result handlers and presentation references inside gameplay config.
- EditMode tests were added or expanded for each risky rule: DNF reward logic, save validation, deterministic AI continuation, countdown behavior, level-result event bridging, and presenter flow.
- Unity logs and result XML were reviewed after test runs.
- Manual UI checks drove corrections to countdown text, popup close flow, host-game Success/Fail separation, Shop/Leaderboard visuals, navbar hit targets, tooltip mirroring, first-click hitch, and double-animation behavior.
- AI-generated ideas were treated as drafts; any output that hid dependencies, weakened event rules, or made UI runtime-generated instead of scene/prefab-authored was rewritten.

## Known Limitations

- Time authority is local device UTC. Without a backend, large forward clock changes can expire or advance the event.
- The host game level is intentionally a placeholder. It exists to prove event integration, not to implement a full puzzle game.
- Shop and Leaderboard are presentation-only shell content. They are included to make the event feel embedded in a real game, not to add economy or backend scope.
- The current demo event duration is short for recording and review. Production duration is data-driven.
- The countdown starts visually before player entry by design, because the event is presented as an active live-ops window inspired by Royal Match Sky Race; pressing Start joins that window and starts race progression.
- The event resolves when the top-three reward positions are filled before the player finishes. It does not keep running for cosmetic lower-rank placement because no additional reward outcome can change.
- There is no analytics/backend reward grant integration. Rewards are represented as local event outcome data.

## What I Would Improve With One More Day

- Add a small PlayMode smoke suite for Entry -> Running -> host Success/Fail -> Result -> Completed.
- Add haptic feedback for Start, Success, overtake, finish, and reward reveal.
- Add dedicated overtake and finish SFX clips instead of reusing generic button/popup sounds.
- Add localization keys for all user-facing strings.
- Add analytics event interfaces for start, progress, rank change, finish, DNF, reward claim, and expiration.
- Add a lightweight debug panel for seed/event reset/time-skip testing, excluded from production builds.
- Add an optional hosted playable/WebGL build in addition to the recorded demo video.
