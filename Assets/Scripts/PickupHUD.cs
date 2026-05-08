using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Programmatic pickup HUD. Auto-creates itself after scene load — no scene setup required.
// Shows active buffs for both players on their respective screen side (left=Blue, right=Red).
// All visuals (halo, scanlines, segmented bar, corner brackets) built at runtime.
public class PickupHUD : MonoBehaviour {

    static PickupHUD instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void AutoCreate(){
        if (instance != null) return;
        var go = new GameObject("~PickupHUD");
        go.AddComponent<PickupHUD>();
    }

    // --- Palette ---
    static readonly Color BlueSide = new Color(0.35f, 0.85f, 1.9f);
    static readonly Color RedSide = new Color(1.9f, 0.30f, 0.45f);
    static readonly Color PanelBG = new Color(0.018f, 0.022f, 0.05f, 0.90f);
    static readonly Color PanelBGInner = new Color(0.035f, 0.045f, 0.09f, 0.92f);
    static readonly Color SegmentOff = new Color(0.08f, 0.10f, 0.16f, 0.85f);
    static readonly Color DescColor = new Color(0.72f, 0.78f, 0.88f, 1f);
    static readonly Color TimeColor = new Color(1f, 1f, 1f, 0.95f);

    // --- Sizing ---
    const float PanelW = 420f;
    const float PanelH = 120f;
    const int BarSegments = 14;

    enum BuffKind { SpeedBoost, DoubleScore, Invincibility }

    struct BuffDef {
        public string title, desc, icon;
        public Color accent;
    }

    // Icon glyphs chosen to exist in TMP's default Liberation Sans (avoid missing-glyph rects).
    static readonly Dictionary<BuffKind, BuffDef> defs = new Dictionary<BuffKind, BuffDef> {
        {
            BuffKind.SpeedBoost, new BuffDef {
                title = "SPEED BOOST", desc = "1.5× acceleration and top speed", icon = "▶", accent = new Color(0.25f, 1.95f, 0.6f)
            }
        }, {
            BuffKind.DoubleScore, new BuffDef {
                title = "DOUBLE SCORE", desc = "Every coin is worth 2 points", icon = "★", accent = new Color(2.1f, 1.35f, 0.15f)
            }
        }, {
            BuffKind.Invincibility, new BuffDef {
                title = "INVINCIBILITY", desc = "Immune to all external forces", icon = "◆", accent = new Color(1.8f, 1.8f, 2.0f)
            }
        },
    };

    class Entry {
        public GameObject root;
        public CanvasGroup group;
        public RectTransform rt;
        public Image[] segments;
        public Image sideStripOuter;
        public TextMeshProUGUI timeText;
        public Color accent;
        public bool leftSide;
        public bool dying;
    }

    Canvas canvas;
    RectTransform leftCol, rightCol;
    readonly Dictionary<BuffKind, Entry> blueEntries = new Dictionary<BuffKind, Entry>();
    readonly Dictionary<BuffKind, Entry> redEntries = new Dictionary<BuffKind, Entry>();

    static Sprite _scanlineSprite;

    void Awake(){
        if (instance != null && instance != this) {
            Destroy(gameObject);
            return;
        }
        instance = this;
        BuildCanvas();
        
        DontDestroyOnLoad(this.gameObject);
    }

    void OnDestroy(){
        if (instance == this) instance = null;
    }

    // === Top-level structure ===

    void BuildCanvas(){
        var canvasGO = new GameObject("Canvas");
        canvasGO.transform.SetParent(transform, false);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        leftCol = BuildColumn("LeftColumn", true);
        rightCol = BuildColumn("RightColumn", false);
    }

