# PLAN.md — Thread Race Implementation Plan

## 1. Product summary

Build a polished **Thread Race** live event for Thread Fever.

The player competes against four AI opponents. Every successful placeholder level advances the player by one step. A failed level gives no progress. AI racers advance independently over time. The finish line is ten steps. Rankings change as racers overtake each other. Only the first three finishers receive rewards, and the player receives nothing unless the player reaches the finish.

The implementation must look like a production-ready live-event module, not a throwaway prototype.

---

## 2. Evaluation priorities

| Area | Weight | Project response |
|---|---:|---|
| Architecture | 40% | Pure gameplay assembly, Extenject composition, data-driven config, persistence, tests |
| Polish & game feel | 35% | UI Kit, DOTween, overtakes, reward reveal, sound/VFX |
| AI workflow & README | 15% | AI log, correction example, verification notes |
| Brief compliance | 10% | Exact race rules, five racers, ten steps, top-three rewards |

---

## 3. Locked technical decisions

### Engine and presentation

- Unity `2022.3.62f2`
- Universal Render Pipeline (URP), using the Universal 3D project template
- uGUI
- TextMeshPro
- Mobile portrait
- Reference resolution `1080 × 1920`
- Required 2D Mobile Game UI Kit
- DOTween

### Rendering pipeline

- URP is approved and locked for this project.
- Do not migrate to Built-in.
- Use URP only where it improves polish or presentation.
- Keep mobile performance and shader compatibility in mind.
- Avoid unnecessary renderer features and expensive post-processing.
- Record the active URP asset and renderer paths during Milestone 0 inspection.

### Dependency injection

- **Extenject is installed and required**
- No manual DI fallback
- No custom DI container
- No service locator
- Composition through installers

### Architecture

- Assembly-level boundaries
- Unity-independent `ThreadRace.Gameplay`
- Scene-authored/prefab-authored UI
- Presenter/View separation
- Zenject Signals for notifications
- Direct method calls for commands
- ScriptableObject assets converted to immutable runtime config
- One centralized AI simulation driver
- Versioned save/restore
- Deterministic randomness
- EditMode tests for core rules

---

## 4. Explicit race-end decision

The race result is resolved when the player’s outcome becomes irreversible.

Rules:

1. Record a racer’s finish position the first time it reaches the finish step.
2. If the player reaches the finish, the player’s rank is immediately fixed.
3. If the player finishes after three AI racers, the player is rank 4 and receives no reward.
4. If all four AI racers finish before the player, the player becomes rank 5 / DNF.
5. Transition to `Results` immediately after the player result is known.
6. Stop AI simulation after result resolution.
7. Reward ranks 1–3 only.
8. Never reward the player without reaching the finish.

This supports:

- 1st place
- 2nd place
- 3rd place
- 4th place
- 5th / DNF

---

## 5. Assembly architecture

```text
ThreadRace.Core
ThreadRace.Gameplay
ThreadRace.Infrastructure
ThreadRace.Presentation
ThreadRace.App
ThreadRace.Tests
```

### Dependency map

```text
ThreadRace.Core
      ↑
ThreadRace.Gameplay
      ↑                 ↑
Infrastructure      Presentation
          \          /
          ThreadRace.App
```

### Assembly responsibilities

#### `ThreadRace.Core`

- Time contracts
- Random contracts
- Logging contracts
- Shared low-level utilities
- No gameplay rules

#### `ThreadRace.Gameplay`

- Domain state
- Application services
- Race rules
- Ranking
- Finish order
- Rewards
- Runtime config models
- Save-data contracts
- `noEngineReferences: true`

#### `ThreadRace.Infrastructure`

- PlayerPrefs/JSON persistence
- Unity time adapter
- Random implementation
- Audio implementation
- Logging implementation

#### `ThreadRace.Presentation`

- Views
- Presenters
- Zenject signals
- DOTween animation
- UI Kit integration
- VFX/audio triggers

#### `ThreadRace.App`

- Extenject installers
- Scene composition
- Runtime startup
- Scene-level bindings

#### `ThreadRace.Tests`

