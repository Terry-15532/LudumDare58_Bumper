using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Piper;
using Random = UnityEngine.Random;

public enum CommentaryEvent {
    GameStart,
    BlueKill,
    RedKill,
    BlueDash,
    RedDash,
    BlueDoubleScore,
    RedDoubleScore,
    BlueInvincible,
    RedInvincible,
    Victory,
    HalfTime,
    FifteenSeconds
}

public class Commentator : MonoBehaviour {
    public static Commentator instance;

    [Header("Settings")]
    public float commentaryCooldown = 8f;
    public float eventBufferTime = 0.6f;

    private float lastCommentaryTime;
    private float currentBufferTime;

    private Queue<string> voiceLineQueue = new Queue<string>();
    private List<CommentaryEvent> eventBuffer = new List<CommentaryEvent>();
    private bool isSpeaking;
    private bool isGenerating;
    private int generationId;

    private int maxLeadBlue;
    private int maxLeadRed;

    private bool hasAnnouncedGameStart;
    private bool hasAnnouncedHalfTime;
    private bool hasAnnouncedFifteenSeconds;

    private TMPro.TextMeshProUGUI subtitleText;
    private CanvasGroup subtitleCanvasGroup;
    private Coroutine subtitleCoroutine;
    private Coroutine playVoiceLineCoroutine;

    private Dictionary<CommentaryEvent, List<string>> fallbackCommentary = new Dictionary<CommentaryEvent, List<string>> {
        {
            CommentaryEvent.RedKill, new List<string> {
                "Blue player is knocked off the stage!",
                "Oh no! Blue is eliminated!",
                "Blue takes a tumble off the edge!",
                "Another one bites the dust! Blue is out!"
            }
        }, {
            CommentaryEvent.BlueKill, new List<string> {
                "Red player is knocked off the stage!",
                "Oh no! Red is eliminated!",
                "Red takes a tumble off the edge!",
                "Another one bites the dust! Red is out!"
            }
        }, {
            CommentaryEvent.BlueDoubleScore, new List<string> {
                "Blue gets a double score buff! Watch out red!",
                "Blue is on fire with double points!",
                "Double score for Blue! Extra points incoming!",
                "Blue powered up! Double score activated!"
            }
        }, {
            CommentaryEvent.RedDoubleScore, new List<string> {
                "Red gets a double score buff! Watch out blue!",
                "Red is on fire with double points!",
                "Double score for Red! Extra points incoming!",
                "Red powered up! Double score activated!"
            }
        }, {
            CommentaryEvent.BlueInvincible, new List<string> {
                "Blue got an invincible buff now!",
                "Blue has become invincible!",
                "Invincibility activated for Blue!",
                "Blue is in God mode!"
            }
        }, {
            CommentaryEvent.RedInvincible, new List<string> {
                "Red got an invincible buff now!",
                "Red has become invincible!",
                "Invincibility activated for Red!",
                "Red is in God mode!"
            }
        }, {
            CommentaryEvent.GameStart, new List<string> {
                "Ladies and gentlemen, welcome to the arena! The crowd is roaring! The players are ready! And so the match begins!",
                "It all comes down to this! Both competitors are locked in! They're ready to clash! It's time for the ultimate showdown!",
                "Welcome to BUMPER MASTER! The energy here is absolutely high! Strap in, cuz this is gonna be wild!",
                "Here we go! The stage is set! The stakes are high! And the battle starts now!"
            }
        }, {
            CommentaryEvent.HalfTime, new List<string> {
                "We've reached the halfway point!",
                "Half time! The pace is intense!",
                "Halfway there! Let's check the standings.",
                "That's half time! What a match so far!"
            }
        }, {
            CommentaryEvent.FifteenSeconds, new List<string> {
                "Only 15 seconds left! The time is running out!",
                "Final moments! Everything is on the line!",
                "Fifteen seconds to go! Make it count!",
                "The clock is ticking down! Final countdown!"
            }
        }, {
            CommentaryEvent.Victory, new List<string> {
                "Time's up! What an absolutely phenomenal display of skill!",
                "Time is up! What a breathtaking conclusion to an unforgettable match!",
                "The final whistle blows! A historic finish!",
                "Match Ends! Truly a masterful performance from start to finish!",
                "Finish! The dust settles and the players stand tall!"
            }
        }
    };

    private enum PlayerIntroTier {
        VeteranHigh,
        VeteranLow,
        RookieHigh,
        RookieLow
    }