    RectTransform BuildColumn(string colName, bool leftSide){
        var go = new GameObject(colName, typeof(RectTransform));
        go.transform.SetParent(canvas.transform, false);
        var rt = (RectTransform)go.transform;
        // Anchor both min/max y to the same value so the column's top is fixed at screen space 0.9
        rt.anchorMin = new Vector2(leftSide ? 0 : 1, 0.9f);
        rt.anchorMax = new Vector2(leftSide ? 0 : 1, 0.9f);
        // Use pivot.y = 1 so the column expands downward when children are added; this keeps the first
        // entry's screen position fixed (new entries are appended below).
        rt.pivot = new Vector2(leftSide ? 0 : 1, 1f);
        rt.anchoredPosition = new Vector2(leftSide ? 36 : -36, 0);
        rt.sizeDelta = new Vector2(PanelW + 40, 0);
        var vlg = go.AddComponent<VerticalLayoutGroup>();
        // Increase spacing between stacked entries so multiple effects are visually separated.
        vlg.spacing = 24;
        vlg.childControlWidth = true;
        vlg.childControlHeight = false;
        vlg.childForceExpandWidth = false;
        vlg.childForceExpandHeight = false;
        // Align children to the top so the first item stays pinned at the top of the column.
        vlg.childAlignment = leftSide ? TextAnchor.UpperLeft : TextAnchor.UpperRight;
        var fitter = go.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return rt;
    }

    void Update(){
        if (Game.instance == null) return;
        var blue = Game.instance.playerBlue;
        var red = Game.instance.playerRed;
        if (blue) HandlePlayer(blue, leftCol, blueEntries, true);
        if (red) HandlePlayer(red, rightCol, redEntries, false);
        AnimateAll(blueEntries);
        AnimateAll(redEntries);
    }

    void HandlePlayer(PlayerController p, RectTransform parent, Dictionary<BuffKind, Entry> entries, bool leftSide){
        Sync(p, parent, entries, leftSide, BuffKind.SpeedBoost, p.speedBoostUntil, p.speedBoostDuration);
        Sync(p, parent, entries, leftSide, BuffKind.DoubleScore, p.doubleScoreUntil, p.doubleScoreDuration);
        Sync(p, parent, entries, leftSide, BuffKind.Invincibility, p.invincibleUntil, p.invincibleDuration);
    }

    void Sync(PlayerController p, RectTransform parent, Dictionary<BuffKind, Entry> entries, bool leftSide, BuffKind kind, float until, float duration){
        float remaining = until - Time.time;
        bool active = remaining > 0f && duration > 0f;
        bool has = entries.TryGetValue(kind, out var entry);

        if (active) {
            if (!has || entry.root == null) {
                entry = BuildEntry(parent, leftSide, kind, p.side);
                entries[kind] = entry;
            }
            float fraction = Mathf.Clamp01(remaining / duration);
            UpdateSegments(entry, fraction, remaining);
            entry.timeText.text = $"{remaining:0.0}s";
        }
        else if (has && !entry.dying) {
            StartCoroutine(ExitAnim(entry));
            entries.Remove(kind);
        }
    }

    void AnimateAll(Dictionary<BuffKind, Entry> entries){
        foreach (var kv in entries) {
            var e = kv.Value;
            if (e.root == null || e.dying) continue;
            // Gentle sine pulse on the side strip for a "living" HUD feel.
            float pulse = 0.85f + Mathf.Sin(Time.unscaledTime * 3.5f) * 0.15f;
            if (e.sideStripOuter != null) {
                Color c = e.sideStripOuter.color;
                c.a = pulse;
                e.sideStripOuter.color = c;
            }
        }
    }

    // === Per-entry visual update ===

    void UpdateSegments(Entry entry, float fraction, float remaining){
        float exact = fraction * BarSegments;
        int full = Mathf.FloorToInt(exact);
        float partial = exact - full;

        // Urgency: accelerate flash on the leading segment when <1.5s remain.
        bool urgent = remaining < 1.5f;
        float urgentBlink = urgent ? (0.55f + 0.45f * Mathf.Sin(Time.unscaledTime * 18f)) : 1f;

        for (int i = 0; i < entry.segments.Length; i++) {
            var seg = entry.segments[i];
            if (i < full) {
                seg.color = entry.accent * urgentBlink;
            }
            else if (i == full && partial > 0f) {
                Color c = entry.accent * urgentBlink;
                c.a = partial;
                seg.color = c;
            }
            else {
                seg.color = SegmentOff;
            }
        }
    }

    // === Exit animation ===

