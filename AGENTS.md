# AGENTS.md — Thread Race Case Study

## 1. Mission

Build a production-quality **Thread Race** in-game event for **Thread Fever** as a standalone Unity case-study project.

The feature must feel like a live-event module that could be integrated into an existing mobile game maintained by a team.

The project is evaluated primarily on:

1. **Code quality and architecture — 40%**
2. **UI polish and game feel — 35%**
3. **AI workflow and README quality — 15%**
4. **Following the brief — 10%**

Depth, clarity, maintainability, and polish matter more than feature count.

---

## 2. Source of truth

Before making any implementation decision, read:

- `AGENTS.md`
- `PLAN.md`
- `PROGRESS.md`
- The original case-study document supplied by the user
- The current saved Unity project state
- The latest user instruction

Priority order when sources conflict:

1. Original case-study requirements
2. Latest user instruction
3. Current project state and imported assets
4. `AGENTS.md`
5. `PLAN.md`
6. `PROGRESS.md`
7. Older prompts or assumptions

Do not silently reinterpret requirements.

If an instruction or project state changes:

- Update `PLAN.md`
- Update `PROGRESS.md`
- Record the decision in the decision log

---

## 3. Hard requirements

The final submission must include:

- Unity **2022.3 LTS**, compatible with **2022.3.62f2**
- Mobile portrait target, approximately **1080 × 1920**
- A shareable Git repository with sensible commit history
- A proper Unity `.gitignore`
- A complete `README.md`
- A 60–90 second demo video and/or hosted playable build
- UI built with the provided **2D Mobile Game UI Kit**
- AI usage documented in the README
- Five racers total:
  - Player
  - Four AI opponents
- Finish line at **10 successful levels**
- Player advances by one step on **Success**
- Player does not advance on **Fail**
- AI opponents advance independently over time
- Ranking updates while racers overtake each other
- Rewards only for ranks 1–3
- The player receives a reward only if the player reaches the finish
- Rank 4, rank 5, or DNF receives no reward

Do not remove or weaken these requirements.

---

## 4. Required technical baseline

Use the following unless the user explicitly changes them:

- Unity: `2022.3.62f2`
- Rendering: Universal Render Pipeline (URP), created from Unity Hub's Universal 3D template
- UI: uGUI + TextMeshPro
- Animation: DOTween
- Dependency injection: **Extenject**
- Orientation: Portrait
- Reference resolution: `1080 × 1920`
- Namespace root: `ThreadRace`
- Main demo scene: `Assets/ThreadRace/Scenes/ThreadRace_Demo.unity`

### URP is the approved rendering pipeline

The project was intentionally created with Unity Hub's **Universal 3D** template and will remain on URP.

Rules:

- Do not convert the project to the Built-in Render Pipeline.
- Do not replace the active URP assets without user approval.
- Keep renderer features and post-processing limited to features that create visible value for the case.
- Prefer mobile-friendly shaders and effects.
- Avoid adding expensive full-screen effects only because URP supports them.
- Any custom shader or Shader Graph asset must have a clear presentation purpose and mobile fallback.
- Record the installed URP package version and active pipeline assets in `PROGRESS.md`.

### Extenject is mandatory

Extenject is already installed and must be used.

Do not:

- Create a custom DI container
- Add a second DI framework
- Use a service locator
- Use static singleton access for application services
- Replace Extenject with manual DI fallback
- Create hidden runtime dependency graphs outside installers

All runtime composition must be visible through Extenject installers.

---

## 5. Architecture principles

The project must use clear architecture boundaries.

### Required layers

```text
App / Composition
Core
Gameplay
Infrastructure
Presentation
Tests
```

### Required dependency direction

```text
Presentation -> Gameplay
Infrastructure -> Gameplay contracts
App -> Core + Gameplay + Infrastructure + Presentation
Gameplay -> Core only
```

### Gameplay assembly must be Unity-independent

`ThreadRace.Gameplay` must:

