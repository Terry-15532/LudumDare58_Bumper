using TMPro;
using UnityEngine;

// World-space TMP that pops above a player. Resolves EmojiOne sprite tags by
// loading the sprite asset from Resources at first use.
public class EmojiReaction : MonoBehaviour {
    public static TMP_FontAsset font;
    public static TMP_SpriteAsset spriteAsset;

    Transform anchor;
    float t0, lifetime = 1.0f;
    Vector3 baseOffset = Vector3.up * 1.5f;
    TextMeshPro tmp;

    public static void Spawn(Transform anchor, string symbol, Color tint = default) {
        if (font == null) font = TMP_Settings.defaultFontAsset;
        if (font == null) { Debug.LogWarning("[EmojiReaction] No TMP font asset; skipping."); return; }
        if (spriteAsset == null) spriteAsset = Resources.Load<TMP_SpriteAsset>("Sprite Assets/NotoEmoji");
        if (anchor == null || string.IsNullOrEmpty(symbol)) return;

        var go = new GameObject("EmojiReaction");
        go.transform.position = anchor.position + Vector3.up * 1.5f;
        var r = go.AddComponent<EmojiReaction>();
        r.anchor = anchor;
        r.t0 = Time.time;

        var tmp = go.AddComponent<TextMeshPro>();
        tmp.font = font;
        if (spriteAsset != null) tmp.spriteAsset = spriteAsset;
        tmp.text = symbol;
        tmp.fontSize = 6;
        tmp.alignment = TextAlignmentOptions.Center;
        // Sprite emoji are pre-colored; default to white so the yellow shows through.
        tmp.color = tint == default ? Color.white : tint;
        tmp.fontStyle = FontStyles.Bold;
        tmp.GetComponent<MeshRenderer>().sortingOrder = 1000;
        var rt = go.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(4, 2);
        r.tmp = tmp;
    }

    void Update() {
        float t = (Time.time - t0) / lifetime;
        if (t >= 1f) { Destroy(gameObject); return; }

        Vector3 anchorPos = anchor != null ? anchor.position : transform.position;
        transform.position = anchorPos + baseOffset + Vector3.up * (t * 1.0f);

        float scale = t < 0.2f ? Mathf.Lerp(0.3f, 1.3f, t / 0.2f)
                    : t < 0.4f ? Mathf.Lerp(1.3f, 1.0f, (t - 0.2f) / 0.2f)
                    : t < 0.8f ? 1.0f
                    : Mathf.Lerp(1.0f, 0.6f, (t - 0.8f) / 0.2f);
        transform.localScale = Vector3.one * scale;

        var cam = Camera.main;
        if (cam != null) transform.rotation = Quaternion.LookRotation(transform.position - cam.transform.position);

        if (tmp != null) {
            var c = tmp.color;
            c.a = t < 0.7f ? 1f : Mathf.Lerp(1f, 0f, (t - 0.7f) / 0.3f);
            tmp.color = c;
        }
    }
}
