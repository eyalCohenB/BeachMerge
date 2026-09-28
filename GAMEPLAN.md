# Merge Town — build roadmap

A staged plan for building the Travel Town–style merge game yourself in Unity. Each
milestone lists what to build, the Unity/C# concepts it's meant to teach you, and a
self-check so you know when it's actually working before moving to the next one.
No code here on purpose — the point is you design and write every script.

Suggested order below is also a dependency order: each milestone assumes the previous
ones are working.

---

## 1. Project & scene setup

**Goal:** a scene with a camera framed on an empty board area, ready to hold content.

**Concepts to look up:**
- This project is already Unity 2D + URP (Universal Render Pipeline) — read up on what
  the 2D Renderer asset under `Assets/Settings` actually does.
- Sprite import settings: Pixels Per Unit, Filter Mode (Point vs Bilinear), Compression —
  these matter a lot for how crisp flat-vector art looks.
- Orthographic vs perspective camera, and how `Orthographic Size` maps to world units
  visible on screen.
- A sane `Assets/` folder convention (e.g. `Art/`, `Scripts/`, `Prefabs/`, `Data/`,
  `Scenes/`) before you have 50 files making a mess.

**Self-check:** you can place a sprite in the Scene view and see it framed sensibly by
the Game view at the aspect ratio you're targeting (e.g. a phone portrait ratio).

---

## 2. Data-driven items via ScriptableObject

**Goal:** each item in your merge chain (pebble, stone pile, shell, ...) exists as a
reusable data asset rather than a hardcoded value somewhere in a script.

**Concepts to look up:**
- `ScriptableObject` and `[CreateAssetMenu]` — how to define a custom data asset type
  and create instances of it in the Project window like any other asset.
- What fields such an item asset probably needs: an id/name, a sprite reference, a tier
  number, and a reference to "the item this merges into." Think about whether that
  "next tier" reference should point to another ScriptableObject directly.
- Why this beats a giant `switch` statement or hardcoded arrays once you have dozens of
  items — content changes become data changes, not code changes.

**Self-check:** you can create one ScriptableObject asset per item in the Project
window, assign each its sprite, and inspect/edit them without touching any script.

---

## 3. The board grid

**Goal:** a grid of empty "slot" GameObjects spawned by code at runtime.

**Concepts to look up:**
- GameObject/Transform parent-child hierarchy, and instantiating from a **prefab**
  (build one slot as a prefab, then spawn copies).
- Nested loops to lay out a grid, and converting a (row, column) index into a world or
  local position.