    IEnumerator ExitAnim(Entry entry){
        entry.dying = true;
        if (entry.root == null) yield break;
        float elapsed = 0f;
        float duration = 0.22f;
        Vector2 startPos = entry.rt.anchoredPosition;
        Vector2 endPos = startPos + new Vector2(entry.leftSide ? -40f : 40f, 0);
        Vector3 startScale = Vector3.one;
        Vector3 endScale = new Vector3(0.9f, 0.7f, 1f);
        while (elapsed < duration && entry.root != null) {
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(elapsed / duration);
            k = k * k;
            entry.rt.anchoredPosition = Vector2.LerpUnclamped(startPos, endPos, k);
            entry.rt.localScale = Vector3.LerpUnclamped(startScale, endScale, k);
            if (entry.group != null) entry.group.alpha = 1f - k;
            yield return null;
        }
        if (entry.root != null) Destroy(entry.root);
    }

    // === Entry construction ===

    Entry BuildEntry(RectTransform parent, bool leftSide, BuffKind kind, PlayerSide side){
        var def = defs[kind];
        var sideColor = side == PlayerSide.Blue ? BlueSide : RedSide;

        // Root
        var root = new GameObject($"Entry_{kind}", typeof(RectTransform));
        root.transform.SetParent(parent, false);
        var rootRT = (RectTransform)root.transform;
        var le = root.AddComponent<LayoutElement>();
        le.preferredHeight = PanelH;
        le.preferredWidth = PanelW;
        rootRT.sizeDelta = new Vector2(PanelW, PanelH);
        var group = root.AddComponent<CanvasGroup>();
        group.alpha = 0f;

        // --- Halo: 3 stacked boxes behind ---
        BuildHaloLayer(root.transform, def.accent, 0.10f, 44f);
        BuildHaloLayer(root.transform, def.accent, 0.18f, 24f);
        BuildHaloLayer(root.transform, sideColor, 0.22f, 10f);

        // --- Panel background (two-tone inner for subtle depth) ---
        AddImage(root.transform, "BG", PanelBG, full: true);
        var bgIn = AddImage(root.transform, "BGInner", PanelBGInner);
        Inset(bgIn.rectTransform, 3f);

        // --- Scanlines (tiled texture) ---
        var scan = AddImage(root.transform, "Scanlines", new Color(1f, 1f, 1f, 1f));
        Inset(scan.rectTransform, 3f);
        scan.sprite = GetScanlineSprite();
        scan.type = Image.Type.Tiled;
        scan.color = new Color(1f, 1f, 1f, 0.5f);
        scan.raycastTarget = false;

        // --- Top / bottom edge lines (bright accent) ---
        BuildEdgeLine(root.transform, sideColor, top: true);
        BuildEdgeLine(root.transform, sideColor, top: false);

        // --- Side strip: outer (bright, pulsing) + inner (darker static) ---
        var sideOuter = BuildSideStrip(root.transform, leftSide, sideColor, 10f);
        BuildSideStrip(root.transform, leftSide, new Color(sideColor.r * 0.5f, sideColor.g * 0.5f, sideColor.b * 0.5f, 1f), 4f);

        // --- Corner brackets (sci-fi L-shapes at 4 corners) ---
        BuildCornerBracket(root.transform, def.accent, top: true, leftCorner: true);
        BuildCornerBracket(root.transform, def.accent, top: true, leftCorner: false);
        BuildCornerBracket(root.transform, def.accent, top: false, leftCorner: true);
        BuildCornerBracket(root.transform, def.accent, top: false, leftCorner: false);

        // --- Icon ---
        float iconX = leftSide ? 24f : (PanelW - 24f - 56f);
        var icon = BuildText(root.transform, "Icon", def.icon, 54f, def.accent, FontStyles.Bold);
        icon.rectTransform.anchorMin = new Vector2(0, 0.5f);
        icon.rectTransform.anchorMax = new Vector2(0, 0.5f);
        icon.rectTransform.pivot = new Vector2(0, 0.5f);
        icon.rectTransform.anchoredPosition = new Vector2(iconX, 0);
        icon.rectTransform.sizeDelta = new Vector2(56, 56);
        icon.alignment = TextAlignmentOptions.Center;

        // --- Text block (title + description) ---
        float textLeft = leftSide ? 90f : 20f;
        float textRight = leftSide ? 20f : 90f;
        var title = BuildText(root.transform, "Title", def.title, 30f, def.accent, FontStyles.Bold | FontStyles.UpperCase);
        title.characterSpacing = 4f;
        title.alignment = leftSide ? TextAlignmentOptions.BottomLeft : TextAlignmentOptions.BottomRight;
        AnchorBand(title.rectTransform, textLeft, textRight, topFromTop: 14, height: 38);

        var desc = BuildText(root.transform, "Desc", def.desc, 16f, DescColor, FontStyles.Normal);
        desc.alignment = leftSide ? TextAlignmentOptions.Left : TextAlignmentOptions.Right;
        AnchorBand(desc.rectTransform, textLeft, textRight, topFromTop: 50, height: 22);

        // --- Bar area (segments + time) ---
        const float timeW = 54f;
        float barLeft = textLeft;
        float barRight = textRight + timeW + 10f;
        if (!leftSide) {
            barLeft = textLeft + timeW + 10f;
            barRight = textRight;
        }

        var segments = BuildSegmentedBar(root.transform, leftSide, barLeft, barRight, bottomY: 14, height: 14);

        // Time text on the inner edge (close to player-side strip). "s" suffix is part of the string.
        var time = BuildText(root.transform, "Time", "0.0s", 18f, TimeColor, FontStyles.Bold);
        time.characterSpacing = 2f;
        time.alignment = leftSide ? TextAlignmentOptions.MidlineRight : TextAlignmentOptions.MidlineLeft;
        time.rectTransform.anchorMin = new Vector2(leftSide ? 1 : 0, 0);
        time.rectTransform.anchorMax = new Vector2(leftSide ? 1 : 0, 0);
        time.rectTransform.pivot = new Vector2(leftSide ? 1 : 0, 0);
        time.rectTransform.anchoredPosition = new Vector2(leftSide ? -textRight : textLeft, 10);
        time.rectTransform.sizeDelta = new Vector2(timeW, 22);

        // Intro anim
        rootRT.localScale = new Vector3(0.94f, 0.88f, 1f);
        StartCoroutine(IntroAnim(rootRT, group));

        return new Entry {
            root = root,
            group = group,
            rt = rootRT,
            segments = segments,
            sideStripOuter = sideOuter,
            timeText = time,
            accent = def.accent,
            leftSide = leftSide,
        };
    }