    private void Awake(){
        if (instance == null) {
            instance = this;
            CreateSubtitleUI();

            if (piper != null) {
                _ = piper.TextToSpeechAsync("");
            }
        }
        else {
            Destroy(gameObject);
        }
    }

    private void OnDestroy(){
        if (instance == this) {
            instance = null;
        }
    }

    private void CreateSubtitleUI(){
        GameObject canvasGO = new GameObject("SubtitleCanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        UnityEngine.UI.CanvasScaler scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        GameObject bgGO = new GameObject("SubtitleBg");
        bgGO.transform.SetParent(canvasGO.transform, false);

        subtitleCanvasGroup = bgGO.AddComponent<CanvasGroup>();
        subtitleCanvasGroup.alpha = 0f;

        UnityEngine.UI.Image bgImage = bgGO.AddComponent<UnityEngine.UI.Image>();
        bgImage.color = new Color(0, 0, 0, 0.7f);

        RectTransform bgRt = bgGO.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0.5f, 0.08f);
        bgRt.anchorMax = new Vector2(0.5f, 0.08f);
        bgRt.pivot = new Vector2(0.5f, 0f);
        bgRt.sizeDelta = new Vector2(1200, 0);

        UnityEngine.UI.VerticalLayoutGroup layout = bgGO.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.padding = new RectOffset(30, 30, 15, 15);
        layout.childAlignment = TextAnchor.MiddleCenter;

        UnityEngine.UI.ContentSizeFitter fitter = bgGO.AddComponent<UnityEngine.UI.ContentSizeFitter>();
        fitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

        GameObject textGO = new GameObject("SubtitleText");
        textGO.transform.SetParent(bgGO.transform, false);

        subtitleText = textGO.AddComponent<TMPro.TextMeshProUGUI>();
        subtitleText.alignment = TMPro.TextAlignmentOptions.Center;
        subtitleText.fontSize = 35;
        subtitleText.color = Color.white;
        subtitleText.fontStyle = TMPro.FontStyles.Bold;
        subtitleText.textWrappingMode = TMPro.TextWrappingModes.Normal;

        StartCoroutine(AssignFontRoutine());
    }

    private IEnumerator AssignFontRoutine(){
        yield return null;
        if (Game.instance != null && Game.instance.countdownText != null) {
            subtitleText.font = Game.instance.countdownText.font;
            subtitleText.fontSharedMaterial = Game.instance.countdownText.fontSharedMaterial;
        }
    }

    public void TriggerEvent(CommentaryEvent evt, float priority = 1f){
        if (evt == CommentaryEvent.Victory) {
            priority = 5f;
            hasAnnouncedGameStart = false;
            hasAnnouncedHalfTime = false;
            hasAnnouncedFifteenSeconds = false;
        }

        if (IsInFinalSilentWindow() && evt != CommentaryEvent.Victory) {
            return;
        }

        if (IsInFinalSilentWindow() && evt == CommentaryEvent.Victory) {
            eventBuffer.Clear();
            voiceLineQueue.Clear();
            generationId++;
            isGenerating = false;

            if (playVoiceLineCoroutine != null) StopCoroutine(playVoiceLineCoroutine);
            if (audioSource) audioSource.Stop();
            if (subtitleCoroutine != null) StopCoroutine(subtitleCoroutine);
            if (subtitleCanvasGroup) subtitleCanvasGroup.alpha = 0f;
        }

        if (evt == CommentaryEvent.GameStart) {
            if (hasAnnouncedGameStart) return;
            hasAnnouncedGameStart = true;
            hasAnnouncedHalfTime = false;
            hasAnnouncedFifteenSeconds = false;

            eventBuffer.Clear();
            currentBufferTime = 0f;
            isGenerating = false;
            generationId++;
        }

        if (!eventBuffer.Contains(evt)) {
            eventBuffer.Add(evt);
            currentBufferTime = eventBufferTime;
        }

        if (priority >= 3f && evt != CommentaryEvent.GameStart) {
            voiceLineQueue.Clear();
            generationId++;
            isGenerating = false;

            if (priority >= 5f) {
                eventBuffer.Clear();
                eventBuffer.Add(evt);
                currentBufferTime = 0f;
                isSpeaking = false;
                if (playVoiceLineCoroutine != null) StopCoroutine(playVoiceLineCoroutine);
                if (audioSource) audioSource.Stop();
                if (subtitleCoroutine != null) StopCoroutine(subtitleCoroutine);
                if (subtitleCanvasGroup) subtitleCanvasGroup.alpha = 0f;
            }
        }
    }