- EditMode domain/application tests
- Optional PlayMode smoke tests

---

## 6. Folder structure

```text
Assets/
  ThreadRace/
    Runtime/
      App/
        Installers/
          ThreadRaceProjectInstaller.cs
          ThreadRaceSceneInstaller.cs

      Core/
        Time/
        Random/
        Logging/

      Gameplay/
        Domain/
        Application/
        Contracts/
        Config/

      Infrastructure/
        Persistence/
        Randomness/
        Time/
        Audio/
        Logging/

      Presentation/
        Views/
        Presenters/
        Signals/
        Animation/

    Tests/
      EditMode/
      PlayMode/

    Scenes/
      ThreadRace_Demo.unity

    Prefabs/
      UI/
      Racers/
      Effects/

    ScriptableObjects/
      RaceEventConfigAsset.asset
      RacePresentationConfigAsset.asset

    Audio/
    VFX/
    Art/

  ThirdParty/
```

---

## 7. Extenject composition plan

## 7.1 Project installer

`ThreadRaceProjectInstaller`

Bind:

- `IRaceSaveRepository`
- `IRaceTimeProvider`
- `IRaceRandomSourceFactory`
- `IRaceAudioService`
- `IRaceLogger`

These are long-lived technical services.

## 7.2 Scene installer

`ThreadRaceSceneInstaller`

Serialized fields:

- `RaceEventConfigAsset`
- `RacePresentationConfigAsset`
- Entry view
- HUD view
- Level-result view
- Result view
- Racer view collection
- Audio/VFX references where scene-bound

Bind:

- Validated `RaceEventConfig`
- Validated `RacePresentationConfig`
- `RaceRuntimeState`
- `RaceEventController`
- `RaceSimulationService`
- `RaceRankingService`
- `RaceFinishService`
- `RaceRewardService`
- `ILevelResultSource`
- Presenters
- Signals

### Binding strategy

```csharp
Container.Bind<RaceEventConfig>()
    .FromInstance(raceEventConfigAsset.ToRuntimeConfig())
    .AsSingle();

Container.BindInterfacesAndSelfTo<RaceEventController>()
    .AsSingle();

Container.BindInterfacesTo<RaceSimulationDriver>()
    .AsSingle();
```

Use `BindInterfacesAndSelfTo` only where both direct and interface resolution are justified.

---

## 8. Static config design

## 8.1 Race event asset

`RaceEventConfigAsset : ScriptableObject`

Contains:

- Finish step
- Racer definitions
- AI profiles
- Reward tiers
- Save version
- Save key
- Default seed
- Optional event duration

Converts to:

`RaceEventConfig`

The runtime model should be plain C#, validated, and effectively immutable.

## 8.2 Presentation asset

`RacePresentationConfigAsset : ScriptableObject`

Contains:

- Popup durations
- Button punch timings
- Racer movement duration
- Tween ease values
- Overtake feedback
- Finish feedback
- Reward reveal timing
- UI motion tuning
- Audio/VFX references or IDs

Converts to:

`RacePresentationConfig`

Gameplay logic must not depend on presentation timing.

---

## 9. Runtime data model

### `RaceRuntimeState`

- Schema version
- Current event state
- Racer states
- Finish order
- Player result
- Reward claim state
- Simulation state

### `RacerRuntimeState`

- Racer ID
- Is player
- Progress
- Current rank
- Finish position
- Has finished
- Next AI move time
- AI random stream state if needed

### Stable identifiers

Use value types or immutable IDs.

Examples:

```text
RacerId
RewardTierId
RaceSaveVersion
```

Do not identify racers by scene-object references.

---

## 10. Application services

### `RaceEventController`

Owns:

- Event lifecycle
- Start
- Level-result handling
- State transitions
- Result resolution
- Save requests

Does not own:

- UI animation
- Concrete persistence
- AudioSource
- Random implementation

### `RaceSimulationService`

Owns:

- AI schedules
- AI movement due checks
- AI progression requests
- Simulation start/stop

### `RaceRankingService`

Owns:

- Rank recalculation
- Tie policy
- Ranking snapshots

