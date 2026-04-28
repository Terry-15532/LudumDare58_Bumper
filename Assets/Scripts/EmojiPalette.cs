// NotoEmoji sprite tags. Asset:
// Assets/TextMesh Pro/Resources/Sprite Assets/NotoEmoji.asset (Apache 2.0).
// Curated 32-face palette — wide emotional range, all single-codepoint so
// they resolve cleanly via the sprite-asset name lookup.
public static class EmojiPalette {
    const string A = "<sprite=\"NotoEmoji\" name=\"";
    const string Z = "\">";

    public static readonly string[] All = {
        A+"1f600"+Z, // grinning
        A+"1f601"+Z, // beaming
        A+"1f602"+Z, // tears of joy
        A+"1f603"+Z, // open-mouth
        A+"1f605"+Z, // sweat smile
        A+"1f606"+Z, // squint laugh
        A+"1f609"+Z, // wink
        A+"1f60a"+Z, // blush
        A+"1f60b"+Z, // tongue
        A+"1f60d"+Z, // heart eyes
        A+"1f60e"+Z, // sunglasses
        A+"1f60f"+Z, // smirk
        A+"1f610"+Z, // neutral
        A+"1f612"+Z, // unamused
        A+"1f614"+Z, // pensive
        A+"1f618"+Z, // kiss
        A+"1f61c"+Z, // tongue + wink
        A+"1f61d"+Z, // squint tongue
        A+"1f61e"+Z, // disappointed
        A+"1f620"+Z, // angry
        A+"1f621"+Z, // pouting
        A+"1f622"+Z, // crying
        A+"1f624"+Z, // huffing
        A+"1f62a"+Z, // sleepy
        A+"1f62c"+Z, // grimace
        A+"1f62d"+Z, // sobbing
        A+"1f631"+Z, // screaming
        A+"1f633"+Z, // flushed
        A+"1f634"+Z, // sleeping
        A+"1f635"+Z, // dizzy
        A+"1f923"+Z, // rofl
        A+"1f970"+Z, // smiling-with-hearts
    };
    public static string Random() => All[UnityEngine.Random.Range(0, All.Length)];
}