    public void QueueLine(string text){
        if (string.IsNullOrWhiteSpace(text)) return;
        voiceLineQueue.Enqueue(text);
    }

    public void AnnouncePlayerIntro(string playerName, int wins, int losses, int matchesPlayed, bool isNewProfile){
        string line = BuildPlayerIntroLine(playerName, wins, losses, matchesPlayed, isNewProfile);
        QueueLine(line);
    }

    private bool IsInFinalSilentWindow(){
        return Game.instance != null
            && Game.instance.matchRunning
            && Game.instance.timer <= 10f;
    }

    private void Update(){
        if (IsInFinalSilentWindow()) {
            if (eventBuffer.Count > 0) {
                bool hasVictory = eventBuffer.Contains(CommentaryEvent.Victory);

                eventBuffer.Clear();
                currentBufferTime = 0f;

                if (!hasVictory) {
                    voiceLineQueue.Clear();
                    isGenerating = false;
                    generationId++;
                }
            }
        }
        else {
            if (eventBuffer.Count > 0) {
                currentBufferTime -= Time.deltaTime;
                if (currentBufferTime <= 0f) {
                    ProcessEventBuffer();
                }
            }
        }

        if (Game.instance != null && Game.instance.matchRunning) {
            float t = Game.instance.timer;
            float max = Game.instance.maxTime;

            if (!hasAnnouncedHalfTime && t <= max / 2f) {
                hasAnnouncedHalfTime = true;
                TriggerEvent(CommentaryEvent.HalfTime, 1.5f);
            }
            if (!hasAnnouncedFifteenSeconds && t <= 15f) {
                hasAnnouncedFifteenSeconds = true;
                TriggerEvent(CommentaryEvent.FifteenSeconds, 1.5f);
            }
        }

        if (!isSpeaking && !isGenerating && voiceLineQueue.Count > 0) {
            string nextLine = voiceLineQueue.Dequeue();
            playVoiceLineCoroutine = StartCoroutine(PlayVoiceLine(nextLine));
        }
    }

    private void ProcessEventBuffer(){
        if (IsInFinalSilentWindow()) {
            eventBuffer.Clear();
            return;
        }

        if (isSpeaking || isGenerating) return;

        bool isHighPriority = eventBuffer.Contains(CommentaryEvent.GameStart) ||
            eventBuffer.Contains(CommentaryEvent.Victory) ||
            eventBuffer.Contains(CommentaryEvent.HalfTime) ||
            eventBuffer.Contains(CommentaryEvent.FifteenSeconds);

        if (Time.time - lastCommentaryTime < commentaryCooldown && !isHighPriority) {
            eventBuffer.Clear();
            return;
        }

        List<CommentaryEvent> eventsCopy = new List<CommentaryEvent>(eventBuffer);
        eventBuffer.Clear();

        GenerateLLMLine(eventsCopy);
    }

    private string GetArrayPick(string[] arr){
        if (arr == null || arr.Length == 0) return "";
        return arr[Random.Range(0, arr.Length)];
    }

    private PlayerIntroTier GetPlayerTier(int wins, int losses, int matchesPlayed){
        int total = Mathf.Max(0, wins + losses);

        if (matchesPlayed >= 10) {
            if (total == 0) return PlayerIntroTier.VeteranLow;
            float rate = (float)wins / total;
            return rate >= 0.6f ? PlayerIntroTier.VeteranHigh : PlayerIntroTier.VeteranLow;
        }

        if (total == 0) return PlayerIntroTier.RookieLow;

        float rookieRate = (float)wins / total;
        return rookieRate >= 0.6f ? PlayerIntroTier.RookieHigh : PlayerIntroTier.RookieLow;
    }

