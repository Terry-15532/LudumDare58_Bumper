using UnityEngine;

// Rolls an arcade-friendly English player name.
// Pattern: <ADJECTIVE> <NOUN> [NUMBER?]  — all caps, single space, <= 18 chars.
// Kept short so it reads cleanly on the scoreboard.
public static class NameGenerator {
    static readonly string[] Adjectives = {
        "TURBO", "COSMIC", "ANGRY", "SPICY", "GROOVY", "LASER", "NEON",
        "SALTY", "CYBER", "FUZZY", "ROGUE", "MIGHTY", "SNEAKY", "VIBING",
        "CHONKY", "JOLLY", "CRISPY", "FANCY", "RETRO", "PIXEL", "FERAL",
        "ZESTY", "SMUG", "DIZZY", "CLUTCH", "HYPE", "GOBLIN", "MYSTIC",
        "SLEEPY", "UNHINGED", "WASABI", "GALAXY", "WOBBLY", "PRIME", "SONIC",
        "PLASMA", "CRYPTIC", "RADICAL", "COZY", "DANK", "EPIC", "HONKING",
        "LOFI", "MOIST", "SILKY", "THICC", "WACKY", "SWAGGY", "ZOMBIE",
        "WILD",
    };

    static readonly string[] Nouns = {
        "PANDA", "HAMSTER", "TOASTER", "WIZARD", "NOODLE", "GOBLIN", "KRAKEN",
        "GRIFFIN", "RACCOON", "POTATO", "CACTUS", "PENGUIN", "YETI", "BAGEL",
        "DUMPLING", "ROBOT", "KITTEN", "PUPPY", "MANTIS", "NARWHAL",
        "OCTOPUS", "PIGEON", "BANANA", "BISCUIT", "DRAGON", "FALCON", "PICKLE",
        "MUFFIN", "PANGOLIN", "PLATYPUS", "SQUID", "WAFFLE", "ZEPPELIN",
        "OVERLORD", "BEAST", "LEGEND", "WARLOCK", "NINJA", "PIRATE", "KNIGHT",
        "HAMBURGER", "HIPPO", "LOBSTER", "MOOSE", "OTTER", "SHARK", "SLOTH",
        "TIGER", "TROUT", "WOMBAT",
    };

    // ~35% chance of appending a 1-3 digit number, which gives a "gamer tag" vibe.
    const float NumberChance = 0.35f;

    public static string Roll() {
        string adj  = Adjectives[Random.Range(0, Adjectives.Length)];
        string noun = Nouns[Random.Range(0, Nouns.Length)];
        string baseName = $"{adj} {noun}";

        if (Random.value < NumberChance) {
            int n = Random.Range(2, 1000);
            string withNumber = $"{baseName} {n}";
            if (withNumber.Length <= 18) return withNumber;
        }
        return baseName;
    }
}
