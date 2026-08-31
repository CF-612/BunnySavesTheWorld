# Unity Project Context

<!-- unity-onboarding:generated:start -->

## Project Summary

- Project root: `D:/GameCreator/Document/BunnySavesTheWorld`
- Last analyzed: 2026-08-31
- Last analyzed commit: `8aa4a9123ddf697cc887947c04effb661a0284b6`
- Game: 2D single-player platform adventure with a plain-C# player state machine, NodeCanvas dialogue, uGUI, and scene-authored gameplay objects.

## Confirmed Environment

- Unity version: 6000.4.0f1
- Render pipeline: Built-in Render Pipeline (`GraphicsSettings.asset` has no custom render pipeline)
- Input system: both Input System and legacy Input Manager are enabled
- Target platform observed in generated project: Standalone Windows 64-bit

## Important Packages And Frameworks

| Area | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| Input | Input System 1.19.0 plus legacy input | Confirmed | `Packages/manifest.json`, `ProjectSettings/ProjectSettings.asset` |
| Dialogue | NodeCanvas Dialogue Trees | Confirmed | `Assets/ParadoxNotion/NodeCanvas`, first-party UI scripts |
| UI | uGUI 2.0.0 | Confirmed | package manifest and runtime UI scripts |
| Scene/content loading | Addressables installed; target game flow currently uses SceneManager | Confirmed | package manifest and first-party scene scripts |
| Camera/cinematics | Cinemachine 3.1.6 and Timeline 1.8.11 installed | Confirmed | package manifest |
| Tweening | DOTween modules imported | Confirmed | `Assets/Plugins/Demigiant` |

## Directory Structure

| Path | Purpose | Confidence | Evidence |
| --- | --- | --- | --- |
| `Assets/Resources/Scripts` | First-party runtime code | Confirmed | representative code inspection |
| `Assets/Resources/Prefab` | Shared game prefabs, including the level manager | Confirmed | prefab inspection |
| `Assets/Scenes` | Main menu and gameplay scenes | Confirmed | build settings and scene assets |
| `Assets/ParadoxNotion` | NodeCanvas vendor code | Confirmed | asmdefs and package content |
| `Assets/FunkyCode` | SmartLighting2D vendor code and demos | Confirmed | imported scripts and demo scenes |

## Assembly Boundaries

| Assembly | Responsibility | Key references | Notes |
| --- | --- | --- | --- |
| Assembly-CSharp | First-party runtime plus SmartLighting2D scripts | Unity, Input System, NodeCanvas, DOTween | Monolithic; no first-party asmdef |
| NodeCanvas | Dialogue and graph runtime | ParadoxNotion | Vendor assembly |
| ParadoxNotion | NodeCanvas foundation | Unity | Vendor assembly |
| DOTween.Modules | Tweening integrations | DOTween | Vendor assembly |

## Scenes And Startup Flow

- Enabled build scenes: `MainMenu`, `Home_Light`, `Home_Dark`, `City`, `City_Giant`, `Giant`.
- Startup scene: `MainMenu`.
- Scene flow: `MainMenuController` and `ScenePortal` call the shared `SceneTransitionService`; `SceneEntrance` supplies the initial fade adapter.
- Checkpoint resume: `GameProgressService` stores the last checkpoint per scene; `LevelManager` consumes a resume request when continuing from the menu.

## Architecture

| Pattern | Finding | Confidence | Evidence |
| --- | --- | --- | --- |
| Player behavior | Plain-C# state objects owned by a `Player` MonoBehaviour | Confirmed | `Player.cs`, `StateMachine` scripts |
| Scene composition | Scene/prefab-authored MonoBehaviours with serialized references | Confirmed | gameplay scenes and `LevelManager.prefab` |
| Dialogue | NodeCanvas graphs with thin trigger/UI adapters | Confirmed | dialogue scripts and scene references |
| Persistence | Static, narrow JSON progress service for scene/checkpoint state | Confirmed | `GameFlow/GameProgressService.cs` |
| Cross-scene presentation | Persistent scene-transition service with runtime fade overlay | Confirmed | `GameFlow/SceneTransitionService.cs` |

## Coding Conventions

- Namespace style: first-party gameplay scripts currently use the global namespace.
- Serialized fields: mixed public fields and `[SerializeField] private`; existing names are preserved for scene compatibility.
- Async: Unity coroutines and `SceneManager.LoadSceneAsync`.
- Comments/docs: bilingual Inspector labels, with XML summaries for system ownership and non-obvious lifecycle behavior.

## Testing And Validation

- EditMode tests: none found for first-party code.
- PlayMode tests: none found for first-party code.
- Compilation: Unity Roslyn response-file compilation is available from `Library/Bee`.
- Known pre-existing import defect: `Assets/Resources/Animations/Player/Player.controller` contains an overflowing local file ID near line 1058.

## Available Unity Tooling

| Capability | Status | Evidence |
| --- | --- | --- |
| Unity Editor 6000.4.0f1 | available | `D:/GameCreator/Unity/Editor/Unity.exe` |
| Unity batch compilation | available when the project is not open elsewhere | batch invocation reached the project lock |
| Unity MCP read/mutation tools | unavailable | no Unity MCP tools exposed in this session |
| C# response-file compilation | available | `Library/Bee/artifacts/*/Assembly-CSharp.rsp` |

## Important Constraints

- Preserve scene and prefab field names and existing UnityEvent method names.
- Do not import the Udemy sample's combat, inventory, quest, merchant, or skill dependencies.
- Keep NodeCanvas as the authoritative dialogue authoring system.
- Treat scenes, prefabs, controllers, and project settings as high-risk serialized assets.
- Do not include `.claude/` or unrelated editor-generated changes in feature commits.

## Unknowns And Confidence

- Runtime behavior still needs manual Play Mode coverage for death timing, checkpoint visuals, all portal routes, and menu continue/new-game UX.
- The active Unity Editor prevented a separate batch instance from performing a full import-and-test pass.
- There is no user-facing save-slot selection; the current menu has one continue-or-start action and a callable `StartNewGame` method for a future separate button.

## Source Files Inspected

- `ProjectSettings/ProjectVersion.txt`
- `ProjectSettings/EditorBuildSettings.asset`
- `ProjectSettings/GraphicsSettings.asset`
- `ProjectSettings/ProjectSettings.asset`
- `Packages/manifest.json`
- `Assets/Resources/Scripts/Player/Player.cs`
- `Assets/Resources/Scripts/LevelManager/*`
- `Assets/Resources/Scripts/UI/*Dialogue*`
- `Assets/Resources/Scripts/UI/MainMenuController.cs`
- `Assets/Resources/Prefab/LevelManager.prefab`
- Relevant gameplay scenes and the Udemy reference project's corresponding scripts

<!-- unity-onboarding:generated:end -->