- Use `noEngineReferences: true`
- Avoid `UnityEngine`
- Avoid `MonoBehaviour`
- Avoid Unity UI
- Avoid DOTween
- Avoid scene objects
- Avoid Extenject attributes inside domain models where not required

The gameplay layer contains pure rules and application logic.

---

## 6. Assembly definition rules

Create a small, intentional assembly structure.

Recommended assemblies:

```text
ThreadRace.Core
ThreadRace.Gameplay
ThreadRace.Infrastructure
ThreadRace.Presentation
ThreadRace.App
ThreadRace.Tests
```

Rules:

- `ThreadRace.Gameplay` references only `ThreadRace.Core`
- `ThreadRace.Infrastructure` references `ThreadRace.Core` and `ThreadRace.Gameplay`
- `ThreadRace.Presentation` references `ThreadRace.Core` and `ThreadRace.Gameplay`
- `ThreadRace.App` references all runtime assemblies and Extenject
- `ThreadRace.Tests` references the assemblies it tests
- Do not create an asmdef for every folder
- Do not allow circular assembly references
- Do not put business rules inside `ThreadRace.App`

---

## 7. Extenject composition rules

Use two installers unless the existing project structure strongly justifies another simple arrangement:

### `ThreadRaceProjectInstaller`

Bind long-lived services:

- Logger
- Save repository
- Time provider
- Random source or random source factory
- Global audio service
- Shared configuration providers if truly global

### `ThreadRaceSceneInstaller`

Bind scene/event-specific objects:

- Race runtime config
- Presentation config
- Race controller
- AI simulation service
- Ranking service
- Reward service
- Finish service
- Level result source
- Presenters
- Scene views
- Zenject signals

### Binding rules

- Prefer constructor injection for plain C# classes
- Prefer `[Inject]` only for scene-bound `MonoBehaviour` views when appropriate
- Use `AsSingle()` for one race-event session service
- Use `FromInstance()` for validated runtime config
- Use `FromComponentInHierarchy()` only for known scene views
- Do not resolve services manually from the container in gameplay code
- Do not call `Container.Resolve` outside composition/bootstrap code
- Bind interfaces at architectural boundaries, not for every tiny class
- Validate the object graph early

---

## 8. Static config and runtime config

Use ScriptableObjects for Inspector-authored static data, but do not pass mutable ScriptableObject assets directly into core gameplay logic.

Required pattern:

```text
RaceEventConfigAsset : ScriptableObject
        ↓ validation + conversion
RaceEventConfig : immutable plain C# runtime model
```

Use the same pattern for presentation tuning:

```text
RacePresentationConfigAsset : ScriptableObject
        ↓ conversion
RacePresentationConfig : immutable/plain runtime model
```

### `RaceEventConfigAsset` may contain

- Finish step
- Racer definitions
- Four AI profiles
- Reward tiers
- Save schema version
- Save key
- Random seed settings
- Optional event duration
- Race resolution settings

### `RacePresentationConfigAsset` may contain

- Popup timings
- Button feedback timings
- Racer movement duration
- Tween eases
- Overtake feedback timing
- Reward reveal timing
- Audio cues
- VFX references
- UI colors and motion tuning

Rules:

- Gameplay values and presentation values must not be mixed
- Runtime state must never be stored inside a ScriptableObject
- Config assets must validate themselves
- Missing required config must fail clearly
- Do not silently create runtime defaults that hide missing scene references

---

## 9. Domain and application responsibilities

### Domain

Pure race rules and state.

Contains:

- `RaceEventState`
- `RaceRuntimeState`
- `RacerRuntimeState`
- `RacerId`
- Finish-order rules
- Ranking rules
- Reward eligibility rules
- State-transition rules

Must not contain:

- Unity references
- Save implementation
- UI logic
- Audio/VFX calls
- Scene references

### Application

Coordinates use cases.

Contains:

