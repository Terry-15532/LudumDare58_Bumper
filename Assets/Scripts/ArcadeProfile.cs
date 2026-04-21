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
}

[Serializable]
public class ArcadeProfileDb {
    public List<ArcadeProfile> profiles = new();
}
