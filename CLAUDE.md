# CLAUDE.md

Beach Merge: a small 2D merge-puzzle game in Unity, modelled on the board in *Travel Town*. This is a learning project; the owner writes some scripts by hand and uses Claude for wiring and fixes, so explain non-obvious choices when changing things.

- **Unity:** 6000.4.6f1 (Unity 6), URP 2D renderer, new Input System (`com.unity.inputsystem`), uGUI (legacy `Text`, not TextMeshPro).
- **Only scene:** `Assets/Scenes/SampleScene.unity`. A fresh clone opens an empty "Untitled" scene because the last-opened scene is stored in the git-ignored `Library/`; open `SampleScene` manually.
- `README.md` is the player-facing guide. `GAMEPLAN.md` is the original learning roadmap; the code has since diverged from its "parts list" (see below).

## Commands

- Play: open `SampleScene`, press Play.
- Automated playtest (PlayMode test, drives a simulated mouse through the whole game on 3 seeded boards):
  - Editor: Window > General > Test Runner > PlayMode.
  - Headless: `.\tools\playtest.ps1` (copies the project to `%LOCALAPPDATA%\Temp\ttplay` so it works while the Editor is open; hardcodes the Unity path under `C:\Program Files\Unity\Hub\Editor\6000.4.6f1`).
- Rebuild music: `python tools/generate_music.py` (NumPy) → `Assets/Resources/Audio/beach_club_loop.wav`.

## Architecture

All game code is in `Assets/Scripts` (assembly `TravelTown.asmdef`); tests are in `Assets/Tests/PlayMode` (assembly `TravelTown.Tests.PlayMode`, references `TravelTown`).

### What is in the scene vs. created at runtime

In edit mode the scene holds only: `Main Camera` (orthographic, size 5), `GameManager`, `DragController`, `Board` (with child `Bucket`), `Villager`. Everything else is built in code on Play:

- `GameManager.Awake` does `AddComponent<MusicPlayer>()` and `AddComponent<UIManager>()`.
- `UIManager` builds the EventSystem, the Canvas and all panels/buttons in code (no UI in the scene, no UI prefabs).
- `BoardManager.SpawnBoard` instantiates `BoardSlot` prefabs and `MergeItem` prefabs.
- `VillagerManager` creates its `RequestIcon` child and loads its sprite from `Resources`.

### Component → GameObject map (scene wiring)

| GameObject | Components | Inspector references |
|---|---|---|
| GameManager | `GameManager`, `CurrencyManager`, `SaveManager` (+ runtime `MusicPlayer`, `UIManager`) | boardManager, currencyManager, villagerManager, bucket, saveManager; `mergeCheckRadius` 0.5 |
| Board | `BoardManager` | BoardSlot + MergeItem prefabs, boardParent = Board, 7 ItemData assets; 5×5, `cellSpacing` 1.1, bucket at (2,2) |
| Board/Bucket | SpriteRenderer, BoxCollider2D, `Bucket` | 3 BucketTierData assets, MergeItem prefab |
| DragController | `DragController` | mainCamera |
| Villager | SpriteRenderer, BoxCollider2D, `VillagerManager` | 7 ItemData assets as request pool |

Prefabs: `BoardSlot` (tile SpriteRenderer + BoxCollider2D + `BoardSlotView`, child `GreyedItem` renderer), `MergeItem` (SpriteRenderer sorting 2 + CircleCollider2D + `MergeItem`). Art is 512 px at 100 PPU, scaled 0.2 → ~1 world unit per cell.

### Data (ScriptableObjects in `Assets/Data`)

- `ItemData`: id, displayName, tier (1–7), fullSprite, greyedSprite, `nextTierItem` (linked list forming the merge chain; null for Pearl).
- `BucketTierData`: tierLevel, bucketSprite, `upgradeCost` (cost to go from *this* tier to the next), `possibleDrops` list of `BucketDropEntry { item, weight }`.
- `SaveData` is a plain `[Serializable]` class, not an asset: coins, bucketTier, musicOn, version.

### Flow

- `GameState { Menu, Playing, Finished }` lives on `GameManager`; `GameManager.Instance` is a singleton used by `Bucket` and `DragController`.
- Communication is event-based: `CurrencyManager.OnCoinsChanged`, `BoardManager.OnLockedCountChanged` / `OnBoardCleared`, `GameManager.OnStateChanged` / `OnBucketChanged`. `UIManager` subscribes and calls `Refresh()`.
- Board model: `BoardCell[,] cells` with `CellState { Locked, Filled, Empty }` is the source of truth; `BoardSlotView` only renders it. The bucket's cell has a tile but `cells[bucketRow, bucketCol]` is null.
- Input: `Bucket` and `DragController` each poll `Pointer.current` in `Update` (mouse and touch). `DragController.inputActions` is currently unused. The `isDown && !wasDown` check is a fallback for presses the playtest bot injects.
- Drop resolution (`GameManager.ResolveDrop`): `Physics2D.OverlapCircleAll` at the drop point → villager first (fulfil request), then the closest `BoardSlotView` → `TryResolveCell`: empty → move; same item → merge to `nextTierItem` (locked cell unlocks; a Pearl onto a locked Pearl just unlocks it); anything else → snap back.
- Saving: only coins, bucket tier and music are persisted to `Application.persistentDataPath/save.json`. The board is regenerated each round. Bump `SaveData.CurrentVersion` when the format changes (older saves are discarded). Tests redirect via `SaveManager.SavePathOverride`.

## Conventions

- `Resources.Load` is used for assets referenced only from code (`Audio/beach_club_loop`, `UI/coin_icon`, `Characters/villager_placeholder`); keep those paths in sync with the constants in code.
- Scene/prefab/asset YAML is committed; `Library/`, `UserSettings/`, `Temp/`, generated `.csproj`/`.sln` are ignored. Always commit `.meta` files together with their assets (including folder `.meta` files) or GUID references break on other machines.
- When changing gameplay, update `PlaytestBot.cs` so the playtest still covers it, and keep `README.md` tables (drop rates, costs) in sync with the `.asset` files.
