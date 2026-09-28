using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class PlaytestBot
{
    const string ScenePath = "Assets/Scenes/SampleScene.unity";

    Mouse mouse;
    Camera cam;
    GameManager game;
    string screenshotDir;
    int shotIndex;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        SaveManager.SavePathOverride = Path.Combine(Application.temporaryCachePath, "playtest_save.json");
        if (File.Exists(SaveManager.SavePathOverride)) File.Delete(SaveManager.SavePathOverride);

        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        mouse = InputSystem.AddDevice<Mouse>();
        yield return null;
    }

    IEnumerator LoadGame(int seed)
    {
        screenshotDir = Path.Combine(Application.dataPath, "..", "PlaytestScreenshots", seed.ToString());
        Directory.CreateDirectory(screenshotDir);
        shotIndex = 0;

        Random.InitState(seed);
#if UNITY_EDITOR
        yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
            ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#endif
        yield return null;
        yield return null;

        cam = Camera.main;
        game = GameManager.Instance;
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        InputSystem.RemoveDevice(mouse);
        SaveManager.SavePathOverride = null;
        yield return null;
    }

    [UnityTest]
    public IEnumerator FullCoreLoop([Values(12345, 7, 2026)] int seed)
    {
        yield return LoadGame(seed);
        BoardManager board = game.boardManager;
        Screenshot("start");

        // Board spawns: 24 playable cells, 8 open around the bucket, 16 greyed.
        var cells = board.cells.Cast<BoardCell>().Where(c => c != null).ToList();
        Assert.AreEqual(24, cells.Count, "board should have 24 playable cells");
        Assert.AreEqual(8, cells.Count(c => c.state == CellState.Empty), "cells around bucket should start empty");
        Assert.AreEqual(16, cells.Count(c => c.state == CellState.Locked), "rest of board should start greyed");
        Assert.IsNotNull(game.bucket.GetComponent<SpriteRenderer>().sprite, "bucket should have a sprite");

        // Click the bucket 3 times -> 3 items land in empty cells.
        for (int i = 0; i < 3; i++) yield return Click(game.bucket.transform.position);
        Screenshot("after_3_bucket_clicks");
        var items = Items();
        Assert.AreEqual(3, items.Length, "3 bucket clicks should produce 3 items");
        Assert.IsTrue(items.All(i => i.boardRow >= 0), "every spawned item should sit in a cell");

        // Merge two pebbles by dragging one onto the other.
        MergeItem a = items[0], b = items[1];
        Assert.AreEqual(a.data, b.data, "tier-1 bucket only drops pebbles");
        int aRow = a.boardRow, aCol = a.boardCol, bRow = b.boardRow, bCol = b.boardCol;
        ItemData expected = a.data.nextTierItem;
        yield return Drag(a.transform.position, b.transform.position);
        Screenshot("after_merge");
        Assert.AreEqual(CellState.Empty, board.GetCell(aRow, aCol).state, "source cell should be freed by a merge");
        Assert.AreEqual(expected, board.GetCell(bRow, bCol).item, "target cell should hold the next tier");
        Assert.AreEqual(2, Items().Length);

        // Drop onto a matching greyed cell -> merges and unlocks it.
        MergeItem pebble = Items().First(i => i.data.tier == 1);
        BoardCell lockedPebble = cells.FirstOrDefault(c => c.state == CellState.Locked && c.item == pebble.data);
        if (lockedPebble != null)
        {
            int lockedBefore = board.RemainingLocked;
            yield return Drag(pebble.transform.position, board.GetWorldPosition(lockedPebble.row, lockedPebble.col));
            Screenshot("after_unlock");
            Assert.AreEqual(CellState.Filled, lockedPebble.state, "greyed cell should unlock");
            Assert.AreEqual(pebble.data.nextTierItem ?? pebble.data, lockedPebble.item);
            Assert.AreEqual(lockedBefore - 1, board.RemainingLocked);
        }

        // Dropping onto a non-matching greyed cell snaps back.
        MergeItem any = Items().First();
        BoardCell mismatch = cells.FirstOrDefault(c => c.state == CellState.Locked && c.item != any.data);
        if (mismatch != null)
        {
            Vector3 home = any.transform.position;
            yield return Drag(home, board.GetWorldPosition(mismatch.row, mismatch.col));
            Assert.AreEqual(home, any.transform.position, "invalid drop should snap back");
            Assert.AreEqual(CellState.Locked, mismatch.state);
        }

        // Feed the villager.
        MergeItem gift = Items().First();
        game.villagerManager.SetRequest(gift.data);
        int coinsBefore = game.currencyManager.coins;
        int giftRow = gift.boardRow, giftCol = gift.boardCol;
        yield return Drag(gift.transform.position, game.villagerManager.transform.position);
        Screenshot("after_villager");
        Assert.Greater(game.currencyManager.coins, coinsBefore, "villager should pay coins");
        Assert.AreEqual(CellState.Empty, board.GetCell(giftRow, giftCol).state, "delivered item's cell should free up");

        LogAssert.NoUnexpectedReceived();
    }

    static MergeItem[] Items()
    {
        return Object.FindObjectsByType<MergeItem>(FindObjectsSortMode.None)
            .OrderBy(i => i.boardRow).ThenBy(i => i.boardCol).ToArray();
    }

    IEnumerator Click(Vector3 world)
    {
        Vector2 screen = cam.WorldToScreenPoint(world);
        yield return SetMouse(screen, true);
        yield return SetMouse(screen, false);
        yield return null;
    }

    IEnumerator Drag(Vector3 fromWorld, Vector3 toWorld)
    {
        Vector2 from = cam.WorldToScreenPoint(fromWorld);
        Vector2 to = cam.WorldToScreenPoint(toWorld);
        yield return SetMouse(from, false);
        yield return SetMouse(from, true);
        yield return SetMouse(Vector2.Lerp(from, to, 0.5f), true);
        yield return SetMouse(to, true);
        yield return SetMouse(to, false);
        yield return null;
    }

    IEnumerator SetMouse(Vector2 screen, bool pressed)
    {
        mouse.MakeCurrent();
        var state = new MouseState { position = screen };
        if (pressed) state = state.WithButton(MouseButton.Left);
        InputSystem.QueueStateEvent(mouse, state);
        yield return null;
    }

    void Screenshot(string label)
    {
        SaveCameraShot(label + "_landscape", 1280, 720);
        SaveCameraShot(label + "_portrait", 720, 1280);
        shotIndex++;
    }

    void SaveCameraShot(string label, int width, int height)
    {
        var rt = new RenderTexture(width, height, 24);
        RenderTexture previousTarget = cam.targetTexture;
        cam.targetTexture = rt;
        cam.Render();
        cam.targetTexture = previousTarget;

        RenderTexture previousActive = RenderTexture.active;
        RenderTexture.active = rt;
        var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        RenderTexture.active = previousActive;

        File.WriteAllBytes(Path.Combine(screenshotDir, $"{shotIndex:00}_{label}.png"), tex.EncodeToPNG());
        Object.Destroy(tex);
        rt.Release();
    }
}
