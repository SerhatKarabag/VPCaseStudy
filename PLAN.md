# PLAN.md — Thread Race Implementation Plan

## 1. Product summary

Build a polished **Thread Race** live event for Thread Fever.

The player competes against four AI opponents. Every successful placeholder level advances the player by one step. A failed level gives no progress. AI racers advance independently over time, including deterministic UTC-based offline catch-up while the timed event is Running. The finish line is ten steps. Rankings change as racers overtake each other. Only the first three finishers receive rewards, and the player receives nothing unless the player reaches the finish.

The live-event countdown is visible and already ticking before the player joins. Pressing Start accepts entry into the currently active event window; it does not extend the end time. AI/race progression still begins only after Start. Event duration is authored in `RaceEventConfigAsset`; the current demo config uses `1800` seconds and can be changed from data. If the configured event end time is reached before the player finishes, the result is EventExpired DNF with no reward.

Clarification: the original candidate brief describes a multi-day countdown as a nice-to-have and does not require a three-day default or Start-created event window. This project keeps the shorter demo window as authored data and documents the chosen active-window behavior.

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
3. If the top-three reward positions are filled before the player finishes, the player's reward outcome is final and the player receives no reward.
4. The event does not continue simulating only to assign cosmetic 4th/5th placements after all reward slots are gone.
5. Transition to `Reward` immediately after the player result is known.
6. If the event timer expires before the player finishes, resolve EventExpired DNF with no reward.
7. Stop AI simulation after result resolution.
8. Reward ranks 1–3 only.
9. Never reward the player without reaching the finish.
10. Continue/claim transitions `Reward` to `Completed`; `Completed` means the result flow has been acknowledged and any eligible reward has been claimed.

This supports:

- 1st place
- 2nd place
- 3rd place
- top-three-filled DNF/no reward
- event-expired DNF/no reward

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
- Reward tier runtime models
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
      Navigation/

  Tests/
    EditMode/
    PlayMode/

  Scenes/
    ThreadRace_Main.unity

  Prefabs/
    UI/
    Racers/
    Effects/

  ScriptableObjects/
    RaceEventConfigAsset.asset
    RacePresentationConfigAsset.asset

  Audio/
    Sounds/
  Fonts/
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
- Countdown update interval

The asset uses a small custom Editor inspector that explicitly draws all serialized fields and validates conversion to runtime settings, so Unity's default inspector cache/repaint issues cannot hide required config fields.

Reward tiers store data-only reward identity, type, amount, display text, and explicit icon IDs. Presentation sprites are bound separately by ID in result/podium views; gameplay config must not fall back to `Sprite` references or sprite names. Missing or empty reward tiers fail config conversion instead of generating fallback rewards in code.

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
- Config-driven reward tier identity/type/amount/display/icon IDs
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

Current hardening decision:

- `RaceSession` remains the pure runtime state/use-case coordinator.
- Ranking, finish-order tracking, final-outcome resolution, snapshot creation, and save snapshot creation live in small internal Gameplay helpers.
- The split is intentionally internal to `ThreadRace.Gameplay` so controller/presenter APIs and save behavior stay stable.

---

## 11. Level-result flow

```text
Success / Fail button
        ↓
IPlaceholderLevelView event
        ↓
PlaceholderLevelPresenter
        ↓
ILevelResultReporter.Report(LevelResult)
        ↓
LevelResultSource.ResultReported
        ↓
RaceLevelResultListener
        ↓
IRaceEventCommandHandler.ReportLevelResult(LevelResult)
        ↓
RaceUiCommandRouter
        ↓
RaceEventController
```

`PlaceholderLevelView` only exposes button events.

`PlaceholderLevelPresenter` converts accepted host-game completion intent into `LevelResult`.

Current hardening decision:

- Host gameplay presenters publish accepted results through `ILevelResultReporter`.
- `LevelResultSource` owns the `ILevelResultSource.ResultReported` event.
- `RaceLevelResultListener` is composed by Extenject in App and forwards the event to the race command handler/router.
- Placeholder UI no longer depends directly on the race command handler or controller.
- The unused direct `ILevelResultHandler` bridge was removed so the event-source path is the only host-result integration path.

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
- Minimum move interval envelope
- Maximum move interval envelope
- Pacing style
- Consistency
- Volatility
- Early/late race bias
- Burst/slump/final-push chances

The serialized min/max values are no longer the whole AI model. They are the timing envelope for a deterministic race plan generated from:

- Event seed
- Racer ID
- Finish target
- AI pacing style label
- Explicit authored pacing tuning values: dynamic-planning flag, skill, consistency, volatility, early/late bias, burst chance, slump chance, and final-push chance

