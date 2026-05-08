using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using LLMUnity;
using Piper;
using Random = UnityEngine.Random;

public enum CommentaryEvent {
    GameStart,
    // BlueScore,
    // RedScore,
    BlueKill,
    RedKill,
    BlueDash,
    RedDash,
    // BlueShield,
    // RedShield,
    // CoinMoved,
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

    // public LLMAgent llmAgent;

    [Header("Settings")]
    [Tooltip("控制两次播报之间的最小时间间隔（秒），用于控制播报频率")]
    public float commentaryCooldown = 8f;
    public float eventBufferTime = 0.6f;// How long to wait to accumulate events before speaking

    private float lastCommentaryTime;
    private float currentBufferTime;

    private Queue<string> voiceLineQueue = new Queue<string>();
    private List<CommentaryEvent> eventBuffer = new List<CommentaryEvent>();
    private bool isSpeaking;
    private bool isGenerating;
    private int generationId;

    // Removed static voice lines and fallback generation - prompts are now LLM-driven only.

    // Score history / simple heuristics for comeback / rapid closing
    private int maxLeadBlue;
    private int maxLeadRed;

    private bool hasAnnouncedGameStart;
    private bool hasAnnouncedHalfTime;
    private bool hasAnnouncedFifteenSeconds;

    private TMPro.TextMeshProUGUI subtitleText;
    private CanvasGroup subtitleCanvasGroup;
    private Coroutine subtitleCoroutine;
    private Coroutine playVoiceLineCoroutine;

