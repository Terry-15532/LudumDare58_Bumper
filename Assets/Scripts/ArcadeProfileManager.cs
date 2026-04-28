using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// Loads / saves arcade player profiles keyed by NFC card UID.
// Path: Application.persistentDataPath/arcade_profiles.json
//
// When the arcade cabinet is unified with other games (e.g. Jesse's dodgeball),
// change FilePath to a shared location both exes agree on. That's the only
// coupling point.
public enum MatchOutcome { Win, Loss, Draw }

public static class ArcadeProfileManager {
    const string FileName = "arcade_profiles.json";

    static readonly Dictionary<string, ArcadeProfile> byUid = new();
    static bool loaded;

    public static string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    public static void EnsureLoaded() {
        if (loaded) return;
        loaded = true;

        if (!File.Exists(FilePath)) return;
        try {
            string json = File.ReadAllText(FilePath);
            var db = JsonUtility.FromJson<ArcadeProfileDb>(json);
            if (db?.profiles == null) return;
            foreach (var p in db.profiles) {
                if (!string.IsNullOrEmpty(p.uid)) byUid[p.uid] = p;
            }
        }
        catch (Exception e) {
            Debug.LogWarning($"[ArcadeProfileManager] Failed to load: {e.Message}");
        }
    }

    public static ArcadeProfile Get(string uid) {
        EnsureLoaded();
        return byUid.TryGetValue(uid, out var p) ? p : null;
    }

    public static bool Has(string uid) {
        EnsureLoaded();
        return byUid.ContainsKey(uid);
    }

    public static ArcadeProfile Register(string uid, string name) {
        EnsureLoaded();
        var p = new ArcadeProfile {
            uid           = uid,
            name          = name,
            createdAtUnix = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            matchesPlayed = 0,
            wins          = 0,
            losses        = 0,
            draws         = 0,
            points        = 0,
            emoji         = EmojiPalette.Random(),
        };
        byUid[uid] = p;
        Save();
        return p;
    }

    public static void Rename(string uid, string name) {
        EnsureLoaded();
        if (byUid.TryGetValue(uid, out var p)) {
            p.name = name;
            Save();
        }
    }

    public static void SetEmoji(string uid, string emoji) {
        EnsureLoaded();
        if (byUid.TryGetValue(uid, out var p)) {
            p.emoji = emoji;
            Save();
        }
    }

    public static void RecordMatchResult(string uid, MatchOutcome outcome) {
        EnsureLoaded();
        if (!byUid.TryGetValue(uid, out var p)) return;
        p.matchesPlayed++;
        switch (outcome) {
            case MatchOutcome.Win:  p.wins++;   p.points += 3; break;
            case MatchOutcome.Loss: p.losses++; p.points -= 1; break;
            case MatchOutcome.Draw: p.draws++;  p.points += 1; break;
        }
        if (p.points < 0) p.points = 0;
        Save();
    }

    public static void Save() {
        var db = new ArcadeProfileDb { profiles = new List<ArcadeProfile>(byUid.Values) };
        try {
            File.WriteAllText(FilePath, JsonUtility.ToJson(db, prettyPrint: true));
        }
        catch (Exception e) {
            Debug.LogWarning($"[ArcadeProfileManager] Failed to save: {e.Message}");
        }
    }
}