    private string BuildPlayerIntroLine(string playerName, int wins, int losses, int matchesPlayed, bool isNewProfile){
        if (string.IsNullOrWhiteSpace(playerName)) playerName = "Unknown Challenger";

        if (playerName == "Anonymous Player") {
            return GetArrayPick(new[] {
                "An anonymous contender steps into the spotlight! No name, just intent!",
                "No history, no identity: this anonymous player lets their actions speak!",
                "An anonymous challenger emerges! No record, no expectations!",
                "Off the radar and into the arena! An anonymous competitor arrives!",
                "Look who we've got! This anonymous player is ready to prove something!",
                "An anonymous entrant joins the match! Everything to play for!",
                "No past to analyze, only performance matters now! An anonymous player is here!",
                "From the shadows to the center stage! An anonymous challenger steps in!",
                "Unrecognized, but not to be underestimated! Here comes an anonymous player!",
                "An anonymous player enters! No stats, no story: just a chance to make an impact!"
            });
        }

        int total = Mathf.Max(0, wins + losses);
        float rate = total > 0 ? (wins * 100f / total) : -1f;
        int rateInt = total > 0 ? Mathf.RoundToInt(rate) : -1;
        PlayerIntroTier tier = GetPlayerTier(wins, losses, matchesPlayed);

        string opener = tier switch {
            PlayerIntroTier.VeteranHigh => GetArrayPick(new[] {
                $"Here comes a seasoned presence! {playerName} steps in!",
                $"A familiar force enters the arena! It's {playerName}!"
            }),
            PlayerIntroTier.VeteranLow => GetArrayPick(new[] {
                $"Not a perfect record, but plenty of fight! {playerName} is here!",
                $"Tested and resilient! Here comes {playerName}!"
            }),
            PlayerIntroTier.RookieHigh => GetArrayPick(new[] {
                $"Momentum is building! {playerName} arrives!",
                $"A rising contender steps forward! It's {playerName}!"
            }),
            _ => GetArrayPick(new[] {
                $"A new challenger approaches! {playerName} enters!",
                $"Making a first impression! Here's {playerName}!"
            })
        };

        string statsLine;
        if (total > 0) {
            statsLine = $"Record: {wins}-{losses}, win rate: {rateInt}%!";
        }
        else if (isNewProfile) {
            statsLine = "No record yet: this is a fresh start!";
        }
        else {
            statsLine = "No official record available!";
        }

        string followUp = GetArrayPick(new[] {
            "This could shift quickly!",
            "Tension is building!",
            "All eyes on this matchup!",
            "The pace is picking up!",
            "This one could be decisive!",
            "Watch this closely!",
            "Strong entrance!",
            "Ready to compete!",
            "Sets the tone early!",
            "A confident start!"
        });

        return $"{opener} {statsLine} {followUp}";
    }

    private string GetWinnerName(PlayerController p, string fallbackSide){
        if (p == null) return fallbackSide;
        bool anonymous = string.IsNullOrEmpty(p.profileUid) || string.IsNullOrWhiteSpace(p.profileName);
        bool ai = p.device == PlayerControlDevice.AI;
        if (anonymous || ai) return fallbackSide;
        return p.profileName;
    }

    private string BuildVictoryLine(int blueScore, int redScore){
        if (Game.instance == null || Game.instance.playerBlue == null || Game.instance.playerRed == null) {
            return $"And that is game! Final score, {blueScore} to {redScore}!";
        }

        if (blueScore > redScore) {
            string winner = GetWinnerName(Game.instance.playerBlue, "Blue");
            return $"Congratulations to {winner} taking the victory! Final score, {blueScore} - {redScore}!";
        }

        if (redScore > blueScore) {
            string winner = GetWinnerName(Game.instance.playerRed, "Red");
            return $"Congratulations to {winner} taking the victory! Final score, {redScore} - {blueScore}!";
        }

        return $"It ends in a draw! Final score, {blueScore} - {redScore}! Neither side gives an inch out!";
    }