    // Hardcoded fallback commentary library for intelligent fallback
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
                "And that is game! What an absolutely phenomenal display of skill we've just witnessed!",
                "Time is up! It's all over! I cannot believe my eyes, what a breathtaking conclusion to an unforgettable match!",
                "The final whistle blows! A historic finish that we'll be talking about for years to come! Absolutely spectacular!",
                "Match Ends! The dust settles and the winner stands tall! Truly a masterful performance from start to finish!"
            }
        }
    };

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
        bgImage.color = new Color(0, 0, 0, 0.7f);// Semi-transparent black background

        RectTransform bgRt = bgGO.GetComponent<RectTransform>();
        bgRt.anchorMin = new Vector2(0.5f, 0.08f);
        bgRt.anchorMax = new Vector2(0.5f, 0.08f);
        bgRt.pivot = new Vector2(0.5f, 0f);
        bgRt.sizeDelta = new Vector2(1200, 0);// Fixed width, height driven by content

        UnityEngine.UI.VerticalLayoutGroup layout = bgGO.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.padding = new RectOffset(30, 30, 15, 15);
        layout.childAlignment = TextAnchor.MiddleCenter;

        UnityEngine.UI.ContentSizeFitter fitter = bgGO.AddComponent<UnityEngine.UI.ContentSizeFitter>();
        fitter.horizontalFit = UnityEngine.UI.ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

        GameObject textGO = new GameObject("SubtitleText");
        textGO.transform.SetParent(bgGO.transform, false);

        subtitleText = textGO.AddComponent<TMPro.TextMeshProUGUI>();
        subtitleText.alignment = TMPro.TextAlignmentOptions.Center;// Centered text to match Overwatch
        subtitleText.fontSize = 30;
        subtitleText.color = Color.white;
        subtitleText.fontStyle = TMPro.FontStyles.Bold;
        subtitleText.textWrappingMode = TMPro.TextWrappingModes.Normal;

        // Try to get a nicer font from Game if available
        StartCoroutine(AssignFontRoutine());
    }

    private IEnumerator AssignFontRoutine(){
        yield return null;// Wait 1 frame for Game instance
        if (Game.instance != null && Game.instance.countdownText != null) {
            subtitleText.font = Game.instance.countdownText.font;
            subtitleText.fontSharedMaterial = Game.instance.countdownText.fontSharedMaterial;
        }
    }

    public void TriggerEvent(CommentaryEvent evt, float priority = 1f){
        // Ensure Victory is always highest priority
        if (evt == CommentaryEvent.Victory) {
            priority = 5f;
            hasAnnouncedGameStart = false;// 重置状态以便下一局可以正常播报
            hasAnnouncedHalfTime = false;
            hasAnnouncedFifteenSeconds = false;
        }

        // Do not generate or queue new events in the last 10 seconds, except Victory
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

            // 新局开始，重置所有状态
            voiceLineQueue.Clear();
            eventBuffer.Clear();
            isSpeaking = false;
            isGenerating = false;
            generationId++;// 取消当前生成
            if (playVoiceLineCoroutine != null) StopCoroutine(playVoiceLineCoroutine);
            if (audioSource) audioSource.Stop();
            if (subtitleCoroutine != null) StopCoroutine(subtitleCoroutine);
            if (subtitleCanvasGroup) subtitleCanvasGroup.alpha = 0f;

            // if (llmAgent != null) {
            //     _ = llmAgent.ClearHistory();
            // }
        }

        // Add to buffer
        if (!eventBuffer.Contains(evt)) {
            eventBuffer.Add(evt);
            currentBufferTime = eventBufferTime;
        }

        // If it's a super high priority event, we might clear the queue
        if (priority >= 3f) {
            voiceLineQueue.Clear();
            generationId++;// Cancel any currently pending generation
            isGenerating = false;

            // Immediate interrupt for absolute highest priority (e.g., Victory = 5f)
            if (priority >= 5f) {
                eventBuffer.Clear();
                eventBuffer.Add(evt);// Isolate the high priority event
                currentBufferTime = 0f;
                isSpeaking = false;
                if (playVoiceLineCoroutine != null) StopCoroutine(playVoiceLineCoroutine);
                if (audioSource) audioSource.Stop();
                if (subtitleCoroutine != null) StopCoroutine(subtitleCoroutine);
                if (subtitleCanvasGroup) subtitleCanvasGroup.alpha = 0f;
            }
        }
    }

    private bool IsInFinalSilentWindow(){
        return Game.instance != null
            && Game.instance.matchRunning
            && Game.instance.timer <= 10f;
    }

    private void Update(){
        // 最后10秒：停止处理新的普通播报，避免和结算播报冲突
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

        // Prevent overlapping too many minor events
        if (Time.time - lastCommentaryTime < commentaryCooldown && !isHighPriority) {
            eventBuffer.Clear();
            return;
        }

        List<CommentaryEvent> eventsCopy = new List<CommentaryEvent>(eventBuffer);
        eventBuffer.Clear();

        GenerateLLMLine(eventsCopy);
    }

    /// <summary>
    /// 生成智能的 hardcoded fallback 播报文本
    /// </summary>
    private string GenerateFallbackCommentary(List<CommentaryEvent> events, int blueScore, int redScore, float timeLeft, float maxTimeLeft){
        if (events.Count == 0) return "";

        // 选择要播报的事件（按优先级排序）
        CommentaryEvent mainEvent = events[0];

        // 获取该事件的 fallback 播报
        if (fallbackCommentary.ContainsKey(mainEvent) && fallbackCommentary[mainEvent].Count > 0) {
            string fallback = fallbackCommentary[mainEvent][Random.Range(0, fallbackCommentary[mainEvent].Count)];

            // Determine score relation to possibly add a contrast/transition phrase
            string contrast = "";
            bool blueLeading = blueScore > redScore;
            bool redLeading = redScore > blueScore;

            // Helper local: append contrast clause depending on event/team and current scores
            System.Action<string> AppendContrast = (clause) => {
                if (!string.IsNullOrEmpty(clause)) {
                    // Ensure proper spacing/punctuation
                    if (!fallback.EndsWith("!") && !fallback.EndsWith(".") && !fallback.EndsWith("?")) fallback += ".";
                    fallback += " " + clause;
                }
            };

            // Team-specific events where a contrast makes sense
            switch (mainEvent) {
                case CommentaryEvent.BlueDoubleScore:
                case CommentaryEvent.BlueInvincible:
                case CommentaryEvent.BlueKill:
                case CommentaryEvent.BlueDash:
                    if (redLeading) {
                        // Blue did something but Red still leads
                        contrast = new string[] {
                            "But Red is Leading" + $" {redScore} - {blueScore}.",
                            "Yet Red maintains the lead at " + $"{redScore} - {blueScore}.",
                            "Still, Red is ahead with a score of " + $"{redScore} - {blueScore}.",
                        }[Random.Range(0, 3)];
                    }
                    else if (blueLeading) {
                        contrast = new string[] {
                            "That extends Blue's advantage to " + $"{blueScore} - {redScore}.",
                            "And now blue is further leading at " + $"{blueScore} - {redScore}.",
                            "Blue is pulling away with a score of " + $"{blueScore} - {redScore}.",
                            "Blue's lead grows to " + $"{blueScore} - {redScore}."
                        }[Random.Range(0, 4)];
                    }
                    break;
                case CommentaryEvent.RedDoubleScore:
                case CommentaryEvent.RedInvincible:
                case CommentaryEvent.RedKill:
                case CommentaryEvent.RedDash:
                    if (blueLeading) {
                        // Blue did something but Red still leads
                        contrast = new string[] {
                            "But Blue is Leading" + $" {blueScore} - {redScore}.",
                            "Yet Blue maintains the lead at " + $"{blueScore} - {redScore}.",
                            "Still, Blue is ahead with a score of " + $"{blueScore} - {redScore}.",
                        }[Random.Range(0, 3)];
                    }
                    else if (redLeading) {
                        contrast = new string[] {
                            "That extends Red's advantage to " + $"{redScore} - {blueScore}.",
                            "And now Red is further leading at " + $"{redScore} - {blueScore}.",
                            "Red is pulling away with a score of " + $"{redScore} - {blueScore}.",
                            "Red's lead grows to " + $"{redScore} - {blueScore}."
                        }[Random.Range(0, 4)];
                    }
                    break;
                case CommentaryEvent.HalfTime:
                    if (blueLeading) {
                        contrast = "Now Blue leads at " + $"{blueScore} - {redScore}.";
                    }
                    else if (redLeading) {
                        contrast = "Now Red leads at" + $"{redScore} - {blueScore}.";
                    }
                    else {
                        contrast = "And the score is tied at " + $"{blueScore}! What a close match!";
                    }
                    break;
                case CommentaryEvent.FifteenSeconds:
                    if (blueLeading) {
                        if (blueScore - redScore >= 8) {
                            contrast = "And blue is keeping a huge lead at " + $"{blueScore} - {redScore}.";
                        }
                        contrast = "Now Blue is still leading at " + $"{blueScore} - {redScore}.";
                    }
                    else if (redLeading) {
                        if (redScore - blueScore >= 8) {
                            contrast = "And red is keeping a huge lead at " + $"{redScore} - {blueScore}.";
                        }
                        else {
                            contrast = "Now Red is still up " + $"{redScore} - {blueScore}.";
                        }
                    }
                    else {
                        contrast = "With only 15 seconds remaining it's still tied at " + $"{blueScore}.";
                    }
                    break;
                case CommentaryEvent.Victory:
                    if (blueScore > redScore) contrast = "Blue wins the match " + $"{blueScore} - {redScore}!";
                    else if (redScore > blueScore) contrast = "Red takes the victory " + $"{redScore} - {blueScore}!";
                    else contrast = "It's a draw at " + $"{blueScore}!";
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

            // For other events, if we have a contrast clause either append it or, if earlier code already appended some score text, ensure we don't duplicate
            if (!string.IsNullOrEmpty(contrast) && mainEvent != CommentaryEvent.Victory && mainEvent != CommentaryEvent.HalfTime) {
                // If fallback already contained a score mention like "The score is tied" skip adding redundant phrase
                if (fallback.Contains("score is tied") || fallback.Contains("ahead") || fallback.Contains("leading") || fallback.Contains("wins") || fallback.Contains("takes the victory") || fallback.Contains("draw")) {
                    // In many cases the existing fallback already mentions the score; only add a short contrast if it doesn't restate the same fact
                    // We'll still add a short connective where helpful
                    if (redLeading && (mainEvent == CommentaryEvent.BlueDoubleScore || mainEvent == CommentaryEvent.BlueInvincible || mainEvent == CommentaryEvent.BlueKill || mainEvent == CommentaryEvent.BlueDash)) {
                        AppendContrast("Still, Blue needs more to close the gap.");
                    }
                    else if (blueLeading && (mainEvent == CommentaryEvent.RedDoubleScore || mainEvent == CommentaryEvent.RedInvincible || mainEvent == CommentaryEvent.RedKill || mainEvent == CommentaryEvent.RedDash)) {
                        AppendContrast("Still, Red must push to catch up.");
                    }
                }
                else {
                    // Append the contrast as-is
                    AppendContrast(contrast);
                }
            }

            // If we reached here, some events (like Victory/HalfTime) were already returned above when appropriate
            return fallback;
        }

        return "";
    }

    private void GenerateLLMLine(List<CommentaryEvent> events){
        if (events.Count == 0) return;

        if (IsInFinalSilentWindow() && !events.Contains(CommentaryEvent.Victory)) {
            return;
        }

        // Always include current scores
        int blueScore = 0, redScore = 0;
        float timeLeft = 0f;
        float maxTimeLeft = 90f;
        if (Game.instance != null) {
            blueScore = (int)Game.instance.scores.x;
            redScore = (int)Game.instance.scores.y;
            timeLeft = Game.instance.timer;
            maxTimeLeft = Game.instance.maxTime;
        }

        // 生成 fallback 文本（用作 LLM prompt 或直接使用）
        string fallbackText = GenerateFallbackCommentary(events, blueScore, redScore, timeLeft, maxTimeLeft);

        // 如果 LLM 不可用，直接使用 fallback
        if (true) {
            if (!string.IsNullOrEmpty(fallbackText)) {
                voiceLineQueue.Enqueue(fallbackText);
            }
            lastCommentaryTime = Time.time;
            return;
        }

        isGenerating = true;

        if (events.Contains(CommentaryEvent.Victory)) {
            string victoryPrompt = $"{fallbackText} Final Score: Blue {blueScore} - Red {redScore}. Make a memorable closing statement for the match.";
            SendPrompt(victoryPrompt);
            return;
        }

        if (events.Contains(CommentaryEvent.GameStart)) {
            SendPrompt(fallbackText);
            return;
        }

        // Update simple lead history
        // int diff = blueScore - redScore;
        // if (diff > maxLeadBlue) maxLeadBlue = diff;
        // if (-diff > maxLeadRed) maxLeadRed = -diff;

        // Check if Tie or Leading
        string matchStatus = blueScore == redScore ? "Tie" : (blueScore > redScore ? "Blue leading" : "Red leading");

        // Provide context about the length of the match
        string timeContext = "";
        if (timeLeft > maxTimeLeft * 0.7f) timeContext = "Early game";
        else if (timeLeft > maxTimeLeft * 0.4f) timeContext = "Mid game";
        else if (timeLeft > 15f) timeContext = "Late game";
        else timeContext = "final countdown";

        // Build prompt with fallback as base, asking LLM to improve/optimize it
        string llmPrompt = $"{fallbackText}. Context: {timeContext}.";

        Debug.Log($"Fallback: {fallbackText}");
        Debug.Log($"LLM Prompt: {llmPrompt}");
        SendPrompt(llmPrompt);
    }

    private void SendPrompt(string prompt){
        int myGenId = ++generationId;
        string result = "";
        // _ = llmAgent.Chat(prompt, (response) => {
        //     if (myGenId != generationId) return;// Discard if interrupted
        //     if (!string.IsNullOrEmpty(response)) {
        //         result = response.Trim();
        //     }
        // }, () => {
        //     if (myGenId != generationId) return;// Discard if interrupted
        //     isGenerating = false;
        //     if (!string.IsNullOrEmpty(result)) {
        //         voiceLineQueue.Enqueue(result);
        //     }
        // });

        lastCommentaryTime = Time.time;
    }

    public string testString;
    public PiperManager piper;
    public AudioSource audioSource;

    private IEnumerator PlayVoiceLine(string text){
        isSpeaking = true;

        Debug.Log($"[COMMENTATOR]: {text}");

        string ttsText = System.Text.RegularExpressions.Regex.Replace(text, @"(\d+)\s*-\s*(\d+)", "$1 to $2");
        var ttsTask = piper.TextToSpeechAsync(ttsText);

        if (subtitleCoroutine != null) StopCoroutine(subtitleCoroutine);
        subtitleCoroutine = StartCoroutine(ShowSubtitle(text));

        // 不阻塞主线程，逐帧等待
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

        // if (llmAgent != null)
        //     _ = llmAgent.ClearHistory();

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