- Why you want a **logical board model** (e.g. a plain C# 2D array or `List` describing
  what's in each cell) that is separate from the visual GameObjects representing it —
  this is the "don't let your data model and your view get tangled" lesson, and it pays
  off hugely once you add save/load later.

**Self-check:** running the scene spawns a visible grid of empty slot sprites at the
size you choose, and you have a script-side data structure that knows the grid's
dimensions independent of the GameObjects.

---

## 4. Populating the board with greyed-out placeholders

**Goal:** on start, every board slot gets a random greyed-out/locked item assigned to it.

**Concepts to look up:**
- Weighted random selection (not every item should be equally likely).
- Changing a `SpriteRenderer`'s `color` (or swapping to a desaturated material) to show
  a "locked" visual state versus its normal full-color look.
- Updating your logical board model at the same time as the visuals, so the two never
  disagree about what's in a cell.

**Self-check:** starting the scene shows a full board of greyed-out items, each one
logically "assigned" a specific target item type that isn't visible to the player yet
(or is, depending on your design — Travel Town shows a silhouette hint).

---

## 5. The bucket & spawning items

**Goal:** clicking the bucket spawns a tier-1 item near it that isn't part of the board.

**Concepts to look up:**
- Detecting a click on a world-space GameObject (`OnMouseDown`) versus a UI `Button`
  `OnClick` — trade-offs of each for a game like this.
- `Instantiate()` — spawning a new GameObject from a prefab at runtime, and where it
  ends up in the hierarchy.
- The idea of "game state" as simple as tracking whether an item is currently sitting
  loose on the board waiting to be placed or merged.

**Self-check:** clicking the bucket produces one visible tier-1 item sprite on screen
each time, positioned somewhere sensible (e.g. bucket's location, or the first open
staging spot).

---

## 6. Drag and drop

**Goal:** the player can pick up a loose item and drag it anywhere on screen.

**Concepts to look up:**
- Pointer/touch input. This project already has an `InputSystem_Actions` asset, so
  it's worth learning the **new Input System** package rather than the legacy `Input`
  class — look at Action Maps, bindings, and reading a pointer position/press action
  from a script.
- Converting a screen-space pointer position into a world-space position
  (`Camera.ScreenToWorldPoint` and the inverse) so the dragged object tracks the
  pointer correctly.
- The pointer-down → pointer-drag → pointer-up lifecycle, and where each part of that
  belongs (`Update()` for continuous tracking, versus one-shot logic on press/release).

**Self-check:** you can press an item, move the mouse/finger, and the item follows
smoothly; releasing leaves it wherever you dropped it (even if nothing happens with it
yet).

---

## 7. Detecting a valid drop & merging into a board slot

**Goal:** dropping the correct item onto its matching greyed slot clears that slot.

**Concepts to look up:**
- Overlap/distance checks — either `Physics2D.OverlapCircle`/`OverlapBox` with
  colliders, or a simple "closest slot within some distance" calculation without
  physics at all. Consider which is more appropriate for a grid-snapped game like this.
- Comparing item identity (comparing references to your ScriptableObject asset, or an
  id field on it) between the dragged item and what the target slot expects.
- Updating the logical board model on a successful match, and only then reflecting that
  change visually (destroy the greyed placeholder, snap the real item into place, or
  swap sprites).

**Self-check:** dragging a matching item onto its greyed slot snaps it into place and
clears the "locked" look; dragging onto a non-matching slot does nothing (or gives
some rejection feedback).

---

## 8. The merge chain (combining two same-tier items)

**Goal:** dragging one loose item onto another loose item of the same tier merges them
into the next tier up.

**Concepts to look up:**
- Distinguishing "this drag target is a board slot" from "this drag target is another
  loose item" — your drop-detection logic from milestone 7 needs to branch on what kind
  of thing is underneath the dragged item.
- Detecting two loose items of matching tier overlapping, and promoting them: destroy
  both, instantiate the "next tier" item (read from the ScriptableObject chain
  reference you designed in milestone 2) at the drop location.
- Object pooling vs plain `Instantiate`/`Destroy` — fine to skip pooling for now, but
  worth knowing it exists and why larger games use it.

**Self-check:** two pebbles dragged together become one stone pile; two stone piles
become one shell; and so on up your whole chain, ending at your top-tier item.

---

## 9. Game loop / win state

**Goal:** the game knows when the board is fully cleared.

**Concepts to look up:**
- Tracking "how many greyed slots remain" as a simple counter on your board model,
  decremented on each successful clear rather than recomputed by scanning every frame.
- Event-driven state checks: react to a "slot cleared" event instead of polling
  everything in `Update()` — a good first taste of why `Update()` isn't the right place
  for most logic in a well-structured game.
- A lightweight central "game manager" script and the singleton pattern — useful for
  something every other script needs to reach (like "how many slots are left"), but
  worth understanding the coupling/testability cost of overusing singletons.

**Self-check:** clearing the last greyed slot triggers an observable "board complete"
event (even just a Debug.Log or a placeholder UI banner) exactly once, at the right
moment.

---

## 10. Currency, villager requests, bucket upgrades

**Goal:** villagers ask for specific items; fulfilling a request grants coins; coins
upgrade the bucket, which changes what it can produce.

**Concepts to look up:**
- A request/quest data structure — likely another small ScriptableObject or plain C#
  class: "which item is requested" + "coin reward."
- Matching a produced/merged item against the currently active request, and what
  happens to items that don't match anything (do they just sit around? get auto-stored?
  — your design call).
- A coins counter with simple UI (`Text` or `TextMeshPro`), and updating it reactively
  when a request is fulfilled.
