using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class PlaytestBot
{
    const string ScenePath = "Assets/Scenes/SampleScene.unity";

    Mouse mouse;
    Camera cam;
    GameManager game;
    UIManager ui;
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

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        InputSystem.RemoveDevice(mouse);
        // Destroy the game first: otherwise its save-on-quit fires after the override is cleared
        // and writes the bot's progress into the player's real save file.
        if (game != null) Object.Destroy(game.gameObject);
        yield return null;
        SaveManager.SavePathOverride = null;
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
        ui = game.UI;
    }

    [UnityTest]
    public IEnumerator FullGameFlow([Values(12345, 7, 2026)] int seed)
    {
        yield return LoadGame(seed);
        BoardManager board = game.boardManager;

        // Start menu comes first; the board isn't spawned until Play.
        Assert.AreEqual(GameState.Menu, game.State);
        Assert.IsTrue(ui.MenuPanel.activeSelf, "start menu should be showing");
        Assert.IsNull(board.cells, "board should not spawn before Play");
        Assert.IsNotNull(game.Music.Clip, "music clip should load");
        Screenshot("menu");

        yield return ClickUI(ui.PlayButton);
        Assert.AreEqual(GameState.Playing, game.State, "Play should start the game");
        Assert.IsFalse(ui.MenuPanel.activeSelf);
        Screenshot("start");

        var cells = board.cells.Cast<BoardCell>().Where(c => c != null).ToList();
        Assert.AreEqual(24, cells.Count, "board should have 24 playable cells");
        Assert.AreEqual(8, cells.Count(c => c.state == CellState.Empty), "cells around bucket should start empty");
        Assert.AreEqual(16, cells.Count(c => c.state == CellState.Locked), "rest of board should start greyed");

        for (int i = 0; i < 3; i++) yield return Click(game.bucket.transform.position);
        var items = Items();
        Assert.AreEqual(3, items.Length, "3 bucket clicks should produce 3 items");
        Assert.IsTrue(items.All(i => i.boardRow >= 0), "every spawned item should sit in a cell");

        MergeItem a = items[0], b = items[1];
        int aRow = a.boardRow, aCol = a.boardCol, bRow = b.boardRow, bCol = b.boardCol;
        ItemData expected = a.data.nextTierItem;
        yield return Drag(a.transform.position, b.transform.position);
        Assert.AreEqual(CellState.Empty, board.GetCell(aRow, aCol).state, "source cell should be freed by a merge");
        Assert.AreEqual(expected, board.GetCell(bRow, bCol).item, "target cell should hold the next tier");

        MergeItem pebble = Items().First(i => i.data.tier == 1);
        BoardCell lockedPebble = cells.FirstOrDefault(c => c.state == CellState.Locked && c.item == pebble.data);
        if (lockedPebble != null)
        {
            int lockedBefore = board.RemainingLocked;
            yield return Drag(pebble.transform.position, board.GetWorldPosition(lockedPebble.row, lockedPebble.col));
            Assert.AreEqual(CellState.Filled, lockedPebble.state, "greyed cell should unlock");
            Assert.AreEqual(lockedBefore - 1, board.RemainingLocked);
        }

        MergeItem any = Items().First();
        BoardCell mismatch = cells.FirstOrDefault(c => c.state == CellState.Locked && c.item != any.data);
        if (mismatch != null)
        {
            Vector3 home = any.transform.position;
            yield return Drag(home, board.GetWorldPosition(mismatch.row, mismatch.col));
            Assert.AreEqual(home, any.transform.position, "invalid drop should snap back");
        }

        // Feeding the villager pays coins and writes them to the save file.
        MergeItem gift = Items().First();
        game.villagerManager.SetRequest(gift.data);
        int coinsBefore = game.currencyManager.coins;
        yield return Drag(gift.transform.position, game.villagerManager.transform.position);
        Screenshot("after_villager");
        Assert.Greater(game.currencyManager.coins, coinsBefore, "villager should pay coins");
        Assert.AreEqual(game.currencyManager.coins, ReadSave().coins, "coins should be saved to disk");

        // Clearing every greyed cell shows the finish screen.
        foreach (BoardCell cell in cells.Where(c => c.state == CellState.Locked).ToList())
        {
            board.SpawnItemInCell(cell, cell.item);
        }
        yield return null;
        Assert.AreEqual(GameState.Finished, game.State, "clearing all greyed items should finish the board");
        Assert.IsTrue(ui.FinishPanel.activeSelf, "finish screen should show");
        Screenshot("finished");

        yield return ClickUI(ui.PlayAgainButton);
        Assert.AreEqual(GameState.Playing, game.State);
        Assert.AreEqual(16, board.RemainingLocked, "play again should deal a fresh board");
        Assert.AreEqual(0, Items().Length, "old items should be cleared on restart");

        yield return ClickUI(ui.HudMenuButton);
        Assert.AreEqual(GameState.Menu, game.State, "Menu button should return to the start menu");

        // Upgrade the bucket from the menu.
        game.currencyManager.SetCoins(60);
        yield return null;
        Assert.IsTrue(ui.UpgradeButton.interactable, "upgrade should be affordable at 60 coins");
        yield return ClickUI(ui.UpgradeButton);
        Assert.AreEqual(2, game.bucket.currentTier.tierLevel, "bucket should upgrade to level 2");
        Assert.AreEqual(10, game.currencyManager.coins, "upgrade should cost 50 coins");
        Assert.AreEqual(game.bucket.currentTier.bucketSprite, game.bucket.GetComponent<SpriteRenderer>().sprite);
        Assert.AreEqual(2, ReadSave().bucketTier, "bucket level should be saved to disk");
        Assert.IsFalse(ui.UpgradeButton.interactable, "next upgrade should be unaffordable");
        Screenshot("menu_upgraded");

        yield return ClickUI(ui.MusicButton);
        Assert.IsFalse(game.MusicOn, "music toggle should turn music off");
        Assert.IsFalse(ReadSave().musicOn, "music preference should be saved");

        // Delete progress needs two taps.
        yield return ClickUI(ui.ResetButton);
        Assert.AreEqual(10, game.currencyManager.coins, "first tap should only ask for confirmation");
        Screenshot("reset_confirm");
        yield return ClickUI(ui.ResetButton);
        Assert.AreEqual(0, game.currencyManager.coins, "confirming should wipe coins");
        Assert.AreEqual(1, game.bucket.currentTier.tierLevel, "confirming should reset the bucket");
        Assert.AreEqual(0, ReadSave().coins);
        Assert.AreEqual(1, ReadSave().bucketTier);

        LogAssert.NoUnexpectedReceived();
    }

    static SaveData ReadSave() => JsonUtility.FromJson<SaveData>(File.ReadAllText(SaveManager.SavePathOverride));

    static MergeItem[] Items()
    {
        return Object.FindObjectsByType<MergeItem>()
            .OrderBy(i => i.boardRow).ThenBy(i => i.boardCol).ToArray();
    }

    IEnumerator ClickUI(Button button)
    {
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, button.transform.position);
        yield return SetMouse(screen, false);
        yield return SetMouse(screen, true);
        yield return SetMouse(screen, false);
    }

    IEnumerator Click(Vector3 world)
    {
        Vector2 screen = cam.WorldToScreenPoint(world);
        yield return SetMouse(screen, false);
        yield return SetMouse(screen, true);
        yield return SetMouse(screen, false);
    }

    IEnumerator Drag(Vector3 fromWorld, Vector3 toWorld)
    {
        Vector2 from = cam.WorldToScreenPoint(fromWorld);
        Vector2 to = cam.WorldToScreenPoint(toWorld);
        yield return SetMouse(from, false);
        yield return SetMouse(from, true);
        Assert.IsNotNull(Object.FindFirstObjectByType<DragController>().heldItem, $"bot failed to pick up the item at {fromWorld}");
        yield return SetMouse(Vector2.Lerp(from, to, 0.5f), true);
        yield return SetMouse(to, true);
        yield return SetMouse(to, false);
    }

    // Holds each mouse state for two frames, like a real hand would, so a press is never
    // first noticed after the pointer has already moved on.
    IEnumerator SetMouse(Vector2 screen, bool pressed)
    {
        mouse.MakeCurrent();
        var state = new MouseState { position = screen };
        if (pressed) state = state.WithButton(MouseButton.Left);
        InputSystem.QueueStateEvent(mouse, state);
        yield return null;
        yield return null;
    }

    void Screenshot(string label)
    {
        SaveCameraShot(label + "_landscape", Screen.width, Screen.height);
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