    // === Visual primitives ===

    void BuildHaloLayer(Transform parent, Color color, float alpha, float expand){
        var img = AddImage(parent, "Halo", new Color(color.r, color.g, color.b, alpha));
        var rt = img.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(-expand, -expand);
        rt.offsetMax = new Vector2(expand, expand);
        img.raycastTarget = false;
    }

    void BuildEdgeLine(Transform parent, Color color, bool top){
        var img = AddImage(parent, top ? "EdgeTop" : "EdgeBottom", color);
        var rt = img.rectTransform;
        rt.anchorMin = new Vector2(0, top ? 1 : 0);
        rt.anchorMax = new Vector2(1, top ? 1 : 0);
        rt.pivot = new Vector2(0.5f, top ? 1 : 0);
        rt.sizeDelta = new Vector2(0, 2);
        rt.anchoredPosition = Vector2.zero;
    }

    Image BuildSideStrip(Transform parent, bool leftSide, Color color, float width){
        var img = AddImage(parent, "SideStrip", color);
        var rt = img.rectTransform;
        rt.anchorMin = new Vector2(leftSide ? 0 : 1, 0);
        rt.anchorMax = new Vector2(leftSide ? 0 : 1, 1);
        rt.pivot = new Vector2(leftSide ? 0 : 1, 0.5f);
        rt.sizeDelta = new Vector2(width, 0);
        rt.anchoredPosition = Vector2.zero;
        return img;
    }

    void BuildCornerBracket(Transform parent, Color color, bool top, bool leftCorner){
        const float len = 18f, thick = 2f;
        // Horizontal leg
        var h = AddImage(parent, "CornerH", color);
        var hr = h.rectTransform;
        hr.anchorMin = new Vector2(leftCorner ? 0 : 1, top ? 1 : 0);
        hr.anchorMax = new Vector2(leftCorner ? 0 : 1, top ? 1 : 0);
        hr.pivot = new Vector2(leftCorner ? 0 : 1, top ? 1 : 0);
        hr.sizeDelta = new Vector2(len, thick);
        hr.anchoredPosition = new Vector2(leftCorner ? 4 : -4, top ? -4 : 4);
        // Vertical leg
        var v = AddImage(parent, "CornerV", color);
        var vr = v.rectTransform;
        vr.anchorMin = new Vector2(leftCorner ? 0 : 1, top ? 1 : 0);
        vr.anchorMax = new Vector2(leftCorner ? 0 : 1, top ? 1 : 0);
        vr.pivot = new Vector2(leftCorner ? 0 : 1, top ? 1 : 0);
        vr.sizeDelta = new Vector2(thick, len);
        vr.anchoredPosition = new Vector2(leftCorner ? 4 : -4, top ? -4 : 4);
    }