- Persisting coins and bucket level between play sessions: `PlayerPrefs` for simple
  values, or a hand-rolled save file using `JsonUtility` if you want more structure
  (and this is where the "keep your data model separate from your GameObjects" habit
  from milestone 3 really pays off — it's much easier to serialize plain data).
- Gating which item tiers the bucket can drop behind the current upgrade level (a
  weighted table per bucket tier).

**Self-check:** fulfilling a villager's request increases your coin count; spending
enough coins on an upgrade visibly changes the bucket (sprite and/or its output range);
closing and reopening the game keeps your coins and bucket level.

---

## 11. Polish pass

**Goal:** make merges and drops feel good instead of instant/robotic.

**Concepts to look up:**
- Simple animation via coroutines and `Lerp`/`Mathf.SmoothStep` for a snap-into-place
  or merge-pop effect, or bring in the DOTween asset if you want a more ergonomic
  tweening API.
- `AudioSource.PlayOneShot` for merge/drop/coin sounds.
- A basic particle burst (Unity's built-in Particle System) on a successful merge.
- Only tackle this once milestones 1–10 are solid — polish on top of broken mechanics
  is wasted work.

**Self-check:** merges and matches have visible/audible feedback beyond an instant
sprite swap.

---

## Starter content reference

A first art pass (`Assets/Art/Sprites/...`) is generated separately — see the asset
pack for the concrete item chain (7 tiers: Pebble → Stone Pile → Shell → Shiny Shell →
Starfish → Golden Starfish → Pearl), board tile, 3 bucket upgrade stages, coin icon, and
a villager placeholder — enough to build and test milestones 1–10 end-to-end before
worrying about visual variety.

---

## Practical build reference — the actual scripts, objects, and data to create

This section turns the milestones above into a concrete parts list: which C# scripts
to create, what each one is attached to, what fields it should hold, and what it's
responsible for. **Field lists are a starting point, not a spec** — you'll adjust them
as you build. No method bodies or logic here on purpose; that's the part you design and
write. Types are written as `Type name` so you know what to declare, not what to type
verbatim.

### Scene hierarchy (what exists in the scene)

```
SampleScene
├─ Main Camera
├─ GameManager                  (empty GameObject — see GameManager script)
├─ DragController                (empty GameObject — see DragController script)
├─ Board                         (empty GameObject, parent for spawned BoardSlot instances)
├─ Bucket                        (SpriteRenderer + Collider2D + Bucket script)
├─ Villager                      (SpriteRenderer + VillagerView script)
└─ Canvas (UI)
   ├─ CoinsText                  (TextMeshProUGUI)
   ├─ RequestPanel               (Image for requested item sprite + reward text)
   └─ BucketUpgradePanel         (Button + cost text, hidden until affordable)
```

`GameManager` and `DragController` are empty GameObjects that exist purely to host a
script — this is a normal Unity pattern for "systems" that don't need a visual
presence in the world.

### Data assets (ScriptableObject)

These live in `Assets/Data/...` and are created via the Unity menu once you've written
the `[CreateAssetMenu]` class — one `.asset` file per item/tier, editable entirely in
the Inspector.

| Script | Fields to give it | Purpose | How many instances |
|---|---|---|---|
| `ItemData` | `string id`, `string displayName`, `int tier`, `Sprite fullSprite`, `Sprite greyedSprite`, `ItemData nextTierItem` | One data asset per item in the chain; `nextTierItem` is what two of this item merge into (null for the top tier) | 7 (Pebble → Pearl) |
| `BucketDropEntry` | `ItemData item`, `float weight` | One weighted entry in a bucket tier's drop table — not a `ScriptableObject` itself, just a plain `[Serializable]` class used inside `BucketTierData`'s list | n/a |
| `BucketTierData` | `int tierLevel`, `Sprite bucketSprite`, `int upgradeCost`, `List<BucketDropEntry> possibleDrops` | One asset per bucket upgrade stage — defines what it can drop and what the *next* upgrade costs | 3 (wood/tin/gold) |
| `SaveData` | `int coins`, `int bucketTier`, plain serializable data describing board cell contents | Not a ScriptableObject — a plain `[Serializable]` class instantiated at runtime and written to disk via `JsonUtility`, not created as an asset | 1 (runtime only) |

### Runtime scripts (MonoBehaviour)

| Script | Attached to | Fields to give it | Responsible for |
|---|---|---|---|
| `BoardCell` | *(not a MonoBehaviour — plain C# class)* | `int row`, `int col`, `ItemData targetItem`, `bool isCleared` | The logical state of one board position; lives inside `BoardManager`'s data, has no GameObject of its own |
| `BoardManager` | `GameManager` | `int rows`, `int columns`, `GameObject boardSlotPrefab`, `Transform boardParent`, `float cellSpacing`, `BoardCell[,] cells`, `List<ItemData> allItems` (for weighted picks) | Spawning the grid of `BoardSlot` instances at start, assigning each cell a random target `ItemData`, tracking how many cells remain locked, exposing a method the drop-resolution logic calls to clear a cell, raising a "board cleared" event |
| `BoardSlotView` | `BoardSlot` prefab | `SpriteRenderer greyedRenderer`, `int row`, `int col` | Purely visual: shows the greyed sprite for whatever `ItemData` its `BoardCell` was assigned, plays a "cleared" transition when told to by `BoardManager` |
| `Bucket` | `Bucket` GameObject | `BucketTierData currentTier`, `GameObject mergeItemPrefab`, `Transform spawnPoint` | Detecting a click (world-space `Collider2D` + pointer input, or a UI `Button` if you place it in the Canvas instead), picking a weighted-random `ItemData` from `currentTier.possibleDrops`, instantiating a `MergeItem` at `spawnPoint` |
| `MergeItem` | `MergeItem` prefab (spawned at runtime) | `ItemData data`, `SpriteRenderer spriteRenderer`, `Collider2D col` | Represents one loose, draggable item on the board. Knows its own `ItemData` and how to display it; drag movement itself is driven by `DragController`, not this script — keep input handling in one place |
| `DragController` | `DragController` GameObject | `MergeItem heldItem`, `Camera mainCamera`, reference to your Input Actions asset (`InputSystem_Actions`) | The single owner of "what's currently being dragged." On pointer-down, finds the `MergeItem` under the pointer (`Physics2D.OverlapPoint` or similar) and grabs it; on pointer-drag, moves `heldItem`'s transform to the pointer's world position every frame; on pointer-up, releases it and asks `GameManager` to resolve the drop |
| `GameManager` | `GameManager` GameObject | References to `BoardManager`, `CurrencyManager`, `VillagerManager`, `Bucket` (a simple singleton, e.g. `public static GameManager Instance`) | The central coordinator other scripts talk to instead of finding each other directly. Owns "resolve a drop": given a released `MergeItem` and what's underneath it, decide whether that's a matching board slot (place + clear), a same-tier loose item (merge to next tier), or nothing (leave it / snap back) |
| `CurrencyManager` | `GameManager` GameObject | `int coins`, an event other scripts can subscribe to (e.g. `Action<int> OnCoinsChanged`) | Adding/spending coins, notifying the UI when the total changes |
| `VillagerManager` | `Villager` GameObject | `ItemData currentRequestItem`, `int currentReward`, `List<ItemData> possibleRequestPool` | Picking a new random request, checking a produced item against the active request, granting coins via `CurrencyManager` on a match and rolling a new request |
| `BucketUpgradeUI` | `BucketUpgradePanel` (Canvas) | References to `Bucket`, `CurrencyManager`, the upgrade `Button`, cost `Text` | Showing/hiding itself based on affordability, handling the upgrade button click, swapping the `Bucket`'s `currentTier` to the next `BucketTierData` |
| `SaveManager` | `GameManager` GameObject | Reference to everything it needs to read for a save (`CurrencyManager`, `Bucket`, `BoardManager`) | Serializing a `SaveData` instance via `JsonUtility` to `Application.persistentDataPath` (or `PlayerPrefs` for the simple version) on relevant events, and loading it back on scene start |

### Prefabs to create

| Prefab | Contains | Notes |
|---|---|---|
| `BoardSlot.prefab` | `SpriteRenderer` (using `board_slot.png` as background) + a child `SpriteRenderer` for the greyed item + `BoardSlotView` | Instantiated once per cell by `BoardManager` |
| `MergeItem.prefab` | `SpriteRenderer` + `Collider2D` (a `CircleCollider2D` sized to the item art) + `MergeItem` | Instantiated by `Bucket` on click, and again whenever two items merge into the next tier |

`Bucket` and `Villager` don't need to be prefabs — there's exactly one of each in the
scene, so a plain scene GameObject is simpler than a prefab you'd only ever instantiate
once.

### Suggested project folder layout

```
Assets/
├─ Art/Sprites/...        (already generated)
├─ Data/
│  ├─ Items/              ItemData_Pebble.asset ... ItemData_Pearl.asset
│  └─ Bucket/             BucketTier1.asset, BucketTier2.asset, BucketTier3.asset
├─ Prefabs/
│  ├─ BoardSlot.prefab
│  └─ MergeItem.prefab
├─ Scripts/
│  ├─ Data/                ItemData.cs, BucketTierData.cs, BucketDropEntry.cs
│  ├─ Board/                BoardManager.cs, BoardSlotView.cs, BoardCell.cs
│  ├─ Items/                MergeItem.cs, Bucket.cs
│  ├─ Input/                 DragController.cs
│  ├─ Core/                  GameManager.cs, CurrencyManager.cs, VillagerManager.cs, SaveManager.cs
│  └─ UI/                    BucketUpgradeUI.cs
└─ Scenes/SampleScene.unity
```

### Build order matched to the milestones above

1. Create `ItemData` + the 7 item assets (milestone 2).
2. Create `BoardCell`, `BoardManager`, `BoardSlot.prefab` + `BoardSlotView` (milestones 3–4).
3. Create `Bucket`, `MergeItem`, `MergeItem.prefab` (milestone 5).
4. Create `DragController` (milestone 6).
5. Create `GameManager` and give it the drop-resolution responsibility — first just
   handle "place on matching slot" (milestone 7), then extend it to also handle
   "merge two loose items" (milestone 8).
6. Add the remaining-slots counter and a cleared event to `BoardManager` (milestone 9).
7. Create `BucketTierData` + 3 assets, `CurrencyManager`, `VillagerManager`,
   `BucketUpgradeUI` (milestone 10).
8. Create `SaveManager` last, once there's meaningful state worth persisting.
9. Polish (milestone 11) touches whatever's already there — no new core scripts.