Supported pacing style labels:

- `LegacyFixed` for backwards-compatible tests/helpers
- `Steady`
- `Sprinter`
- `Closer`
- `Wildcard`
- `Balanced`

Current hardening decision:

- AI pacing style labels no longer hide gameplay tuning presets in code.
- `RaceEventConfigAsset` serializes the actual tuning values per AI racer and converts them into immutable `AiPacingProfile` runtime models.
- Gameplay keeps only a minimal `LegacyFixed` helper for fixed-delay tests/backwards-compatible direct construction; production event pacing is authored through data.

The generated `AiRacePlan` contains one delay per finish step. Save/restore persists each AI racer's remaining step timer and the deterministic seed, so restored sessions regenerate the same plan and continue from the saved timer without storing extra scene or ScriptableObject state.

### Central simulation

Use one `RaceSimulationDriver : ITickable`.

Pseudo-flow:

```text
Tick
  For each AI
    if Racing and remaining step timer is due
      advance by one
      schedule next delay from the generated race plan
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
- No last-second cheating or player-reactive rubber-banding
- Different seeds/configured profiles can produce different first-place AI racers

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
- Event start UTC
- Event end UTC
- Last observed UTC
- Explicit resolution reason

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
- Completed eligible outcomes missing a claimed reward flag

Offline progression is in scope. Restore uses elapsed UTC time from the persisted last observed timestamp, clamps catch-up at the event end timestamp, and applies the existing deterministic AI simulation without auto-advancing the player.

---

## 16. UI plan

## 16.0 Main menu shell

Uses the adapted MyTemplate swipe/page mechanic as a host-game wrapper around the event.

Visual source:

- Thread Fever exported main-menu sprites for page backgrounds, navbar tabs, and navbar icons
- Shop uses Thread Fever `InGame_Background` as a calmer distinct page background; Leaderboard uses `SkyRushSky_BG` as a distinct page background
- Required 2D Mobile Game UI Kit remains used by event UI prefabs and as fallback for generated menu elements that do not have Thread Fever counterparts
- HoleCraze `LilitaOne-Regular SDF` is copied as a project-owned TMP font asset under `Assets/Fonts` and applied to all generated presentation text
- Generated TMP text uses the Lilita font asset together with its matching default font material to avoid TextMeshPro material/font mismatches

Shows:

- Five swipeable pages
- Bottom navigation bar with five icons
- Bottom navigation has no separate `SelectionIndicator`; `NavbarItems` stretch edge-to-edge with zero spacing and 190px item height
- Selected navbar tab background scales from the bottom using the Thread Fever pressed/unpressed sprite size ratio, applies a small upward offset, and renders above neighboring tabs, so the pressed tab's yellow arch aligns with the navbar background's yellow top line while inactive items remain visible
- Navbar item roots own the click hit target; child background/icon graphics do not receive raycasts, preserving tap navigation while selected art overlaps visually
- Center Home page with a Play button
- Thread Race as a Home-side live-ops entry button
- Thread Race Home button shows the remaining event time directly instead of generic "Live Ops" copy
- Shop page shows static visual offer packs using 2D Mobile Game UI Kit `UI-pack_Sprite_1`/`UI-pack_Sprite_2` sprites, including currency badge icons (`UI-pack_Sprite_1_22` and `UI-pack_Sprite_1_16`) and pack card backgrounds
- Middle navigation pages ordered Shop, Home, Leaderboard
- Leaderboard page shows a fake populated leaderboard using 2D Mobile Game UI Kit panels/rows until real meta systems exist; fake row backgrounds use `UI-pack_Sprite_1_46` with dark readable text colors
- Leftmost and rightmost navigation entries locked as Coming Soon

Behavior:

- Horizontal swipe changes pages
- Navbar taps snap to Shop, Home, or Leaderboard
- Leftmost and rightmost navbar taps show a HoleCraze-style animated Coming Soon tooltip
- Coming Soon tooltip mirrors only its bubble background per tapped side so the pointer targets the correct navbar icon while label text remains readable
- Coming Soon tooltip pointer alignment exposes left/right offset fields on `PageNavbar` for Unity Inspector fine-tuning
- NotStarted opens the main menu first
- Home Play opens a host-game placeholder gameplay panel
- Home Play remains available after Thread Race is Completed; the event state must not block the host-game placeholder flow
- Home Play uses `UI-pack_Sprite_1_37` as a dedicated main CTA button instead of a nav-tab sprite
- Home Play button label shows the current host-game level as `LEVEL N`, not generic `PLAY`
- Host-game placeholder level progress is persisted separately from Thread Race event save data through `IHostLevelProgressService`, so returning to the app resumes the last claimed host level without making presenters own persistence.
- Thread Race live-ops button opens the Entry popup from Home
- Thread Race live-ops button and Entry popup both show the same ticking event time remaining before Start
- If the Thread Race live-ops button shows `ENDED`, tapping it resolves the expired event as DNF/no reward and opens the Result popup instead of the Entry popup
- If Thread Race is Completed because any racer result made the player outcome final, the live-ops countdown label shows `ENDED`
- If Thread Race is already Running, the Thread Race live-ops button opens the race HUD popup instead of starting gameplay
- Returning from a host-game level while Thread Race is Running automatically opens the race HUD popup after applying the level result
- Restored Running events return to the main menu with the Thread Race live-ops button available
- Restored Completed events may open directly into the result flow

This shell is Presentation-only and must not change gameplay, AI, persistence, or timed-event rules.

## 16.1 Entry popup

Uses required UI Kit.

Shows:

- Event title
- Description
- Race target
- Current event time remaining, ticking before the player joins
- Reward preview
- Start button
- Close button returning to the main menu before Start

Polish:

- Overlay fade
- Shared DOTween popup spring-in through `PhaseViewBase`
- Staggered content
- Button punch

## 16.2 Race HUD

Use a Royal Match Sky Race-inspired composition, adapted to Thread Race and the provided 2D Mobile Game UI Kit:

- Top reward/podium panel with three visible prize positions
- Yellow live-event title tab and compact countdown pill
- Blue status/message band
- Five horizontal race lanes stacked vertically
- Right-side checkered finish stripe
- One pre-created racer row per participant, reordered by rank
- Per-row progress marker that travels toward the finish line using the existing snapshot progress

Shows:

- Five racers
- Ten-step progress
- Finish line
- Current rank
- Prize tiers
- Active countdown while Running
- Overtakes

Use five pre-created racer views.

Do not instantiate/destroy racers during ranking changes.

The HUD visual rework must remain Presentation-only. It must not change race rules, AI timing, persistence, ranking, rewards, or timed-event behavior.

## 16.3 Placeholder level panel

This is now a host-game gameplay panel opened from the Home Play button, not a child section inside the Thread Race popup.

It represents the main game flow, so it must not display `1 / 10` or any other Thread Race finish-target copy. The host level stream is treated as unbounded for this case study. Thread Race counts only accepted Success/Fail results after the player has joined the event.

Challenge screen shows:

- Current host-game level title as `LEVEL N`
- Success
- Fail

Success flow:

- Success button opens `LevelWin`
- `LevelWin` shows a fake coin claim
- `LevelWin` uses UI Kit sprite `UI-pack_Sprite_1_12` for the coin icon and `UI-pack_Sprite_1_36` for the claim button
- `LevelWin` opens with the same premium spring-in motion language as the event popups
- Claim advances the host-game level by one and returns to the main menu
- If Thread Race is Running, the Success result is reported to the race and the race HUD popup opens with updated progress/ranking

Fail flow:

- Fail button opens `LevelFail`
- `LevelFail` uses host-game-only copy and must not mention race progress, because the player may not have joined Thread Race
- `LevelFail` opens with the same premium spring-in motion language as the event popups
- Back Home returns to the main menu without advancing the host-game level
- If Thread Race is Running, the Fail result is reported to the race and the race HUD popup opens with updated AI/ranking state

The placeholder panel remains a stand-in for the host game. It must not contain race rules; it only reports `LevelResult` through the existing abstraction when the host-game result is accepted.

Optional development-only reset button can exist behind a debug flag.

## 16.4 Result popup

Shows:

- Rank or DNF
- TIME'S UP DNF state for event expiration
- Config-driven reward display text/icon if eligible
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
- Use application/core services for host-flow state changes instead of loading/saving persistence directly
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
- Shared popup alpha fade plus scale spring from ~0.84 to 1
- Back/overshoot ease with input locked until the intro completes
- Stagger rewards and Start button

### Shared popups

- `EntryPopupView`, `RaceHudView`, `RaceResultView`, and `PlaceholderLevelView` inherit the same `PhaseViewBase` spring-in/dismiss animation.
- Host `LevelWin` and `LevelFail` sub-panels use matching CanvasGroup + RectTransform spring-in transitions.
- Tweens use unscaled time and are killed safely on destroy or visibility changes.

### Buttons

- Runtime `ButtonPressFeedback` on key uGUI buttons
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
- Improved-rank row pulse
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

## 18.1 Audio plan

Audio follows the HoleCraze pattern but is implemented in ThreadRace architecture:

- One central `IRaceAudioService`
- Separate looping music sources for smooth Menu/InGame crossfade
- One SFX source for one-shot clips
- Clip references live in `RaceAudioLibraryAsset`, loaded from `Resources/ThreadRaceAudioLibrary`
- Music/SFX cue enums live in `ThreadRace.Core` with no Unity references
- `RaceAudioService` lives in `Infrastructure`; Zenject lifecycle is adapted by `RaceAudioDriver` in `App`
- `RaceAudioPresenter` reacts to presentation signals and does not put audio logic into views
- `menuMusic` plays on menu/event popups
- `inGameMusic` plays in the host placeholder gameplay panel
- `challangePopupOpen` plays when the host challenge opens
- `LevelWinPopupOpen` plays when LevelWin appears
- `failPopupOpen` plays when LevelFail appears
- `ClaimButtonClick` plays when the LevelWin claim is accepted
- `ButtonClick` plays when a navbar item is tapped, the Thread Race live-ops button is pressed, Entry Start is pressed, popup close buttons are pressed, and the LevelFail Back Home button is pressed
- Additional event-specific SFX can be added when dedicated clips are available; do not add unmapped cue enums that would break audio-library validation

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

- Five-page swipe main menu shell
- Bottom navigation bar
- Home-side Thread Race live-ops entry
- Entry popup
- HUD
- Five racer views
- Level-result panel
- Result popup
- Configurable countdown display
- UTC-based offline AI catch-up while Running
- Event expiration DNF/no-reward presentation
- Lifecycle checkpoint/catch-up hooks
- Presenter bindings
- UI Kit usage
- Safe-area support

### Acceptance

- Main menu pages can be swiped horizontally
- Bottom navigation selects the correct page
- Thread Race Entry opens from the Home live-ops entry while NotStarted
- Full flow works visually
- Views contain no race rules
- Success/Fail use abstraction
- Ranking and rewards display correctly
- Countdown is visible before Start and continues ticking while NotStarted or Running
- Start does not extend the active event window; AI and player race progression still begin only after Start
- Restored Running events apply bounded offline AI catch-up
- Expired unfinished events open directly to Result as DNF/no reward
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
- First-show UI/audio prewarm

### Acceptance

- No animation conflicts
- Tweens clean up safely
- Input lock works
- Reward reveal is high quality
- Audio can be controlled centrally
- First Play/Home gameplay transition avoids lazy audio-data, TMP mesh, and layout warm-up spikes
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

Current submission-doc decision:

- `README.md` is now the primary external-facing submission document.
- It must remain factual and code-backed: no claim of a final Unity/Test Runner pass unless that pass was actually run.
- It explicitly covers project structure, architecture decisions, AI tools used, one concrete AI correction/override, verification method, known limitations, and one-more-day improvements.

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

Submitted demo video:

- [YouTube Shorts](https://youtube.com/shorts/h3hAK5zwKgo?si=MPjAOpBHNaINDq1a)

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

### Unity scene/prefab override corruption

Mitigation:

- Keep generated UI in reusable prefabs where practical
- Avoid automatic editor setup that writes scene/prefab assets during project or scene open
- Keep `Tools > Thread Race > Setup Main Menu Navigation` validation-only during crash recovery
- Repair saved scene/prefab state offline or through narrowly scoped Unity-authored edits instead of rebuilding active Canvas UI interactively
- Keep scene prefab instance overrides minimal, especially for generated HUD prefabs

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

### Device-clock manipulation

Mitigation:

- Use UTC only
- Persist last observed UTC
- Effective time is `max(currentUtc, lastObservedUtc)`
- Document that large forward device-clock changes can advance or expire the offline-only event without a backend time authority

### Scope creep

Mitigation:

- No real puzzle gameplay
- No shop/meta
- No backend
- Multi-day countdown and offline progression are Milestone 3 scope by user-approved correction

---

## 24. Stretch goals

Only after core completion:

- Haptics
- Analytics logger
- Localization keys
- WebGL build
- Additional race presets
- Accessibility options

---

## 25. Final checklist

- [x] Unity 2022.3.62f2 compatible
- [x] Portrait 1080 × 1920
- [x] Extenject used throughout composition
- [x] No custom DI container
- [x] Gameplay assembly has no Unity dependency
- [x] Required UI Kit visibly used
- [x] Five racers
- [x] Finish at ten
- [x] Success +1
- [x] Fail +0
- [x] AI independent
- [x] Ranking updates
- [x] Overtakes visible
- [x] Top-three rewards
- [x] Rank 4/5/DNF no reward
- [x] Finish required for reward
- [x] Save/restore
- [x] Deterministic AI
- [x] Core tests
- [x] Polished reward reveal
- [x] README
- [x] AI correction documented
- [x] Demo ready
- [x] Repo shareable