    private string GenerateFallbackCommentary(List<CommentaryEvent> events, int blueScore, int redScore, float timeLeft, float maxTimeLeft){
        if (events.Count == 0) return "";

        CommentaryEvent mainEvent = events[0];

        // if (mainEvent == CommentaryEvent.Victory) {
        // }

        if (fallbackCommentary.ContainsKey(mainEvent) && fallbackCommentary[mainEvent].Count > 0) {
            string fallback = fallbackCommentary[mainEvent][Random.Range(0, fallbackCommentary[mainEvent].Count)];

            string contrast = "";
            bool blueLeading = blueScore > redScore;
            bool redLeading = redScore > blueScore;

            System.Action<string> AppendContrast = (clause) => {
                if (!string.IsNullOrEmpty(clause)) {
                    if (!fallback.EndsWith("!") && !fallback.EndsWith(".") && !fallback.EndsWith("?")) fallback += ".";
                    fallback += " " + clause;
                }
            };

            switch (mainEvent) {
                case CommentaryEvent.BlueDoubleScore:
                case CommentaryEvent.BlueInvincible:
                case CommentaryEvent.BlueKill:
                case CommentaryEvent.BlueDash:
                    if (redLeading) {
                        contrast = new string[] {
                            "But Red is leading " + $"{redScore} - {blueScore}!",
                            "Yet Red still holds the lead at " + $"{redScore} - {blueScore}!",
                            "Still, Red is ahead with a score of " + $"{redScore} - {blueScore}!"
                        }[Random.Range(0, 3)];
                    }
                    else if (blueLeading) {
                        contrast = new string[] {
                            "That extends Blue's advantage to " + $"{blueScore} - {redScore}!",
                            "Blue is pulling away at " + $"{blueScore} - {redScore}!",
                            "Blue's lead grows to " + $"{blueScore} - {redScore}!",
                            "And Blue is getting further ahead at " + $"{blueScore} - {redScore}!"
                        }[Random.Range(0, 4)];
                    }
                    break;

                case CommentaryEvent.RedDoubleScore:
                case CommentaryEvent.RedInvincible:
                case CommentaryEvent.RedKill:
                case CommentaryEvent.RedDash:
                    if (blueLeading) {
                        contrast = new string[] {
                            "But Blue is leading " + $"{blueScore} - {redScore}!",
                            "Yet Blue still holds the lead at " + $"{blueScore} - {redScore}!",
                            "Still, Blue is ahead with a score of " + $"{blueScore} - {redScore}!"
                        }[Random.Range(0, 3)];
                    }
                    else if (redLeading) {
                        contrast = new string[] {
                            "That extends Red's advantage to " + $"{redScore} - {blueScore}!",
                            "Red is pulling away at " + $"{redScore} - {blueScore}!",
                            "Red's lead grows to " + $"{redScore} - {blueScore}!",
                            "And Red is getting further ahead at " + $"{redScore} - {blueScore}!"
                        }[Random.Range(0, 4)];
                    }
                    break;

                case CommentaryEvent.HalfTime:
                    if (blueLeading) {
                        contrast = "Now Blue leads at " + $"{blueScore} - {redScore}!";
                    }
                    else if (redLeading) {
                        contrast = "Now Red leads at " + $"{redScore} - {blueScore}!";
                    }
                    else {
                        contrast = "And the score is tied at " + $"{blueScore} - {redScore}!";
                    }
                    break;

                case CommentaryEvent.FifteenSeconds:
                    if (blueLeading) {
                        contrast = "Now Blue is still leading at " + $"{blueScore} - {redScore}!";
                    }
                    else if (redLeading) {
                        contrast = "Now Red is still leading at " + $"{redScore} - {blueScore}!";
                    }
                    else {
                        contrast = "With only 15 seconds remaining, it is still tied!";
                    }
                    break;
                case CommentaryEvent.Victory:
                    contrast = BuildVictoryLine(blueScore, redScore);
                    break;
                case CommentaryEvent.GameStart:
                    // No contrast needed for game start
                    contrast = "";
                    break;
                default:
                    contrast = "";
                    break;
            }

            // Special handling for Victory/HalfTime branches that previously appended score info inside fallback
            if (mainEvent == CommentaryEvent.Victory) {
                // Replace fallback entirely with a more emphatic closing if contrast already contains the score statement
                if (!string.IsNullOrEmpty(contrast)) {
                    // Use existing fallback as opener then add the contrast (which already includes final score)
                    if (!fallback.EndsWith("!") && !fallback.EndsWith(".") && !fallback.EndsWith("?")) fallback += ".";
                    fallback += " " + contrast;
                    return fallback;
                }
            }

            if (mainEvent == CommentaryEvent.HalfTime) {
                // HalfTime fallback handled: attach the halftime score phrase
                if (!string.IsNullOrEmpty(contrast)) {
                    AppendContrast(contrast);
                    return fallback;
                }
            }

            if (!string.IsNullOrEmpty(contrast) && mainEvent != CommentaryEvent.Victory && mainEvent != CommentaryEvent.HalfTime) {
                if (fallback.Contains("score is tied") || fallback.Contains("ahead") || fallback.Contains("leading") || fallback.Contains("wins") || fallback.Contains("takes the victory") || fallback.Contains("draw")) {
                    if (redLeading && (mainEvent == CommentaryEvent.BlueDoubleScore || mainEvent == CommentaryEvent.BlueInvincible || mainEvent == CommentaryEvent.BlueKill || mainEvent == CommentaryEvent.BlueDash)) {
                        AppendContrast("Still, Blue needs more to close the gap!");
                    }
                    else if (blueLeading && (mainEvent == CommentaryEvent.RedDoubleScore || mainEvent == CommentaryEvent.RedInvincible || mainEvent == CommentaryEvent.RedKill || mainEvent == CommentaryEvent.RedDash)) {
                        AppendContrast("Still, Red has to push harder to catch up!");
                    }
                }
                else {
                    AppendContrast(contrast);
                }
            }

            if (mainEvent == CommentaryEvent.HalfTime && !string.IsNullOrEmpty(contrast)) {
                AppendContrast(contrast);
                return fallback;
            }

            return fallback;
        }

        return "";
    }