### `RaceFinishService`

Owns:

- Finish-order recording
- Irreversible outcome detection
- DNF resolution

### `RaceRewardService`

Owns:

- Rank-to-reward mapping
- Eligibility checks
- Claim guard

### `RaceSaveDataMapper`

Owns:

- Runtime state -> save data
- Save data -> validated runtime state

Avoid combining all responsibilities into one oversized session class.

---

## 11. Level-result flow

```text
Success / Fail button
        ↓
LevelResultView
        ↓
LevelResultPresenter
        ↓
ILevelResultSource
        ↓
RaceEventController
```

`LevelResultView` only exposes button events.

`LevelResultPresenter` converts button intent into `LevelResult`.

The race controller receives the result through the abstraction.

This makes the event integration-ready for a real host game.

---

## 12. Signal plan

Recommended Zenject Signals:

```text
RaceStartedSignal
RacerProgressChangedSignal
RankingChangedSignal
RacerFinishedSignal
RaceResolvedSignal
RewardClaimedSignal
```

Example:

```csharp
public readonly struct RacerProgressChangedSignal
{
    public RacerId RacerId { get; }
    public int PreviousProgress { get; }
    public int CurrentProgress { get; }
}
```

Use direct method calls for commands such as:

- Start race
- Report level result
- Claim reward
- Continue

Use signals for notifications consumed by multiple systems.

---

## 13. AI simulation plan

### Profiles

Each AI profile contains:

- Racer ID
- Display name
- Avatar/icon
- Initial delay
- Minimum move interval
- Maximum move interval
- Pause chance
- Seed offset
- Optional pacing bias

### Central simulation

Use one `RaceSimulationDriver : ITickable`.

Pseudo-flow:

```text
Tick
  For each AI
    if Racing and now >= next move time
      decide whether to advance
      advance by one
      schedule next move
      recalculate ranking
      save state
```

Requirements:

- Unscaled time
- No per-AI `Update()`
- No per-frame LINQ
- No per-frame allocations
- Deterministic with fixed seed
- AI stops outside Racing

---

## 14. Tie policy

Ranking ties must be deterministic.

Recommended policy:

1. Finished racers are ordered by finish position.
2. Unfinished racers are ordered by progress descending.
3. If progress is equal:
   - Preserve previous relative order, or
   - Use stable racer order

Choose one and document it.

Preferred for visual stability:

- Preserve previous relative order while progress is tied.

This avoids UI flicker.

---

## 15. Persistence plan

### Contract

```csharp
public interface IRaceSaveRepository
{
    bool TryLoad(out RaceSaveData data);
    void Save(RaceSaveData data);
    void Clear();
}
```

### Save data

Include:

- Version
- Event state
- Racer progress
- Finish order
- Player result
- Reward claim state
- AI next-move times
- Seed/random state
- Entry accepted state

### Save triggers

- Start
- Player success
- AI move
- Racer finish
- Result
- Reward claim
- Pause/focus loss/quit

### Restore validation

Reject or safely reset when:

- Version is unsupported
- Racer count mismatches
- Racer IDs are missing/duplicated
- Progress is out of range
- Finish order contains duplicates
- Reward state contradicts rank/result

No offline progression in core scope.

---

## 16. UI plan

## 16.1 Entry popup

Uses required UI Kit.

Shows:

- Event title
- Description
- Race target
- Reward preview
- Start button

Polish:

- Overlay fade
- Popup spring-in
- Staggered content
- Button punch

## 16.2 Race HUD

Shows:

- Five racers
- Ten-step progress
- Finish line
- Current rank
- Prize tiers
- Overtakes

Use five pre-created racer views.

Do not instantiate/destroy racers during ranking changes.

## 16.3 Placeholder level panel

Shows:

- Success
- Fail

Optional development-only reset button can exist behind a debug flag.

## 16.4 Result popup

Shows:

- Rank or DNF
- Reward if eligible
- No-reward state otherwise
- Continue button

Reward reveal is the strongest polish moment.

---

## 17. Presenter/View plan

Recommended:

