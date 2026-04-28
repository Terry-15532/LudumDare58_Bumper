using System;
using System.Collections.Generic;

// Minimal profile record for the NFC arcade flow. One entry per card UID.
// Serialized via JsonUtility → a flat list wrapped in ArcadeProfileDb.
[Serializable]
public class ArcadeProfile {
    public string uid;
    public string name;
    public long   createdAtUnix;
    public int    matchesPlayed;
    public int    wins;
    public int    losses;
    public int    draws;
    public int    points;
    public string emoji;
}

[Serializable]
public class ArcadeProfileDb {
    public List<ArcadeProfile> profiles = new();
}