    Image[] BuildSegmentedBar(Transform parent, bool leftSide, float leftPad, float rightPad, float bottomY, float height){
        var container = new GameObject("BarContainer", typeof(RectTransform));
        container.transform.SetParent(parent, false);
        var crt = (RectTransform)container.transform;
        crt.anchorMin = new Vector2(0, 0);
        crt.anchorMax = new Vector2(1, 0);
        crt.pivot = new Vector2(0.5f, 0);
        crt.offsetMin = new Vector2(leftPad, bottomY);
        crt.offsetMax = new Vector2(-rightPad, bottomY + height);

        float totalW = PanelW - leftPad - rightPad;
        float gap = 2f;
        float segW = (totalW - gap * (BarSegments - 1)) / BarSegments;

        var segs = new Image[BarSegments];
        for (int i = 0; i < BarSegments; i++) {
            int idx = leftSide ? i : (BarSegments - 1 - i);// fill/drain direction
            var img = AddImage(container.transform, $"Seg{i}", SegmentOff);
            var rt = img.rectTransform;
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 0.5f);
            rt.anchoredPosition = new Vector2(idx * (segW + gap), 0);
            rt.sizeDelta = new Vector2(segW, 0);
            segs[idx] = img;
        }
        return segs;
    }

    // === Intro anim ===

    IEnumerator IntroAnim(RectTransform rt, CanvasGroup cg){
        float elapsed = 0f, dur = 0.22f;
        while (elapsed < dur && rt != null) {
            elapsed += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(elapsed / dur);
            k = 1f - (1f - k) * (1f - k);
            rt.localScale = Vector3.Lerp(new Vector3(0.94f, 0.88f, 1f), Vector3.one, k);
            if (cg != null) cg.alpha = k;
            yield return null;
        }
        if (rt != null) rt.localScale = Vector3.one;
        if (cg != null) cg.alpha = 1f;
    }

    // === Low-level helpers ===

    static Image AddImage(Transform parent, string name, Color color, bool full = false){
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        if (full) {
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
        else {
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
        return img;
    }

    static void Inset(RectTransform rt, float amount){
        rt.offsetMin = new Vector2(amount, amount);
        rt.offsetMax = new Vector2(-amount, -amount);
    }

    static TextMeshProUGUI BuildText(Transform parent, string name, string text, float size, Color color, FontStyles style){
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.color = color;
        tmp.fontStyle = style;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        tmp.raycastTarget = false;
        return tmp;
    }

    // Position a band stretching horizontally inside parent, anchored to top edge with given padding.
    static void AnchorBand(RectTransform rt, float leftPad, float rightPad, float topFromTop, float height){
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1);
        rt.offsetMin = new Vector2(leftPad, -(topFromTop + height));
        rt.offsetMax = new Vector2(-rightPad, -topFromTop);
    }

    // Runtime-generated 1×4 tiled scanline sprite: one darker row, three empty rows.
    static Sprite GetScanlineSprite(){
        if (_scanlineSprite != null) return _scanlineSprite;
        var tex = new Texture2D(1, 4, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Repeat;
        var dark = new Color(0f, 0f, 0f, 0.35f);
        var clear = new Color(0f, 0f, 0f, 0f);
        tex.SetPixel(0, 0, dark);
        tex.SetPixel(0, 1, clear);
        tex.SetPixel(0, 2, clear);
        tex.SetPixel(0, 3, clear);
        tex.Apply();
        _scanlineSprite = Sprite.Create(tex, new Rect(0, 0, 1, 4), new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect);
        _scanlineSprite.name = "ScanlineSprite";
        return _scanlineSprite;
    }
}