```text
EntryPopupView + EntryPopupPresenter
RaceHudView + RaceHudPresenter
LevelResultView + LevelResultPresenter
ResultPopupView + ResultPopupPresenter
RewardRevealView + RewardRevealPresenter
```

Presenter responsibilities:

- Subscribe to signals/application state
- Build render snapshots
- Call view methods
- Forward user intent
- Dispose/unsubscribe safely

View responsibilities:

- Serialized references
- User events
- Render methods
- Animation triggers

---

## 18. UI motion plan

### Entry

- Fade overlay
- Scale from ~0.82 to 1
- Back/overshoot ease
- Stagger rewards and Start button

### Buttons

- Pointer down scale
- Pointer up bounce
- Disabled visual state
- Input lock during transitions

### Racer movement

- Animate between progress markers
- Slight anticipation
- Smooth landing
- Small step particle

### Overtake

- Rank badge punch
- Glow
- Lane/card reorder animation
- Subtle displaced-racer reaction

### Finish

- Stronger particle
- Audio impact
- Finish-line reaction

### Reward

- Medal drop
- Reward icon reveal
- Amount count-up
- Confetti
- Continue appears last

---

## 19. Test plan

### Domain/application EditMode tests

- Success +1
- Fail +0
- Progress cap
- Finish order recorded once
- Ranking correctness
- Tie stability
- Rank 1 reward
- Rank 2 reward
- Rank 3 reward
- Rank 4 no reward
- Rank 5/DNF no reward
- Finish required for reward
- Valid transitions
- Invalid transitions
- Deterministic AI
- Save/restore round trip
- Invalid save rejection
- Reward cannot be claimed twice

### Optional PlayMode tests

- Entry -> Racing
- Success/Fail wiring
- HUD update
- Result popup
- Continue

Do not remove tests later for convenience.

---

## 20. Milestones

## Milestone 0 — Inspect and finalize project setup

### Goals

- Inspect current Unity project
- Confirm Unity version
- Confirm URP setup, active pipeline assets, and renderer configuration
- Confirm Extenject installation
- Confirm UI Kit import
- Confirm DOTween import
- Confirm TextMeshPro
- Create folder structure
- Create asmdefs
- Create installers
- Configure portrait scene
- Initialize Git if needed

### Acceptance

- Extenject compiles
- UI Kit exists
- DOTween exists
- Main scene opens
- Assembly graph is valid
- Gameplay asmdef is Unity-independent
- Git ignores generated folders

### Commit

`chore: initialize Thread Race project and architecture`

### Gate

`AWAITING_USER_TEST`

---

## Milestone 1 — Domain, config, and tests

### Goals

- Runtime config models
- Config assets
- Domain state
- State machine
- Ranking
- Finish order
- Rewards
- EditMode tests

### Acceptance

- No Unity references in gameplay
- Ten-step finish is data-driven
- Five racers are data-driven
- Reward tiers are data-driven
- Core rules pass tests
- Race-end rule is exact

### Commit

`feat: add race domain model and runtime configuration`

### Gate

`AWAITING_USER_TEST`

---

## Milestone 2 — Simulation and persistence

### Goals

- Level-result abstraction
- Central AI simulation
- Deterministic randomness
- Save repository
- Save mapper
- Restore validation
- Application controller

### Acceptance

- Start begins simulation
- Success advances
- Fail does not
- AI moves independently
- AI stops outside Racing
- Result resolves correctly
- Save/restore works
- No per-AI Update loops

### Commit

`feat: implement race simulation and persistence`

### Gate

`AWAITING_USER_TEST`

---

## Milestone 3 — UI structure

### Goals

- Entry popup
- HUD
- Five racer views
- Level-result panel
- Result popup
- Presenter bindings
- UI Kit usage
- Safe-area support

### Acceptance

- Full flow works visually
- Views contain no race rules
- Success/Fail use abstraction
- Ranking and rewards display correctly
- No missing references

### Commit

`feat: build race event UI flow`

### Gate

`AWAITING_USER_TEST`

---

## Milestone 4 — Polish

### Goals

