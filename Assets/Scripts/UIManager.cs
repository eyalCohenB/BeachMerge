using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    static readonly Color Ocean = new Color(0.12f, 0.37f, 0.55f, 1f);
    static readonly Color Sand = new Color(0.96f, 0.89f, 0.76f);
    static readonly Color Coral = new Color(1f, 0.5f, 0.33f);
    static readonly Color Teal = new Color(0.18f, 0.72f, 0.68f);
    static readonly Color Ink = new Color(0.25f, 0.18f, 0.12f);
    static readonly Color Dim = new Color(0f, 0.08f, 0.15f, 0.65f);

    public GameObject MenuPanel { get; private set; }
    public GameObject HudPanel { get; private set; }
    public GameObject FinishPanel { get; private set; }

    public Button PlayButton { get; private set; }
    public Button UpgradeButton { get; private set; }
    public Button MusicButton { get; private set; }
    public Button RestartButton { get; private set; }
    public Button HudMenuButton { get; private set; }
    public Button PlayAgainButton { get; private set; }
    public Button FinishMenuButton { get; private set; }
    public Button ResetButton { get; private set; }

    GameManager game;
    CanvasScaler scaler;
    Font font;
    Text resetLabel;
    float resetConfirmUntil;
    Sprite rounded;
    Sprite coinSprite;

    Text menuCoins, hudCoins, bucketLevel, bucketDrops, upgradeLabel, musicLabel, lockedCounter, finishEarned, finishTotal;
    Image bucketImage;

    public void Init(GameManager gameManager)
    {
        game = gameManager;
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        rounded = CreateRoundedSprite(64, 22);
        coinSprite = Resources.Load<Sprite>("UI/coin_icon");

        EnsureEventSystem();
        Transform canvas = CreateCanvas();
        BuildMenu(canvas);
        BuildHud(canvas);
        BuildFinish(canvas);

        game.OnStateChanged += ShowState;
        game.OnBucketChanged += Refresh;
        game.currencyManager.OnCoinsChanged += _ => Refresh();
        game.boardManager.OnLockedCountChanged += Refresh;
    }

    void ShowState(GameState state)
    {
        MenuPanel.SetActive(state == GameState.Menu);
        HudPanel.SetActive(state != GameState.Menu);
        FinishPanel.SetActive(state == GameState.Finished);
        Refresh();
    }

    public void Refresh()
    {
        if (MenuPanel == null) return;

        string coins = game.currencyManager.coins.ToString();
        menuCoins.text = coins;
        hudCoins.text = coins;

        BucketTierData tier = game.bucket.currentTier;
        if (tier != null)
        {
            bucketLevel.text = $"Bucket level {tier.tierLevel}";
            bucketImage.sprite = tier.bucketSprite;
            bucketDrops.text = DescribeDrops(tier);
        }

        bool hasNext = game.NextBucketTier != null;
        upgradeLabel.text = hasNext ? $"Upgrade bucket  -  {game.UpgradeCost} coins" : "Bucket fully upgraded";
        UpgradeButton.interactable = game.CanUpgradeBucket;

        musicLabel.text = game.MusicOn ? "Music: On" : "Music: Off";
        resetLabel.text = ResetPending ? "Tap again to confirm" : "Delete progress";
        lockedCounter.text = $"Greyed items left: {game.boardManager.RemainingLocked}";
        finishEarned.text = $"+{game.CoinsEarnedThisRun} coins this round";
        finishTotal.text = $"Total: {game.currencyManager.coins} coins";
    }

    bool ResetPending => Time.unscaledTime < resetConfirmUntil;

    void Update()
    {
        if (scaler != null) FitToScreen();
        if (resetConfirmUntil > 0 && !ResetPending)
        {
            resetConfirmUntil = 0;
            Refresh();
        }
    }

    // Landscape screens fit by height, portrait by width, so panels never spill off-screen.
    void FitToScreen()
    {
        scaler.matchWidthOrHeight = Screen.width >= Screen.height ? 1f : 0f;
    }

    void OnResetClicked()
    {
        if (ResetPending)
        {
            resetConfirmUntil = 0;
            game.ResetProgress();
        }
        else
        {
            resetConfirmUntil = Time.unscaledTime + 3f;
        }
        Refresh();
    }

    static string DescribeDrops(BucketTierData tier)
    {
        float total = tier.possibleDrops.Sum(d => d.weight);
        var sb = new StringBuilder("Drops:");
        foreach (BucketDropEntry entry in tier.possibleDrops)
        {
            sb.Append($"\n{entry.item.displayName}  {Mathf.RoundToInt(entry.weight / total * 100)}%");
        }
        return sb.ToString();
    }

    // ---------------------------------------------------------------- panels

    void BuildMenu(Transform canvas)
    {
        MenuPanel = Panel(canvas, "MenuPanel", Ocean, true);
        Transform root = MenuPanel.transform;

        Label(root, "Beach Merge", 110, Color.white, new Vector2(0, 480), new Vector2(900, 150), true);
        menuCoins = CoinPill(root, new Vector2(0, 340));

        Image card = Box(root, "BucketCard", Sand, new Vector2(0, 50), new Vector2(780, 440));
        bucketImage = Icon(card.transform, null, new Vector2(-220, 0), new Vector2(260, 260));
        bucketLevel = Label(card.transform, "", 50, Ink, new Vector2(110, 130), new Vector2(460, 70));
        bucketDrops = Label(card.transform, "", 36, Ink, new Vector2(110, -40), new Vector2(460, 260));
        bucketDrops.alignment = TextAnchor.UpperLeft;
        bucketLevel.alignment = TextAnchor.MiddleLeft;

        UpgradeButton = MakeButton(root, "UpgradeButton", "", Teal, new Vector2(0, -240), new Vector2(760, 120), out upgradeLabel,
            () => game.TryUpgradeBucket());
        PlayButton = MakeButton(root, "PlayButton", "PLAY", Coral, new Vector2(0, -400), new Vector2(760, 150), out _,
            () => game.StartGame());
        MusicButton = MakeButton(root, "MusicButton", "", new Color(1, 1, 1, 0.25f), new Vector2(-195, -545), new Vector2(370, 90), out musicLabel,
            () => { game.SetMusic(!game.MusicOn); Refresh(); });
        ResetButton = MakeButton(root, "ResetButton", "", new Color(0.85f, 0.3f, 0.3f, 0.85f), new Vector2(195, -545), new Vector2(370, 90), out resetLabel,
            OnResetClicked);
    }

    void BuildHud(Transform canvas)
    {
        HudPanel = Panel(canvas, "HudPanel", Color.clear, false);
        Transform root = HudPanel.transform;

        Image pill = Box(root, "HudCoins", new Color(0, 0, 0, 0.35f), new Vector2(-40, -40), new Vector2(300, 100));
        Anchor(pill.rectTransform, new Vector2(1, 1));
        Icon(pill.transform, coinSprite, new Vector2(-95, 0), new Vector2(110, 110));
        hudCoins = Label(pill.transform, "0", 56, Color.white, new Vector2(40, 0), new Vector2(180, 90), true);

        lockedCounter = Label(root, "", 44, Color.white, new Vector2(0, 220), new Vector2(900, 70), true);
        Anchor(lockedCounter.rectTransform, new Vector2(0.5f, 0));

        RestartButton = MakeButton(root, "RestartButton", "Restart", Coral, new Vector2(-200, 60), new Vector2(360, 110), out _,
            () => game.StartGame());
        Anchor((RectTransform)RestartButton.transform, new Vector2(0.5f, 0));
        HudMenuButton = MakeButton(root, "HudMenuButton", "Menu", Teal, new Vector2(200, 60), new Vector2(360, 110), out _,
            () => game.ReturnToMenu());
        Anchor((RectTransform)HudMenuButton.transform, new Vector2(0.5f, 0));
    }

    void BuildFinish(Transform canvas)
    {
        FinishPanel = Panel(canvas, "FinishPanel", Dim, true);
        Image card = Box(FinishPanel.transform, "FinishCard", Sand, Vector2.zero, new Vector2(820, 780));
        Transform root = card.transform;

        Label(root, "Board cleared!", 84, Coral, new Vector2(0, 285), new Vector2(780, 120), true);
        Icon(root, coinSprite, new Vector2(0, 150), new Vector2(150, 150));
        finishEarned = Label(root, "", 50, Ink, new Vector2(0, 40), new Vector2(780, 70));
        finishTotal = Label(root, "", 40, Ink, new Vector2(0, -20), new Vector2(780, 60));

        PlayAgainButton = MakeButton(root, "PlayAgainButton", "Play again", Coral, new Vector2(0, -140), new Vector2(600, 120), out _,
            () => game.StartGame());
        FinishMenuButton = MakeButton(root, "FinishMenuButton", "Menu", Teal, new Vector2(0, -280), new Vector2(600, 100), out _,
            () => game.ReturnToMenu());
    }

    // ---------------------------------------------------------------- building blocks

    void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;

        var es = new GameObject("EventSystem", typeof(EventSystem));
        es.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
    }

    Transform CreateCanvas()
    {
        var go = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = Camera.main;
        canvas.planeDistance = 5f;
        canvas.sortingOrder = 500;

        scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        FitToScreen();
        return go.transform;
    }

    GameObject Panel(Transform parent, string name, Color color, bool blocksInput)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        var image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = blocksInput;
        return go;
    }

    Image Box(Transform parent, string name, Color color, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.sprite = rounded;
        image.type = Image.Type.Sliced;
        image.color = color;
        Place(image.rectTransform, pos, size);
        return image;
    }

    Image Icon(Transform parent, Sprite sprite, Vector2 pos, Vector2 size)
    {
        var go = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        Place(image.rectTransform, pos, size);
        return image;
    }

    Text Label(Transform parent, string text, int size, Color color, Vector2 pos, Vector2 box, bool shadow = false)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var label = go.GetComponent<Text>();
        label.font = font;
        label.text = text;
        label.fontSize = size;
        label.color = color;
        label.alignment = TextAnchor.MiddleCenter;
        label.horizontalOverflow = HorizontalWrapMode.Overflow;
        label.verticalOverflow = VerticalWrapMode.Overflow;
        label.raycastTarget = false;
        if (shadow) go.AddComponent<Shadow>().effectDistance = new Vector2(3, -3);
        Place(label.rectTransform, pos, box);
        return label;
    }

    Text CoinPill(Transform parent, Vector2 pos)
    {
        Image pill = Box(parent, "CoinPill", new Color(0, 0, 0, 0.3f), pos, new Vector2(340, 110));
        Icon(pill.transform, coinSprite, new Vector2(-110, 0), new Vector2(120, 120));
        return Label(pill.transform, "0", 60, Color.white, new Vector2(40, 0), new Vector2(200, 100), true);
    }

    Button MakeButton(Transform parent, string name, string text, Color color, Vector2 pos, Vector2 size,
        out Text label, UnityEngine.Events.UnityAction onClick)
    {
        Image image = Box(parent, name, color, pos, size);
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);
        label = Label(image.transform, text, Mathf.RoundToInt(size.y * 0.42f), Color.white, Vector2.zero, size, true);
        return button;
    }

    static void Place(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static void Anchor(RectTransform rt, Vector2 anchor)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = anchor;
    }

    static Sprite CreateRoundedSprite(int size, int radius)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float cx = Mathf.Clamp(x + 0.5f, radius, size - radius);
                float cy = Mathf.Clamp(y + 0.5f, radius, size - radius);
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(cx, cy));
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(radius - dist + 0.5f)));
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100, 0, SpriteMeshType.FullRect,
            new Vector4(radius, radius, radius, radius));
    }
}