    private void GenerateLLMLine(List<CommentaryEvent> events){
        if (events.Count == 0) return;

        if (IsInFinalSilentWindow() && !events.Contains(CommentaryEvent.Victory)) {
            return;
        }

        int blueScore = 0, redScore = 0;
        float timeLeft = 0f;
        float maxTimeLeft = 90f;
        if (Game.instance != null) {
            blueScore = (int)Game.instance.scores.x;
            redScore = (int)Game.instance.scores.y;
            timeLeft = Game.instance.timer;
            maxTimeLeft = Game.instance.maxTime;
        }

        string fallbackText = GenerateFallbackCommentary(events, blueScore, redScore, timeLeft, maxTimeLeft);

        if (!string.IsNullOrEmpty(fallbackText)) {
            voiceLineQueue.Enqueue(fallbackText);
        }
        lastCommentaryTime = Time.time;
    }

    private void SendPrompt(string prompt){
        int myGenId = ++generationId;
        string result = "";
        lastCommentaryTime = Time.time;
    }

    public string testString;
    public PiperManager piper;
    public AudioSource audioSource;

    private IEnumerator PlayVoiceLine(string text){
        isSpeaking = true;

        string ttsText = System.Text.RegularExpressions.Regex.Replace(text, @"(\d+)\s*-\s*(\d+)", "$1 to $2");
        var ttsTask = piper.TextToSpeechAsync(ttsText);

        if (subtitleCoroutine != null) StopCoroutine(subtitleCoroutine);
        subtitleCoroutine = StartCoroutine(ShowSubtitle(text));

        while (!ttsTask.IsCompleted)
            yield return null;

        if (ttsTask.IsFaulted) {
            Debug.LogException(ttsTask.Exception);
        }
        else if (!ttsTask.IsCanceled && ttsTask.Result != null) {
            audioSource.Stop();
            if (audioSource && audioSource.clip)
                Destroy(audioSource.clip);

            audioSource.clip = ttsTask.Result;
            audioSource.Play();

            yield return new WaitForSeconds(audioSource.clip.length + Random.Range(2f, 3f));
        }
        else {
            yield return new WaitForSeconds(text.Length * 0.1f + 1f);
        }

        isSpeaking = false;

        if (subtitleCoroutine != null) StopCoroutine(subtitleCoroutine);
        subtitleCoroutine = StartCoroutine(HideSubtitle());
    }

    private IEnumerator ShowSubtitle(string text){
        subtitleText.text = text;
        subtitleCanvasGroup.transform.localScale = Vector3.one * 0.8f;

        float t = 0;
        while (t < 0.2f) {
            t += Time.deltaTime;
            float normalizedTime = t / 0.2f;
            subtitleCanvasGroup.alpha = Mathf.Lerp(0, 1, normalizedTime);
            subtitleCanvasGroup.transform.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, Mathf.SmoothStep(0, 1, normalizedTime));
            yield return null;
        }
        subtitleCanvasGroup.alpha = 1f;
        subtitleCanvasGroup.transform.localScale = Vector3.one;
    }

    private IEnumerator HideSubtitle(){
        float t = 0;
        while (t < 0.2f) {
            t += Time.deltaTime;
            float normalizedTime = t / 0.2f;
            subtitleCanvasGroup.alpha = Mathf.Lerp(1, 0, normalizedTime);
            subtitleCanvasGroup.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.9f, Mathf.SmoothStep(0, 1, normalizedTime));
            yield return null;
        }
        subtitleCanvasGroup.alpha = 0f;
    }
}