- Start race
- Handle level result
- Advance AI
- Recalculate rankings
- Record finish order
- Resolve the race
- Request persistence
- Publish state-change notifications

Do not create one 500–600 line god session class.

Recommended responsibilities:

- `RaceEventController`
- `RaceSimulationService`
- `RaceRankingService`
- `RaceFinishService`
- `RaceRewardService`
- `RaceSaveDataMapper`

Keep the number of classes reasonable. Split only where responsibilities are genuinely different.

---

## 10. Presentation architecture

Use scene-authored/prefab-authored UI based on the required UI Kit.

Do not build the whole UI from code at runtime.

Required pattern:

```text
View
  ↕ user input / render data
Presenter
  ↕ application calls / signals
Gameplay application services
```

Recommended pairs:

```text
EntryPopupView
EntryPopupPresenter

RaceHudView
RaceHudPresenter

LevelResultView
LevelResultPresenter

ResultPopupView
ResultPopupPresenter
```

Views must not decide:

- Ranking
- Rewards
- Finish order
- AI pacing
- Save behavior
- State transitions

Views only:

- Expose user intent
- Render supplied data
- Play presentation feedback

Presenters must unsubscribe/dispose correctly.

---

## 11. Level-result decoupling

The placeholder Success/Fail screen is only a stand-in for the host game.

Use an abstraction such as:

```csharp
public interface ILevelResultSource
{
    event Action<LevelResult> ResultReported;
}
```

Rules:

- Success button reports `LevelResult.Success`
- Fail button reports `LevelResult.Fail`
- The race event subscribes through the abstraction
- The race event must not reference button components
- The view must not change progress directly

Expected flow:

```text
Success / Fail button
        ↓
LevelResultPresenter
        ↓
ILevelResultSource
        ↓
RaceEventController
```

---

## 12. Zenject signal rules

Use Zenject Signals only for notifications that can have multiple listeners or represent presentation-wide state changes.

Recommended signals:

```text
RaceStartedSignal
RacerProgressChangedSignal
RankingChangedSignal
RacerFinishedSignal
RaceResolvedSignal
RewardClaimedSignal
```

Signal payloads should be immutable, preferably `readonly struct`.

Rules:

- Use direct method calls for commands
- Use signals for notifications
- Do not create a signal for every method
- Do not use signals as a hidden global service locator
- Do not pass mutable runtime entities to views
- Prefer IDs and snapshots in payloads

---

## 13. Product rules

### Race start

- Initial event state is `Entry`
- AI movement does not begin before Start
- Start transitions to `Racing`
- Save immediately after race start

### Player progress

- Success advances player by exactly one
- Fail advances player by zero
- Progress never exceeds the finish step
- Repeated input during blocked transitions must be ignored

### AI progress

- Four AI racers advance independently
- Each AI uses a data-driven pacing profile
- AI movement occurs only in `Racing`
- AI movement stops in `Results` and `Completed`
- AI behavior is deterministic when using a fixed seed
- Winning is possible but not guaranteed
- Do not use obviously unfair last-second cheating

### Explicit finish rule

1. Record a racer’s finish order only the first time it reaches the configured finish step.
2. If the player reaches the finish, the player’s final rank is fixed immediately.
3. If all four AI racers finish before the player, the player is rank 5 / DNF.
4. Transition to `Results` as soon as the player outcome becomes irreversible.
5. Rank 1–3 receives configured rewards.
6. Rank 4, rank 5, or DNF receives no reward.
7. A reward is never granted unless the player reached the finish.

Document this rule in the final README.

---

## 14. AI simulation rules

Use one centralized simulation driver.

Recommended implementation:

```csharp
public sealed class RaceSimulationDriver : ITickable
```

Bind it with Extenject:

```csharp
Container
    .BindInterfacesTo<RaceSimulationDriver>()
    .AsSingle();
```

Each AI racer stores or owns:

- Initial delay
- Next scheduled move time
- Minimum/maximum interval
- Optional pause chance
- Seed offset