- Popup animations
- Button feedback
- Racer movement
- Overtake feedback
- Finish feedback
- Reward reveal
- Audio
- VFX

### Acceptance

- No animation conflicts
- Tweens clean up safely
- Input lock works
- Reward reveal is high quality
- Audio can be controlled centrally
- No avoidable continuous allocations

### Commit

`feat: add race polish and reward feedback`

### Gate

`AWAITING_USER_TEST`

---

## Milestone 5 — Hardening

### Goals

- Expand tests
- Add smoke tests
- Validate config
- Test all ranks
- Test DNF
- Test save/restore states
- Profile hot paths
- Remove warnings

### Acceptance

- No blocking errors
- No duplicate rewards
- No duplicate transitions
- Restore is safe
- Portrait layouts are stable
- 60 fps target is reasonable

### Commit

`test: harden race flow and save restore`

### Gate

`AWAITING_USER_TEST`

---

## Milestone 6 — Submission

### Goals

- Final README
- AI workflow
- Correction example
- Demo video
- Optional build
- Link verification
- Git review

### README sections

- Overview
- Setup
- Architecture
- Assembly graph
- Extenject composition
- State flow
- Race-end rule
- Config system
- AI simulation
- Save/restore
- Tests
- AI usage
- AI correction example
- Known limitations
- One-more-day improvements
- Demo/build links

### Commit

`docs: finalize case study submission`

### Gate

`READY_FOR_SUBMISSION`

---

## 21. Demo plan

60–90 seconds:

1. Entry popup
2. Start
3. Race HUD
4. Success/Fail
5. AI movement
6. Overtake
7. Finish or loss
8. Reward/no-reward result
9. Brief config/architecture shot if time permits

---

## 22. AI documentation plan

Capture at least one strong correction example.

Preferred example:

- AI proposes a custom DI container or manual fallback
- Developer rejects it
- Project standardizes on installed Extenject
- Composition is moved into installers
- Verification:
  - Assembly dependency review
  - Installer inspection
  - EditMode tests
  - Manual Play Mode flow

Other valid examples:

- Splitting an oversized session
- Replacing four Update loops with one ITickable
- Fixing DNF reward logic
- Replacing runtime-generated UI with scene UI Kit prefabs
- Fixing save validation

---

## 23. Risks

### Extenject scene binding mistakes

Mitigation:

- Explicit installers
- Early validation
- Minimal `FromComponentInHierarchy`
- No manual container resolve in runtime code

### UI Kit mismatch

Mitigation:

- Build wrapper prefabs
- Keep vendor assets untouched
- Verify portrait layout early

### AI feels unfair

Mitigation:

- Data-driven profiles
- Fixed-seed testing
- Multiple tuning runs
- No hidden last-second cheating

### Architecture overengineering

Mitigation:

- Small assembly count
- Interfaces only at boundaries
- Signals only for notifications
- No generic frameworks without need

### Save corruption

Mitigation:

- Versioning
- Validation
- Safe reset
- Reward consistency checks

### Scope creep

Mitigation:

- No real puzzle gameplay
- No shop/meta
- No backend
- Countdown only as stretch

---

## 24. Stretch goals

Only after core completion:

- Multi-day countdown
- Haptics
- Analytics logger
- Localization keys
- WebGL build
- Additional race presets
- Accessibility options

---

## 25. Final checklist

- [ ] Unity 2022.3.62f2 compatible
- [ ] Portrait 1080 × 1920
- [ ] Extenject used throughout composition
- [ ] No custom DI container
- [ ] Gameplay assembly has no Unity dependency
- [ ] Required UI Kit visibly used
- [ ] Five racers
- [ ] Finish at ten
- [ ] Success +1
- [ ] Fail +0
- [ ] AI independent
- [ ] Ranking updates
- [ ] Overtakes visible
- [ ] Top-three rewards
- [ ] Rank 4/5/DNF no reward
- [ ] Finish required for reward
- [ ] Save/restore
- [ ] Deterministic AI
- [ ] Core tests
- [ ] Polished reward reveal
- [ ] README
- [ ] AI correction documented
- [ ] Demo ready
- [ ] Repo shareable
