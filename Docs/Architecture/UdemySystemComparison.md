# Udemy System Comparison And Migration Decision

## Scope

This review compares the project's checkpoint/death-respawn, dialogue/NPC, main-menu, scene-transition, and opening-story UI code with:

`D:/GameCreator/资源/7. Udemy Course - RPG New - Dialogue System/Udemy Course - RPG New`

Combat, inventory, equipment, crafting, skills, merchants, and quests are deliberately excluded.

## Summary Decision

The reference project is broader, but it is not uniformly more mature. Its useful idea is centralized ownership: a persistent game-flow owner coordinates saves and scene changes, checkpoints have persistent identity, and fade UI is reused. Those ideas are worth adopting.

Directly copying the reference implementation is not recommended because its dialogue flow depends on quest/shop/crafting systems, its save discovery is scene-local, and sample runtime files contain editor/test-only imports. The project should keep NodeCanvas and adopt only the narrow architectural seams that solve current duplication.

## Structural Comparison

| Area | Before in Bunny Saves the World | Udemy reference | Decision implemented |
| --- | --- | --- | --- |
| Checkpoint state | `LevelManager` stores one runtime `Vector3`; scene reload loses it | `Object_Checkpoint` has IDs and implements `ISaveable` | Keep the simple scene-local coordinator, add stable checkpoint identity and a narrow JSON progress service |
| Death/respawn | Static death event plus coroutine; repeated death and input ownership were implicit | Player dead state delegates scene restart/position selection to `GameManager` | Retain fast in-scene respawn, guard duplicate death, make input locking explicit, restore from saved checkpoint only for continue |
| Save data | No system for these features | One large `GameData` mixes inventory, skills, quests, portals, and checkpoints | Use `GameProgressData` containing only scene/checkpoint progress |
| Scene transition | Each caller creates its own Canvas and calls synchronous `LoadScene` | Persistent `GameManager` and reusable `UI_FadeScreen` | Add persistent `SceneTransitionService`, async loading, unscaled fades, and input-blocking overlay |
| Main menu | Button effects, opening pages, audio, and scene loading in one class | Thin UI calls `GameManager.ContinuePlay()` | Keep authored button effects but delegate persistence, story sequencing, and scene loading |
| Opening/portal story pages | Similar array/index logic duplicated in menu and portal | Dialogue/data-driven flow, but coupled to RPG actions | Extract `StorySequence`; preserve existing scene art and behavior |
| NPC interaction | Each NPC polls E and looks at shared dialogue UI state | `IInteractable` NPC base, but tightly coupled to quests/player singleton | Keep a thin NodeCanvas adapter; use per-dialogue completion callback and player input lock |
| Automatic dialogue | Subscribes to global `DialogueTree.OnDialogueFinished` | Dialogue UI owns line progression | Use `DialogueTreeController.StartDialogue(callback)` so only the started graph can complete the trigger |
| Player input lifecycle | Lambdas are added on every `OnEnable`, so re-enable duplicates callbacks | Central player input owner | Subscribe once in `Awake`, unsubscribe/dispose in `OnDestroy`, and use owner-based locks |

## Reference Code Not Migrated

- `UI_Dialogue`, `DialogueLineSO`, and `DialogueNpcData`: they replace rather than improve NodeCanvas and depend on RPG action enums, rewards, quests, shops, and crafting.
- The full `GameData` and `SaveManager`: unrelated RPG state would create unnecessary coupling; saveable discovery is captured only at that manager's `Start` and is fragile across scene changes.
- `Object_NPC`: its base class assumes `Player.instance.questManager` and updates floating UI/facing every frame.
- `Object_Portal`: it models a town-return combat-RPG portal rather than this game's linear authored scene portals.
- Reference sample defects such as runtime `using UnityEditor` and `using NUnit.Framework.Interfaces` were not copied.

## Implemented Ownership

- `GameProgressService`: file IO and authoritative checkpoint/continue data.
- `SceneTransitionService`: persistent fade overlay and asynchronous scene lifetime.
- `StorySequence`: current story-page index and visibility.
- `LevelManager`: scene-local death and respawn timing.
- `CheckPoint`: trigger, stable identity, optional visuals/audio.
- `Player`: input action lifetime, owner-based input locks, and death state.
- Menu, portal, and dialogue scripts: authoring adapters that delegate system work.

## Save Behavior

- Save file: `Application.persistentDataPath/bunny-progress.json`.
- A checkpoint activation immediately records its ID, scene, and respawn position.
- Normal portals update the last entered scene but do not force checkpoint teleportation.
- Continuing from the menu requests a one-time checkpoint spawn in the saved scene.
- Starting a new game resets this narrow progress file in memory and overwrites it before loading the first scene.
- `MainMenuController.StartNewGame()` is available for a future separate “New Game” button; the existing start button continues when a save exists by default.

## Manual Play Mode Checklist

1. Delete or move `bunny-progress.json`, start from `MainMenu`, and verify all opening pages and the first scene fade.
2. Activate two checkpoints in one scene, die after each, and verify the latest one wins.
3. Quit after a checkpoint, relaunch, and verify the menu continues into that scene at the checkpoint.
4. Traverse every portal, including contact-only, key-activated, manually activated, and story-page variants.
5. Trigger manual and automatic NodeCanvas dialogues; verify movement remains locked until the correct dialogue finishes.
6. Trigger two systems that could lock input in sequence; verify one completion does not unlock the other.
7. Use a separate New Game button wired to `StartNewGame()` and verify previous checkpoint progress is not resumed.