Rules:

- Do not add one `Update()` per AI
- Use unscaled time
- Recalculate ranking only after progress changes
- Publish UI updates only when state changes
- No LINQ in hot paths
- No per-frame allocations
- Preallocate fixed-size racer collections where practical
- Avoid repeated hierarchy searches
- Avoid string formatting every frame

---

## 15. Deterministic randomness

Use an interface such as:

```csharp
public interface IRaceRandomSource
{
    float Range(float minInclusive, float maxInclusive);
    float Value { get; }
}
```

Requirements:

- Fixed seed produces repeatable behavior
- Tests can inject a fake or deterministic source
- Save/restore must preserve enough simulation state to continue consistently
- Each AI may use a seed offset or independent stream
- Do not use `UnityEngine.Random` directly in gameplay code

---

## 16. Persistence rules

Implement versioned save/restore behind an interface.

Recommended contract:

```csharp
public interface IRaceSaveRepository
{
    bool TryLoad(out RaceSaveData data);
    void Save(RaceSaveData data);
    void Clear();
}
```

Persist at minimum:

- Save schema version
- Current event state
- Player progress
- AI progress
- Finish order
- Current or reconstructable rankings
- AI next-move timings
- Seed/random state where needed
- Player final result
- Reward claim state
- Entry accepted state

Save after:

- Race start
- Player success
- AI advancement
- Racer finish
- Race resolution
- Reward claim
- App pause/focus loss/quit

Restore validation must check:

- Save version
- Racer count
- Racer IDs
- Progress ranges
- Duplicate finish-order entries
- Result/reward consistency
- Config compatibility

Do not serialize scene-object references.

Provide a development-only reset path.

---

## 17. Performance and allocation rules

The race has only five racers, so avoid unnecessary frameworks while still maintaining good runtime discipline.

Use:

- Fixed-size arrays or pre-sized lists where practical
- Pre-created racer views
- Pooled/reused VFX where useful
- Event-driven UI refresh
- Cached references
- Central tick driver

Do not:

- Instantiate/destroy racer UI during overtakes
- Rebuild the entire HUD after every update
- Use reflection in hot paths
- Allocate closures every tick
- Use LINQ every frame
- Create an oversized generic pooling framework for five racers

---

## 18. UI and polish rules

The provided UI Kit must be visibly used.

Prioritize:

1. Entry popup
2. Button feedback
3. Racer progress movement
4. Overtake feedback
5. Finish-line moment
6. Reward reveal

Animation guidelines:

- Use DOTween
- Keep timing in presentation config
- Kill/reuse tweens safely
- Prevent overlapping transitions
- Use unscaled time for UI transitions
- Input must be locked during critical sequences
- Domain state must not depend on animation completion without safeguards

Polish should clarify game state.

---

## 19. Screen responsibilities

### Entry popup

Shows:

- Event title
- Short explanation
- Race target
- Top-three reward preview
- Start button

### Race HUD

Visible while racing.

Shows:

- Five racers
- Progress toward finish
- Finish line
- Current ranking
- Prize tiers
- Overtake/finish feedback

### Placeholder level screen

Shows only:

- Success
- Fail

### Result popup

Shows:

- Final rank or DNF
- Reward for top three
- No-reward state for lower ranks
- Continue button

Continue completes/closes the event.

---

## 20. Testing rules

At minimum, add EditMode tests for:

- Success increments player by one
- Fail does not increment player
- Progress never exceeds finish
- Finish order is recorded once
- Ranking updates correctly
- Tie handling is deterministic
- Top-three reward mapping
- Rank 4 receives no reward
- Rank 5/DNF receives no reward
- Player must finish to receive reward
- Valid state transitions
- Invalid state transitions
- Save/restore round trip
- Deterministic AI behavior with fixed seed
- Invalid save rejection
- Reward cannot be claimed twice

Optional PlayMode smoke tests:

- Entry -> Racing
- Success/Fail wiring
- Race HUD update
- Result popup
- Continue flow

Do not delete tests merely to simplify folder structure.

Do not claim tests passed unless they were actually run.

---

## 21. Folder structure

Recommended:

```text
Assets/
  ThreadRace/
    Runtime/
      App/
        Installers/
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
      Presentation/
        Views/
        Presenters/
        Signals/
        Animation/
    Tests/
      EditMode/
      PlayMode/
    Scenes/
    Prefabs/
      UI/
      Racers/
      Effects/
    ScriptableObjects/
    Audio/
    VFX/
    Art/
  ThirdParty/
```

---

## 22. Third-party asset rules

Required:

- 2D Mobile Game UI Kit
- DOTween
- Extenject

Rules:

- Third-party assets live under `Assets/ThirdParty/` where practical
- Do not edit vendor code unless required
- Do not rename vendor folders casually
- Do not import paid or unclear-license assets
- Record package/source/version information in `PROGRESS.md`
- If the required UI Kit is missing, stop and ask the user to import it

---

## 23. Codex workflow

### Before every milestone

1. Read `AGENTS.md`
2. Read `PLAN.md`
3. Read `PROGRESS.md`
4. Inspect Git status
5. Inspect current diff
6. Confirm active milestone
7. Confirm required assets and dependencies
8. Do not assume previous work completed

### During implementation

- Work only in the active project repository
- Preserve user-authored changes
- Keep changes milestone-scoped
- Do not create duplicate architecture
- Do not create a custom DI system
- Do not bypass Extenject
- Do not add optional features early

### Unity restrictions

Unless explicitly instructed:

- Do not launch Unity
- Do not enter Play Mode
- Do not run builds
- Do not claim compilation/runtime verification

The user performs Unity testing.

### After every milestone

1. Update `PROGRESS.md`
2. List created/modified files
3. Explain architecture changes
4. Provide exact Unity test steps
5. Mark milestone `AWAITING_USER_TEST`
6. Stop
7. Wait for user verification

---

## 24. Git rules

Recommended commit sequence:

1. `chore: initialize Thread Race project and dependencies`
2. `feat: add race domain model and runtime configuration`
3. `feat: implement race simulation and persistence`
4. `feat: build race event UI flow`
5. `feat: add race polish and reward feedback`
6. `test: cover race rules and save restore`
7. `docs: finalize case study submission`

Rules:

- Commit at real milestones
- Do not fabricate history later
- Do not commit `Library/`, `Temp/`, `Logs/`, `Obj/`, or builds
- Include `.meta` files
- Do not commit secrets
- Do not use `git reset --hard`
- Do not force-push

---

## 25. AI workflow documentation

AI usage is required and graded.

Maintain an AI log containing:

- Date
- Tool
- Task
- Output used
- What was changed/rejected
- Verification method

At least one concrete correction must be documented.

Strong candidate examples:

- Rejecting a custom DI container and standardizing on Extenject
- Rejecting a monolithic race-session class
- Replacing four AI `Update()` methods with one `ITickable`
- Correcting reward logic for DNF
- Correcting unsafe save/restore
- Replacing runtime-generated UI with scene/prefab UI Kit views

The README must explain:

- Which AI tools were used
- Where AI helped
- One correction/override
- How output was verified

---

## 26. Definition of done

The case is complete only when:

- Hard race rules work
- Extenject composition is clear
- Gameplay assembly is Unity-independent
- UI Kit is visibly used
- AI movement is independent and deterministic
- Ranking/overtakes update correctly
- Save/restore works
- Rewards are correct
- DNF receives no reward
- Player cannot receive reward without finishing
- Motion and reward moments feel polished
- Core tests exist and pass
- No blocking console errors remain
- Portrait layout works near 1080 × 1920
- README is complete
- AI workflow is documented
- Demo video is ready
- Git history is sensible
- Submission links are verified

Do not add stretch features before the required experience is stable.
